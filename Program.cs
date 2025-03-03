using Microsoft.EntityFrameworkCore;
using group_finder.Data;
using group_finder;
using group_finder.Domain.Matchmaking;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

var DiscordId = builder.Configuration[Constants.DiscordClientIdKey] ?? throw new Exception("'Discord:ClientId' is missing from configuration/env");
var DiscordSecret = builder.Configuration[Constants.DiscordClientSecretKey] ?? throw new Exception("'Discord:ClientSecret' is missing from configuration/env"); ;
var DiscordBotToken = builder.Configuration[Constants.DiscordBotTokenKey] ?? throw new Exception("'Discord:BotToken' is missing from configuration/env"); ;


// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());

builder.Services.AddScoped<MatchmakingService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<SeedDB>();

builder.Services.AddDefaultIdentity<User>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Student", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("Teacher", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("Admin", policy => policy.RequireAuthenticatedUser());

    // options.AddPolicy("Teacher", policy =>
    // policy.RequireAssertion(context =>
    //     context.User.HasClaim(c =>
    //         (c.Type == "IsTeacher" ||
    //          c.Type == "IsAdmin"))));
    // options.AddPolicy("Admin", policy => policy.RequireClaim("IsAdmin"));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Student", policy: "Student");

    options.Conventions.AuthorizeFolder("/Teacher", policy: "Teacher");

    options.Conventions.AuthorizeFolder("/Admin", policy: "Admin");
});

builder.Services.AddAuthentication().AddDiscord(options =>
{
    options.ClientId = DiscordId;
    options.ClientSecret = DiscordSecret;
    options.Scope.Add("identify");
    options.Scope.Add("email");
});

builder.Services.AddSingleton(provider =>
{
    var botService = new DiscordBotService();
    botService.StartAsync(DiscordBotToken).GetAwaiter().GetResult();
    return botService;
});

var app = builder.Build();

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

// Configure forwarded headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
});

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
