using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Infrastructure.Data;
using SecureMedicalRecordSystem.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer("Server=localhost;Database=SecureMedicalRecordDB_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"));
    })
    .Build();

using var scope = host.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

Console.WriteLine("--- Doctors ---");
var doctors = await context.Doctors.Include(d => d.User).Include(d => d.Department).ToListAsync();
foreach (var d in doctors)
{
    Console.WriteLine($"ID: {d.Id}, Name: Dr. {d.User?.LastName}, Dept: {d.Department?.Name}");
}

Console.WriteLine("\n--- Availability ---");
var avail = await context.DoctorAvailabilities.ToListAsync();
foreach (var a in avail)
{
    Console.WriteLine($"DrId: {a.DoctorId}, Day: {a.DayOfWeek}, Date: {a.SpecificDate}, Start: {a.StartTime}, End: {a.EndTime}, Avail: {a.IsAvailable}");
}

Console.WriteLine("\n--- Appointments ---");
var appts = await context.Appointments.OrderByDescending(a => a.AppointmentDate).Take(10).ToListAsync();
foreach (var a in appts)
{
    Console.WriteLine($"ApptId: {a.Id}, DrId: {a.DoctorId}, Date: {a.AppointmentDate}, Status: {a.Status}");
}
