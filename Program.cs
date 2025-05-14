using Coravel;
using Coravel.Queuing.Interfaces;
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
using QuickFinder.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DiscordServiceOptions>(
    builder.Configuration.GetSection(DiscordServiceOptions.Discord)
);
builder.Services.Configure<DiscordAuthOptions>(
    builder.Configuration.GetSection(DiscordAuthOptions.DiscordAuth)
);
builder.Services.Configure<MatchmakingOptions>(
    builder.Configuration.GetSection(MatchmakingOptions.Matchmaking)
);
builder
    .Services.AddOptions<MatchmakingOptions>()
    .Bind(builder.Configuration.GetSection(MatchmakingOptions.Matchmaking))
    .ValidateDataAnnotations();

// Add services to the container.
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddHttpClient();

builder.Services.AddScoped<MatchmakingService>();
builder.Services.AddScoped<TicketRepository>();
builder.Services.AddScoped<GroupTicketRepository>();
builder.Services.AddScoped<CourseRepository>();
builder.Services.AddScoped<GroupRepository>();
builder.Services.AddScoped<PreferencesRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<GroupMatchmakingService>();
builder.Services.AddScoped<SeedDB>();
builder.Services.AddScoped<DiscordAuthHandler>();
builder.Services.AddScheduler();
builder.Services.AddQueue();
builder.Services.AddEvents();

if (builder.Environment.IsProduction())
{
    builder
        .Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["dpkeys"] ?? "./dpkeys"));
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

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = StudentRoutes.Login();
    options.AccessDeniedPath = StudentRoutes.Home();
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Student", policy: "Student");
    options.Conventions.AuthorizeFolder("/Teacher", policy: "Teacher");
    options.Conventions.AuthorizeFolder("/Admin", policy: "Admin");

    options.Conventions.AuthorizeAreaFolder("Identity", "/Account", policy: "Admin");

    var pages = new[] { "/Login", "/Logout", "/Manage/Index", "/Manage/PersonalData" };

    foreach (var page in pages)
    {
        options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account" + page);
    }
});

builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddScoped<DiscordService>();
builder.Services.AddHostedService<DiscordService>();
builder.Services.AddSingleton<IEmailSender, StubEmailSender>();
builder.Services.AddTransient<RunMatchmakingInvocable>();
builder.Services.AddTransient<DeleteUnusedGroupsInvocable>();
builder.Services.AddTransient<SendDMInvocable>();
builder.Services.AddTransient<OnUserDeleted>();
builder.Services.AddTransient<NotifyUsersOnGroupFilled>();
builder.Services.AddTransient<CreateDiscordChannelOnGroupFilled>();
builder.Services.AddTransient<InviteToServerOnCourseJoined>();

// Configure forwarded headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();
using var scope = app.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<IQueue>>();
app.Services.ConfigureQueue()
    .LogQueuedTaskProgress(logger)
    .OnError(e => logger.LogError(e, "Error in queue:"));
app.Services.ConfigureScheduler(app.Configuration);

var registration = app.Services.ConfigureEvents();

registration.Register<UserDeleted>().Subscribe<OnUserDeleted>();

registration
    .Register<GroupFilled>()
    .Subscribe<NotifyUsersOnGroupFilled>()
    .Subscribe<CreateDiscordChannelOnGroupFilled>();

registration.Register<GroupDisbanded>().Subscribe<DeleteDiscordChannelOnGroupDisbanded>();

registration.Register<GroupMemberLeft>().Subscribe<DeleteUserPermissionsOnGroupMemberLeft>();

registration.Register<GroupMemberAdded>();

registration.Register<CourseJoined>().Subscribe<InviteToServerOnCourseJoined>();

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
    using var db_scope = app.Services.CreateScope();
    var seed_db = db_scope.ServiceProvider.GetRequiredService<SeedDB>();
    seed_db.Seed();
}

app.Run();

public partial class Startup { }
