using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Infrastructure.Data;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args).Build();
using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

var patient = db.Patients.OrderByDescending(p => p.CreatedAt).First();
var patientId = patient.Id;

Console.WriteLine($"Checking Appointments for Patient: {patientId}");

var appointments = db.Appointments
    .Where(a => a.PatientId == patientId)
    .Select(a => new { a.Id, a.Status, a.AppointmentDate })
    .ToList();

Console.WriteLine($"Total appointments: {appointments.Count}");
foreach (var a in appointments)
{
    Console.WriteLine($"ID: {a.Id}, Status: {a.Status}, Date: {a.AppointmentDate}");
}

var stats = appointments
    .GroupBy(a => a.Status)
    .Select(g => new { Status = g.Key, Count = g.Count() })
    .ToList();

Console.WriteLine("\nStatus Summary:");
foreach (var s in stats)
{
    Console.WriteLine($"{s.Status}: {s.Count}");
}
