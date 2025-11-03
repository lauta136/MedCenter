using MedCenter.Data;
using Microsoft.EntityFrameworkCore;
using MedCenter.Services.Authentication.Components;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context, IConfiguration configuration)
    {
        try
        {
            // Asegurar que la base de datos está creada y las migraciones aplicadas
            await context.Database.MigrateAsync();

            // Si ya hay datos de roles, no hacer nada
            if (await context.role_keys.AnyAsync())
            {
                Console.WriteLine("Los datos de roles ya existen en la base de datos.");
                return;
            }

            Console.WriteLine("Inicializando datos de roles...");

            var passwordHasher = new PasswordHashService();
            var medicoPlainKey = configuration["RoleKeys:Medico"];
            var secretariaPlainKey = configuration["RoleKeys:Secretaria"];

            // Validar que las claves estén configuradas
            if (string.IsNullOrEmpty(medicoPlainKey) || string.IsNullOrEmpty(secretariaPlainKey))
            {
                throw new InvalidOperationException("Las claves de rol no están configuradas en appsettings.json");
            }

            var roles = new[]
            {
                new RoleKey 
                { 
                    Id = 1,
                    Role = "Medico", 
                    HashedKey = passwordHasher.HashPassword(medicoPlainKey) 
                },
                new RoleKey 
                { 
                    Id = 2,
                    Role = "Secretaria", 
                    HashedKey = passwordHasher.HashPassword(secretariaPlainKey) 
                }
            };

            await context.role_keys.AddRangeAsync(roles);
            await context.SaveChangesAsync();

            Console.WriteLine("Datos de roles inicializados correctamente.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al inicializar la base de datos: {ex.Message}");
            throw;
        }
    }
}