
using MedCenter.Data;
using Microsoft.EntityFrameworkCore;
using MedCenter.Services.TurnoStates;
using MedCenter.Services.DisponibilidadMedico;
using MedCenter.Services.EspecialidadService;
using MedCenter.Services.TurnoSv;
using MedCenter.Services.HistoriaClinicaSv;
using MedCenter.Services.Reportes;
using MedCenter.Services.MedicoSv;
using MedCenter.Services;
using MedCenter.Services.Authentication;
using MedCenter.Services.Authentication.Components;
using MedCenter.Services.EmailService;
using Microsoft.Extensions.Caching.Memory;
using MedCenter.Services.AdminService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<TurnoStateService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // tiempo que dura la sesión
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PasswordHashService>();
builder.Services.AddScoped<DisponibilidadService>();
builder.Services.AddScoped<EspecialidadService>();
builder.Services.AddScoped<TurnoService>();
builder.Services.AddScoped<HistoriaClinicaService>();
builder.Services.AddScoped<ReportesService>();
builder.Services.AddScoped<MedicoService>();
builder.Services.AddScoped<PasswordRecoveryService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AdminService>();

builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// Add background service for session cleanup
builder.Services.AddHostedService<SessionCleanupService>();

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login"; // ruta donde redirige si no está logueado
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied"; // opcional
        
        // Session timeout and sliding expiration
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;  // Resets timer on activity
        
        // Security settings for medical app
        options.Cookie.HttpOnly = true;  // Prevent JavaScript access (XSS protection)
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS only
        options.Cookie.SameSite = SameSiteMode.Strict;  // CSRF protection
        options.Cookie.IsEssential = true;
        
        // Session cookie (deleted when browser closes) - no persistence
        // options.Cookie.MaxAge is null by default = session cookie
    });

var app = builder.Build();

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

// ACTIVA el sistema de sesión aquí:
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");
    
// Inicializar la base de datos
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    await DbInitializer.InitializeAsync(context, configuration);
}

app.Run();
