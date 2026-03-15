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

Console.WriteLine($"Checking Patient: {patient.UserId} (ID: {patientId})");

var scanHistoryCount = db.ScanHistories.Count(s => s.PatientId == patientId);
Console.WriteLine($"Total ScanHistory records: {scanHistoryCount}");

var scans = db.ScanHistories
    .Where(s => s.PatientId == patientId)
    .GroupBy(s => s.TokenType)
    .Select(g => new { Type = g.Key, Count = g.Count() })
    .ToList();

foreach (var s in scans) {
    Console.WriteLine($"Type: {s.Type}, Count: {s.Count}");
}

var last7Days = DateTime.UtcNow.Date.AddDays(-6);
var recentScans = db.ScanHistories
    .Where(s => s.PatientId == patientId && s.ScannedAt >= last7Days)
    .GroupBy(s => new { Day = s.ScannedAt.Date, s.TokenType })
    .Select(g => new { g.Key.Day, g.Key.TokenType, Count = g.Count() })
    .ToList();

Console.WriteLine("Recent scans (Last 7 Days):");
foreach (var r in recentScans) {
    Console.WriteLine($"Day: {r.Day:yyyy-MM-dd}, Type: {r.TokenType}, Count: {r.Count}");
}

var tokens = db.QRTokens.Where(t => t.PatientId == patientId).ToList();
Console.WriteLine("QR Token AccessCounts:");
foreach (var t in tokens) {
    Console.WriteLine($"Type: {t.TokenType}, Count: {t.AccessCount}");
}
