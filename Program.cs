using Coravel;
using Coravel.Scheduling.Schedule.Interfaces;
using Discord.WebSocket;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using QuickFinder;
using QuickFinder.Data;
using QuickFinder.Domain.DiscordDomain;
using QuickFinder.Domain.Matchmaking;
using QuickFinder.Email;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DiscordServiceOptions>(
    builder.Configuration.GetSection(DiscordServiceOptions.Discord)
);
builder.Services.Configure<MatchmakingOptions>(
    builder.Configuration.GetSection(MatchmakingOptions.Matchmaking)
);
builder.Services.Configure<MailjetOptions>(
    builder.Configuration.GetSection(MailjetOptions.Mailjet)
);

// Add services to the container.
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());

builder.Services.AddScoped<MatchmakingService>();
builder.Services.AddScoped<TicketRepository>();
builder.Services.AddScoped<CourseRepository>();
builder.Services.AddScoped<GroupRepository>();
builder.Services.AddScoped<PreferencesRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<SeedDB>();
builder.Services.AddScheduler();

if (builder.Environment.IsProduction())
{
    builder
        .Services.AddDataProtection()
        .PersistKeysToFileSystem(
            new DirectoryInfo(builder.Configuration[Constants.DPKeysDirKey] ?? "./dpkeys")
        );
}

builder
    .Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddUserManager<CustomUserManager>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Student", policy => policy.RequireAuthenticatedUser());

    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("Teacher", policy => policy.RequireAuthenticatedUser());
        options.AddPolicy("Admin", policy => policy.RequireAuthenticatedUser());
    }
    else
    {
        options.AddPolicy(
            "Teacher",
            policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "IsTeacher" || c.Type == "IsAdmin")
                )
        );

        options.AddPolicy("Admin", policy => policy.RequireClaim("IsAdmin"));
    }
});

builder.Services.ConfigureApplicationCookie(options => options.LoginPath = StudentRoutes.Login());

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Student", policy: "Student");

    options.Conventions.AuthorizeFolder("/Teacher", policy: "Teacher");

    options.Conventions.AuthorizeFolder("/Admin", policy: "Admin");
});

builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddScoped<DiscordService>();
builder.Services.AddHostedService<DiscordService>();
builder.Services.AddSingleton<IEmailSender, StubEmailSender>();
builder.Services.AddTransient<RunMatchmakingInvocable>();

// Configure forwarded headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.Services.UseScheduler(scheduler =>
    {
        scheduler.Schedule<RunMatchmakingInvocable>().EveryThirtySeconds();
    })
    .OnError(e =>
    {
        var logger = app.Services.GetRequiredService<ILogger<Startup>>();
        logger.LogError(e, "Error in scheduled task: {Message}", e.Message);
    });

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseForwardedHeaders();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seed_db = scope.ServiceProvider.GetRequiredService<SeedDB>();
    seed_db.Seed();
}

app.Run();

public partial class Startup { }
