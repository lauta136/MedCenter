using MedCenter.Data;
using MedCenter.Models;
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

            Console.WriteLine("Inicializando datos de roles...");

            var passwordHasher = new PasswordHashService();
            var adminPlainKey = configuration["RoleKeys:Admin"];

            // Validar que la clave esté configurada
            if (string.IsNullOrEmpty(adminPlainKey))
            {
                throw new InvalidOperationException("La clave de Admin no está configurada en appsettings.json");
            }

            // Solo agregar Admin si no existe (Medico y Secretaria ya existen)
            if (!await context.role_keys.AnyAsync(r => r.Role == "Admin"))
            {
                // Obtener el siguiente ID disponible
                var maxId = await context.role_keys.MaxAsync(r => (int?)r.Id) ?? 0;
                var nextId = maxId + 1;
                
                var hashedKey = passwordHasher.HashPassword(adminPlainKey);
                await context.Database.ExecuteSqlAsync(
                    $"INSERT INTO role_keys (\"Id\", role, hashed_key) VALUES ({nextId}, 'Admin', {hashedKey})");
                Console.WriteLine("Rol Admin agregado.");
            }
            else
            {
                Console.WriteLine("Rol Admin ya existe.");
            }

            Console.WriteLine("Datos de roles inicializados correctamente.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al inicializar la base de datos: {ex.Message}");
            throw;
        }
    }
}