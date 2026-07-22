using CloudinaryDotNet;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using QRemember.Web.Data;
using QRemember.Web.Models;
using QRemember.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Razor Pages — organizer pages require an authenticated organizer, guest pages stay public
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Shared/Events");
});

builder.Services.AddSingleton<IQrCodeService, QrCodeService>();

// Cloudinary
builder.Services.AddSingleton(_ =>
{
    var cloudName = builder.Configuration["Cloudinary:CloudName"]
        ?? throw new InvalidOperationException("Cloudinary:CloudName is not configured.");
    var apiKey = builder.Configuration["Cloudinary:ApiKey"]
        ?? throw new InvalidOperationException("Cloudinary:ApiKey is not configured.");
    var apiSecret = builder.Configuration["Cloudinary:ApiSecret"]
        ?? throw new InvalidOperationException("Cloudinary:ApiSecret is not configured.");

    return new Cloudinary(new Account(cloudName, apiKey, apiSecret));
});
builder.Services.AddScoped<ICloudinaryImageService, CloudinaryImageService>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 20 * 1024 * 1024;
});

// Database — reads from user-secrets in development, environment variables in production
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

// ASP.NET Core Identity for organizer login
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // adjust later if you add email confirmation
    options.Password.RequiredLength = 8;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Shared/Onboarding/Login";
    options.AccessDeniedPath = "/Shared/Onboarding/Login";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
// In Development, guests scan the QR code over plain HTTP from their phone on the LAN;
// redirecting to HTTPS here would hit the untrusted local dev cert and fail on their device.

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();