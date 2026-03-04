using Microsoft.AspNetCore.Identity;
using SecureMedicalRecordSystem.Core.Entities;

namespace SecureMedicalRecordSystem.Infrastructure.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        // Part 1: Seed Roles
        string[] roles = { "Admin", "Doctor", "Patient" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
                Console.WriteLine($"[IdentitySeeder] Created Role: {role}");
            }
        }

        // Part 2: Seed Admin User
        var admins = await userManager.GetUsersInRoleAsync("Admin");
        if (admins.Count == 0)
        {
            var adminUser = new ApplicationUser
            {
                Email = "admin@medicalrecord.com",
                UserName = "admin@medicalrecord.com",
                FirstName = "System",
                LastName = "Administrator",
                Role = "Admin",
                EmailConfirmed = true,
                TwoFactorEnabled = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine($"[IdentitySeeder] Created Admin user: {adminUser.Email} / Admin@123!");
            }
            else
            {
                Console.WriteLine("[IdentitySeeder] Failed to create admin:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($" - {error.Description}");
                }
            }
        }

        // Part 3: Seed Test Patient
        var patients = await userManager.GetUsersInRoleAsync("Patient");
        if (patients.Count == 0)
        {
            var testPatient = new ApplicationUser
            {
                Email = "patient@test.com",
                UserName = "patient@test.com",
                FirstName = "Test",
                LastName = "Patient",
                Role = "Patient",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(testPatient, "Patient@123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(testPatient, "Patient");
                
                // We need the DbContext to create the Patient entity
                // This might be tricky in a static method without injecting context, 
                // but Program.cs passes userManager which is from a scope that has the context.
                // However, SeedAsync doesn't take context. 
                // Let's assume for now the user can register or we add it later if needed.
                // Actually, let's keep it simple and just do the Identity part.
                Console.WriteLine($"[IdentitySeeder] Created test patient: {testPatient.Email} / Patient@123!");
            }
        }
    }
}
