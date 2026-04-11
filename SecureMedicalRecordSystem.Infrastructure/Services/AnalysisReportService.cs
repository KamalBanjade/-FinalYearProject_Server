using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SecureMedicalRecordSystem.Core.DTOs.Analysis;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class AnalysisReportService : IAnalysisReportService
{
    private readonly IHealthAnalysisService _analysisService;
    private readonly ITigrisStorageService _tigrisService;
    private readonly IEncryptionService _encryptionService;
    private readonly ApplicationDbContext _context;

    public AnalysisReportService(
        IHealthAnalysisService analysisService,
        ITigrisStorageService tigrisService,
        IEncryptionService encryptionService,
        ApplicationDbContext context)
    {
        _analysisService = analysisService;
        _tigrisService = tigrisService;
        _encryptionService = encryptionService;
        _context = context;
    }

    public async Task<AnalysisReport> GenerateAndStoreReportAsync(Guid patientId, Guid doctorId, string patientFullName)
    {
        // Step A — Fetch analysis data
        var summary = await _analysisService.GetAnalysisSummaryAsync(patientId);
        var trends = await _analysisService.GetVitalTrendsAsync(patientId);
        var correlations = await _analysisService.GetMedicationCorrelationsAsync(patientId);
        var patterns = await _analysisService.GetAbnormalityPatternsAsync(patientId);
        var timeline = await _analysisService.GetStabilityTimelineAsync(patientId);

        // Step B — Generate PDF bytes using QuestPDF
        var pdfBytes = GeneratePdfBytes(patientFullName, summary, trends, correlations, patterns, timeline);

        // Step C — Encrypt
        var encryptedBytes = _encryptionService.EncryptBytes(pdfBytes);

        // Step D — Upload to Tigris
        var objectKey = $"analysis-reports/{patientId}/{Guid.NewGuid()}.enc";
        using var stream = new MemoryStream(encryptedBytes);
        await _tigrisService.UploadFileAsync(stream, objectKey, "application/pdf", $"Analysis_{DateTime.UtcNow.Ticks}.pdf");

        // Step E — Persist AnalysisReport entity
        var report = new AnalysisReport
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            GeneratedByDoctorId = doctorId,
            GeneratedAt = DateTime.UtcNow,
            TigrisObjectKey = objectKey,
            ReportTitle = $"Health Analysis Report – {patientFullName} – {DateTime.UtcNow:MMMM yyyy}",
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = doctorId.ToString()
        };
        _context.AnalysisReports.Add(report);
        await _context.SaveChangesAsync();
        return report;
    }

    public async Task<(Stream decryptedStream, string fileName)> DownloadReportAsync(Guid reportId)
    {
        var report = await _context.AnalysisReports
            .FirstOrDefaultAsync(r => r.Id == reportId && !r.IsDeleted && r.IsAvailable)
            ?? throw new KeyNotFoundException("Report not found.");

        var encryptedStream = await _tigrisService.OpenDownloadStreamAsync(report.TigrisObjectKey);
        using var ms = new MemoryStream();
        await encryptedStream.CopyToAsync(ms);
        var decryptedBytes = _encryptionService.DecryptBytes(ms.ToArray());
        var fileName = $"{report.ReportTitle}.pdf";
        return (new MemoryStream(decryptedBytes), fileName);
    }

    public async Task<List<AnalysisReport>> GetReportsForPatientAsync(Guid patientId)
    {
        return await _context.AnalysisReports
            .Where(r => r.PatientId == patientId && !r.IsDeleted && r.IsAvailable)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync();
    }

    private byte[] GeneratePdfBytes(
        string patientFullName,
        AnalysisSummaryDto summary,
        List<VitalTrendDto> trends,
        List<MedicationCorrelationDto> correlations,
        List<AbnormalityPatternDto> patterns,
        StabilityTimelineDto timeline)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(40);
                page.MarginBottom(40);
                page.MarginLeft(50);
                page.MarginRight(50);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, patientFullName, summary, trends, correlations, patterns, timeline));
                page.Footer().Element(ComposeFooter);
            });
        })
        .GeneratePdf();
    }

    private void ComposeHeader(IContainer container)
    {
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");
        // Fallback to project source path if not in bin
        if (!File.Exists(logoPath))
        {
            logoPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SecureMedicalRecordSystem.Infrastructure", "Assets", "logo.png");
        }

        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                if (File.Exists(logoPath))
                {
                    row.RelativeItem().Column(c => {
                        c.Item().Height(40).Image(logoPath, ImageScaling.FitHeight);
                    });
                }
                else
                {
                    row.RelativeItem().Text("QR Medical System").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                }

                row.RelativeItem().AlignRight().Column(c =>
                {
                    c.Item().Text("CLINICAL HEALTH RECORD").FontSize(24).ExtraBold().FontColor(Colors.Blue.Medium);
                    c.Item().Text(x => {
                        x.AlignRight();
                        x.Span("Generated on: ").FontSize(10).FontColor(Colors.Grey.Medium);
                        x.Span(DateTime.UtcNow.ToString("dd MMMM yyyy HH:mm")).FontSize(10).FontColor(Colors.Grey.Medium);
                    });
                });
            });
            column.Item().PaddingTop(5).LineHorizontal(1.5f).LineColor(Colors.Grey.Medium);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Page ");
            x.CurrentPageNumber();
            x.Span(" of ");
            x.TotalPages();
        });
    }

    private void ComposeContent(
        IContainer container, 
        string patientFullName,
        AnalysisSummaryDto summary,
        List<VitalTrendDto> trends,
        List<MedicationCorrelationDto> correlations,
        List<AbnormalityPatternDto> patterns,
        StabilityTimelineDto timeline)
    {
        container.PaddingVertical(10).Column(column =>
        {
            // Section 1: Cover Block
            column.Item().Text("Health Analysis Report").FontSize(20).Bold();
            column.Item().Text(patientFullName).FontSize(14);
            column.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Medium);
            column.Item().Height(20);

            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"Age: {summary.PatientAge}");
                row.RelativeItem().Text($"Gender: {summary.Gender}");
                row.RelativeItem().Text($"Blood Type: {summary.BloodType ?? "Unknown"}");
            });
            column.Item().Height(10);

            column.Item().Text(t =>
            {
                t.Span("Overall Health Trend: ").Bold();
                t.Span(summary.OverallHealthTrend);
            });
            column.Item().Text(t =>
            {
                t.Span("Latest Stability Score: ").Bold();
                t.Span($"{summary.LatestStabilityScore:F0}/100");
            });
            column.Item().Height(20);

            // Section 2: Key Insights
            column.Item().Text("Key Insights").FontSize(13).Bold();
            column.Item().PaddingBottom(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            if (summary.KeyInsights == null || !summary.KeyInsights.Any())
            {
                column.Item().Text("No significant insights at this time.").Italic().FontColor(Colors.Grey.Medium);
            }
            else
            {
                foreach (var insight in summary.KeyInsights)
                {
                    column.Item().Text($"• {insight}").FontSize(10);
                }
            }
            column.Item().Height(15);

            // Section 3: Active Medications
            column.Item().Text("Active Medications").FontSize(13).Bold();
            column.Item().PaddingBottom(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            if (summary.ActiveMedications == null || !summary.ActiveMedications.Any())
            {
                column.Item().Text("None recorded.").Italic().FontColor(Colors.Grey.Medium);
            }
            else
            {
                column.Item().Text(string.Join(", ", summary.ActiveMedications)).FontSize(10);
            }
            column.Item().Height(15);

            // Section 4: Vital Trends Table
            column.Item().Text("Vital Trends").FontSize(13).Bold();
            column.Item().PaddingBottom(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten4).Padding(2).Text("Vital").Bold();
                    header.Cell().Background(Colors.Grey.Lighten4).Padding(2).Text("Direction").Bold();
                    header.Cell().Background(Colors.Grey.Lighten4).Padding(2).Text("Current Value").Bold();
                    header.Cell().Background(Colors.Grey.Lighten4).Padding(2).Text("Baseline").Bold();
                    header.Cell().Background(Colors.Grey.Lighten4).Padding(2).Text("Change").Bold();
                    header.Cell().Background(Colors.Grey.Lighten4).Padding(2).Text("Volatility").Bold();
                });

                int rowIndex = 0;
                foreach (var trend in trends ?? new List<VitalTrendDto>())
                {
                    string bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                    
                    string dirText = trend.Direction == "Improving" ? "↑ Improving" :
                                     trend.Direction == "Degrading" ? "↓ Degrading" : "→ Stable";
                                     
                    string changeText = trend.PercentChangeFromBaseline.HasValue 
                        ? (trend.PercentChangeFromBaseline > 0 ? $"+{trend.PercentChangeFromBaseline:F1}%" : $"{trend.PercentChangeFromBaseline:F1}%") 
                        : "N/A";

                    table.Cell().Background(bgColor).Padding(2).Text(trend.VitalName);
                    table.Cell().Background(bgColor).Padding(2).Text(dirText);
                    table.Cell().Background(bgColor).Padding(2).Text(trend.CurrentValue?.ToString("F1") ?? "N/A");
                    table.Cell().Background(bgColor).Padding(2).Text(trend.BaselineValue?.ToString("F1") ?? "N/A");
                    table.Cell().Background(bgColor).Padding(2).Text(changeText);
                    table.Cell().Background(bgColor).Padding(2).Text(trend.Volatility.ToString("F2"));
                    
                    rowIndex++;
                }
            });
            column.Item().Height(15);

            // Section 5: Medication Correlations
            column.Item().Text("Medication Correlation Analysis").FontSize(13).Bold();
            column.Item().PaddingBottom(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            if (correlations == null || !correlations.Any())
            {
                column.Item().Text("No medication correlation data available.").Italic().FontColor(Colors.Grey.Medium);
            }
            else
            {
                foreach (var med in correlations)
                {
                    column.Item().Text($"{med.MedicationName.ToUpper()} (Introduced: {med.IntroducedAt:dd MMM yyyy})").Bold().FontSize(11);
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Padding(2).Text("Vital").Bold();
                            h.Cell().Padding(2).Text("Avg Before").Bold();
                            h.Cell().Padding(2).Text("Avg After").Bold();
                            h.Cell().Padding(2).Text("Delta").Bold();
                            h.Cell().Padding(2).Text("Effect").Bold();
                        });
                        foreach (var delta in med.VitalDeltas)
                        {
                            string deltaStr = delta.Delta > 0 ? $"+{delta.Delta:F1}" : $"{delta.Delta:F1}";
                            table.Cell().Padding(2).Text(delta.VitalName);
                            table.Cell().Padding(2).Text(delta.AvgBefore.ToString("F1"));
                            table.Cell().Padding(2).Text(delta.AvgAfter.ToString("F1"));
                            table.Cell().Padding(2).Text(deltaStr);
                            table.Cell().Padding(2).Text(delta.Interpretation);
                        }
                    });
                    column.Item().Height(8);
                }
            }
            column.Item().Height(15);

            // Section 6: Abnormality Patterns
            column.Item().Text("Abnormality Patterns").FontSize(13).Bold();
            column.Item().PaddingBottom(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            if (patterns == null || !patterns.Any())
            {
                column.Item().Text("No significant abnormality streaks detected.").Italic().FontColor(Colors.Grey.Medium);
            }
            else
            {
                foreach (var pattern in patterns)
                {
                    column.Item().Text($"{pattern.VitalName} — Longest streak: {pattern.MaxConsecutiveAbnormalVisits} visits").Bold();
                    foreach (var streak in pattern.Streaks)
                    {
                        column.Item().Text($"  From {streak.From:dd MMM yyyy} to {streak.To:dd MMM yyyy} ({streak.ConsecutiveCount} consecutive visits)");
                    }
                }
            }
            column.Item().Height(15);

            // Section 7: Stability Timeline Table
            column.Item().Text("Quarterly Stability Timeline").FontSize(13).Bold();
            column.Item().PaddingBottom(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            if (timeline == null || timeline.Quarters == null || !timeline.Quarters.Any())
            {
                column.Item().Text("Insufficient visit history.").Italic().FontColor(Colors.Grey.Medium);
            }
            else
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Background(Colors.Grey.Lighten4).Padding(2).Text("Quarter").Bold();
                        h.Cell().Background(Colors.Grey.Lighten4).Padding(2).Text("Score").Bold();
                        h.Cell().Background(Colors.Grey.Lighten4).Padding(2).Text("Interpretation").Bold();
                        h.Cell().Background(Colors.Grey.Lighten4).Padding(2).Text("Visits").Bold();
                        h.Cell().Background(Colors.Grey.Lighten4).Padding(2).Text("Abnormal Readings").Bold();
                        h.Cell().Background(Colors.Grey.Lighten4).Padding(2).Text("Long Gap").Bold();
                    });

                    int rIdx = 0;
                    foreach (var q in timeline.Quarters)
                    {
                        string bColor = rIdx % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                        table.Cell().Background(bColor).Padding(2).Text(q.Quarter);
                        table.Cell().Background(bColor).Padding(2).Text($"{q.StabilityScore:F0}/100");
                        table.Cell().Background(bColor).Padding(2).Text(q.ScoreInterpretation);
                        table.Cell().Background(bColor).Padding(2).Text(q.TotalVisits.ToString());
                        table.Cell().Background(bColor).Padding(2).Text(q.AbnormalReadingCount.ToString());
                        table.Cell().Background(bColor).Padding(2).Text(q.HasLongGap ? "Yes" : "No");
                        rIdx++;
                    }
                });
            }

            column.Item().Height(30);
            column.Item().AlignCenter().Text("This report was auto-generated by the QR Medical Record System. It is intended for licensed medical professionals only.")
                .FontSize(9).Italic().FontColor(Colors.Grey.Medium);
        });
    }
}
