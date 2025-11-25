
using MedCenter.Data;
using Microsoft.EntityFrameworkCore;
using MedCenter.Services.TurnoStates;
using MedCenter.Services.DisponibilidadMedico;
using MedCenter.Services.EspecialidadService;
using MedCenter.Services.TurnoSv;
using MedCenter.Services.HistoriaClinicaSv;

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
builder.Services.AddScoped<DisponibilidadService>();
builder.Services.AddScoped<EspecialidadService>();
builder.Services.AddScoped<TurnoService>();
builder.Services.AddScoped<HistoriaClinicaService>();

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login"; // ruta donde redirige si no está logueado
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied"; // opcional
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
app.UseAuthorization();

// 🔥 ACTIVA el sistema de sesión aquí:
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
