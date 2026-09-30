using FlightManager;
using FlightManager.Data;
using FlightManager.Models;
using FlightManager.Services;
using FlightManager.Services.ExternalFlights;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MVCDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<MVCDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddScoped<FlightContext>();
builder.Services.AddScoped<ReservationContext>();
builder.Services.AddScoped<PassengerContext>();
builder.Services.AddScoped<IdentityContext>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IAirScoutService, AirScoutService>();

builder.Services.AddHttpClient<IAviationDataService, OpenSkyService>(client =>
{
    client.BaseAddress = new Uri("https://opensky-network.org/api/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.Configure<FlightSearchOptions>(builder.Configuration.GetSection("SerpApi"));

builder.Services.AddHttpClient<
    IFlightSearchService,
    SerpApiFlightSearchService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

builder.Services.AddHttpClient<
    IFlightLocationSearchService,
    SerpApiFlightLocationSearchService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    });

builder.Services.AddSingleton<IEmailSender, EmailSender>();


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages(); 
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<User>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    if (!await roleManager.RoleExistsAsync("Employee"))
        await roleManager.CreateAsync(new IdentityRole("Employee"));

    bool adminExists = await userManager.FindByNameAsync("admin") is not null;
    string? adminSeedPassword = builder.Configuration["AdminSeed:Password"];
    if (!adminExists && !string.IsNullOrWhiteSpace(adminSeedPassword))
    {
        User admin = new User
        {
            UserName = "admin",
            Email = "admin@flightmanager.com",
            FirstName = "Admin",
            LastName = "User",
            EGN = "0000000000",
            Address = "Sofia",
            PhoneNumber = "0000000000"
        };

        IdentityResult result = await userManager.CreateAsync(admin, adminSeedPassword);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
        else
            app.Logger.LogWarning("Admin seed account was not created: {Errors}", string.Join("; ", result.Errors.Select(error => error.Code)));
    }
    else if (!adminExists)
    {
        app.Logger.LogWarning("Admin seed account was not created because AdminSeed:Password is not configured.");
    }
}

app.Run();
