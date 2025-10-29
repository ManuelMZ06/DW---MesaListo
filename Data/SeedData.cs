using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MesaListo.Models;

namespace MesaListo.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Asegurar que la base de datos esté creada
                await context.Database.EnsureCreatedAsync();

                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

                // Crear roles
                string[] roleNames = { "Admin", "Restaurante", "Cliente" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // Crear usuario Admin
                var adminUser = new IdentityUser
                {
                    UserName = "admin@mesalisto.com",
                    Email = "admin@mesalisto.com",
                    EmailConfirmed = true
                };

                string adminPassword = "Admin123!";
                var admin = await userManager.FindByEmailAsync(adminUser.Email);

                if (admin == null)
                {
                    var createAdmin = await userManager.CreateAsync(adminUser, adminPassword);
                    if (createAdmin.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }

                // Verificar si ya existen restaurantes para no duplicar
                if (!context.Restaurantes.Any())
                {
                    // Crear usuario Restaurante de ejemplo
                    var restauranteUser = new IdentityUser
                    {
                        UserName = "restaurante@ejemplo.com",
                        Email = "restaurante@ejemplo.com",
                        EmailConfirmed = true
                    };

                    string restaurantePassword = "Rest123!";
                    var restaurante = await userManager.FindByEmailAsync(restauranteUser.Email);

                    if (restaurante == null)
                    {
                        var createRestaurante = await userManager.CreateAsync(restauranteUser, restaurantePassword);
                        if (createRestaurante.Succeeded)
                        {
                            await userManager.AddToRoleAsync(restauranteUser, "Restaurante");

                            // Crear restaurante de ejemplo
                            var restauranteEjemplo = new Restaurante
                            {
                                Nombre = "La Buena Mesa",
                                Direccion = "Calle Principal 123",
                                Telefono = "+1234567890",
                                UsuarioId = restauranteUser.Id
                            };

                            context.Restaurantes.Add(restauranteEjemplo);
                            await context.SaveChangesAsync();

                            // Crear mesas para el restaurante
                            for (int i = 1; i <= 10; i++)
                            {
                                var mesa = new Mesa
                                {
                                    Codigo = $"M{i:00}",
                                    Capacidad = i <= 5 ? 4 : 6,
                                    RestauranteId = restauranteEjemplo.Id
                                };
                                context.Mesas.Add(mesa);
                            }
                            await context.SaveChangesAsync();
                        }
                    }
                }
            }
        }
    }
}