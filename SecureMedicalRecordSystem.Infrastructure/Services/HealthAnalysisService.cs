using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.DTOs.Analysis;
using SecureMedicalRecordSystem.Core.Entities;
using SecureMedicalRecordSystem.Core.Interfaces;
using SecureMedicalRecordSystem.Infrastructure.Data;

namespace SecureMedicalRecordSystem.Infrastructure.Services;

public class HealthAnalysisService : IHealthAnalysisService
{
    private readonly ApplicationDbContext _context;
    private readonly IPrescriptionService _prescriptionService;

    public HealthAnalysisService(
        ApplicationDbContext context,
        IPrescriptionService prescriptionService)
    {
        _context = context;
        _prescriptionService = prescriptionService;
    }

    private static readonly Dictionary<string, (double Min, double Max)> ClinicalNormals = new()
    {
        { "Systolic",    (90,  120)  },
        { "Diastolic",   (60,  80)   },
        { "HeartRate",   (60,  100)  },
        { "SpO2",        (95,  100)  },
        { "Temperature", (97.0, 99.5)},
        { "BMI",         (18.5, 24.9)}
    };

    private string GenerateHumanInterpretation(string vitalName, string direction, double slope, double currentValue)
    {
        if (direction == "Baseline") return "This is your initial reading. It will serve as the baseline for tracking your progress in future visits.";
        if (direction == "Stable") return $"Your {vitalName} has remained consistent across your visits.";
        
        bool isImprovement = direction == "Improving";
        bool isSteep = Math.Abs(slope) > 0.8;

        if (isImprovement)
        {
            return isSteep 
                ? $"Your {vitalName} has been steadily improving over your recent visits."
                : $"Your {vitalName} is showing a positive gradual trend.";
        }
        else
        {
            return isSteep
                ? $"Your {vitalName} has been gradually rising — worth monitoring closely."
                : $"Your {vitalName} has shown some slight variation recently.";
        }
    }

    private string GenerateActionStep(string vitalName, string direction)
    {
        return direction switch
        {
            "Baseline" => "Continue your regular check-ins to enable trend analysis over time.",
            "Degrading" => "Bring this up at your next visit — your doctor may want to review it.",
            "Improving" => "Keep up whatever you have been doing — this trend is heading in the right direction.",
            _ => "No action needed. Continue your regular check-ins."
        };
    }

    private List<AttentionItemDto> DetectAttentionItems(
        AnalysisSummaryDto summary, 
        List<PatientHealthRecord> records, 
        List<AbnormalityPatternDto> patterns)
    {
        var items = new List<AttentionItemDto>();

        // 1. Missed Follow-up (High Priority)
        if (summary.HasMissedFollowUp && summary.NextFollowUpDate.HasValue)
        {
            items.Add(new AttentionItemDto
            {
                Title = "Missed Follow-Up Appointment",
                Description = $"You had a follow-up scheduled for {summary.NextFollowUpDate.Value:MMM dd, yyyy} that has not been recorded.",
                ActionStep = "Contact your clinic to reschedule as soon as possible.",
                Severity = "High",
                Category = "FollowUp"
            });
        }

        // 2. Degrading Trends (High/Medium)
        foreach (var trend in summary.VitalTrends.Where(t => t.Direction == "Degrading"))
        {
            items.Add(new AttentionItemDto
            {
                Title = $"{trend.VitalName} Trend Alert",
                Description = trend.HumanInterpretation,
                ActionStep = trend.ActionStep,
                Severity = Math.Abs(trend.Slope) > 1.0 ? "High" : "Medium",
                Category = "Vital"
            });
        }

        // 3. Abnormality Streaks (High)
        foreach (var p in patterns)
        {
            foreach (var s in p.Streaks.Where(x => x.ConsecutiveCount >= 3))
            {
                items.Add(new AttentionItemDto
                {
                    Title = $"Persistent {p.VitalName} Variation",
                    Description = $"Your {p.VitalName} was outside the normal range for {s.ConsecutiveCount} visits in a row.",
                    ActionStep = "Discuss this pattern with your doctor at your next visit.",
                    Severity = "High",
                    Category = "Vital"
                });
            }
        }

        // 4. Visit Gaps (Medium)
        for (int i = 1; i < records.Count; i++)
        {
            var gap = (records[i].RecordDate - records[i-1].RecordDate).TotalDays;
            if (gap > 120)
            {
                items.Add(new AttentionItemDto
                {
                    Title = "Significant Visit Gap",
                    Description = $"There is a gap of {(int)gap} days between two of your visits with no recorded data.",
                    ActionStep = "Regular check-ins help catch changes early. Try to maintain your visit schedule.",
                    Severity = "Medium",
                    Category = "Gap"
                });
                break; // Only report the largest/latest gap
            }
        }

        // 5. Baseline Reliability (Low)
        if (summary.BaselineReliabilityWarning)
        {
            items.Add(new AttentionItemDto
            {
                Title = "Limited Baseline Data",
                Description = "Your health baseline was calculated from a small number of visits. Trend comparisons may be less accurate.",
                ActionStep = "As you complete more visits, your trend analysis will become more precise.",
                Severity = "Low",
                Category = "Baseline"
            });
        }

        return items.OrderBy(i => i.Severity == "High" ? 0 : i.Severity == "Medium" ? 1 : 2).ToList();
    }

    private List<PatientHealthRecord>? _cachedRecords;
    private Guid? _cachedPatientId;
    private List<CommonLabUnit>? _cachedLabUnits;
    private List<MasterMedication>? _cachedMasterMeds;
    private PatientVitalBaseline? _cachedBaseline;

    private async Task<PatientVitalBaseline?> LoadBaselineAsync(Guid patientId)
    {
        if (_cachedPatientId == patientId && _cachedBaseline != null) return _cachedBaseline;
        _cachedBaseline = await _context.PatientVitalBaselines.FirstOrDefaultAsync(b => b.PatientId == patientId);
        return _cachedBaseline;
    }

    private async Task<List<PatientHealthRecord>> LoadRecordsAsync(Guid patientId)
    {
        if (_cachedPatientId == patientId && _cachedRecords != null) return _cachedRecords;

        _cachedRecords = await _context.PatientHealthRecords
            .Include(r => r.Patient)
            .Include(r => r.Prescriptions)
            .Include(r => r.CustomAttributes)
            .Where(r => r.PatientId == patientId && !r.IsDeleted)
            .OrderBy(r => r.RecordDate)
            .ToListAsync();

        _cachedPatientId = patientId;
        return _cachedRecords;
    }

    private async Task<List<CommonLabUnit>> LoadLabUnitsAsync()
    {
        _cachedLabUnits ??= await _context.CommonLabUnits.AsNoTracking().ToListAsync();
        return _cachedLabUnits;
    }

    private async Task<List<MasterMedication>> LoadMasterMedsAsync()
    {
        _cachedMasterMeds ??= await _context.MasterMedications.AsNoTracking().ToListAsync();
        return _cachedMasterMeds;
    }

    private double LinearRegressionSlope(List<double> values)
    {
        if (values.Count < 2) return 0.0;
        
        int n = values.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        
        for (int i = 0; i < n; i++)
        {
            sumX += i;
            sumY += values[i];
            sumXY += i * values[i];
            sumX2 += i * i;
        }
        
        double denominator = (n * sumX2) - (sumX * sumX);
        if (denominator == 0) return 0.0;
        
        return ((n * sumXY) - (sumX * sumY)) / denominator;
    }

    private double StandardDeviation(List<double> values)
    {
        if (values.Count < 2) return 0.0;
        
        double avg = values.Average();
        double sum = values.Sum(v => Math.Pow(v - avg, 2));
        return Math.Sqrt(sum / values.Count); // population std dev
    }

    private List<StabilityWindowDto> GetStabilityWindows(List<(DateTime date, double value)> points, double baseline)
    {
        var windows = new List<StabilityWindowDto>();
        if (points.Count < 2) return windows;

        double tolerance = baseline * 0.10;
        double minAllowed = baseline - tolerance;
        double maxAllowed = baseline + tolerance;

        int startIndex = -1;
        var currentBandValues = new List<double>();

        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            bool isInBand = p.value >= minAllowed && p.value <= maxAllowed;

            if (isInBand)
            {
                if (startIndex == -1) startIndex = i;
                currentBandValues.Add(p.value);
            }
            else
            {
                if (startIndex != -1)
                {
                    if (i - startIndex >= 2)
                    {
                        windows.Add(new StabilityWindowDto
                        {
                            From = points[startIndex].date,
                            To = points[i - 1].date,
                            AverageValue = currentBandValues.Average()
                        });
                    }
                    startIndex = -1;
                    currentBandValues.Clear();
                }
            }
        }

        // Check if ends in a window
        if (startIndex != -1 && points.Count - startIndex >= 2)
        {
            windows.Add(new StabilityWindowDto
            {
                From = points[startIndex].date,
                To = points[points.Count - 1].date,
                AverageValue = currentBandValues.Average()
            });
        }

        return windows;
    }

    private bool IsVitalAbnormal(string vitalName, double value, PatientVitalBaseline? baseline)
    {
        double? bValue = null;
        if (baseline != null)
        {
            if (vitalName == "Systolic") bValue = baseline.AvgSystolic;
            else if (vitalName == "Diastolic") bValue = baseline.AvgDiastolic;
            else if (vitalName == "HeartRate") bValue = baseline.AvgHeartRate;
            else if (vitalName == "SpO2") bValue = baseline.AvgSpo2;
            else if (vitalName == "Temperature") bValue = baseline.AvgTemperature;
            else if (vitalName == "BMI") bValue = baseline.AvgBmi;
        }

        if (bValue.HasValue && bValue.Value > 0)
        {
            double tol = bValue.Value * 0.20;
            return value < (bValue.Value - tol) || value > (bValue.Value + tol);
        }

        // Global fallbacks
        return vitalName switch
        {
            "Systolic" => value < 90 || value > 140,
            "Diastolic" => value < 60 || value > 90,
            "HeartRate" => value < 60 || value > 100,
            "SpO2" => value < 95,
            "Temperature" => value < 97.0 || value > 99.5,
            "BMI" => value < 18.5 || value > 29.9,
            _ => false
        };
    }

    /// <summary>Synchronous in-memory lookup using a pre-loaded CommonLabUnit dictionary.</summary>
    private static CommonLabUnit? FindLabMetadata(
        string markerName,
        IReadOnlyList<CommonLabUnit> allLabUnits)
    {
        var name = markerName.Trim().ToLower();
        return allLabUnits.FirstOrDefault(u =>
            u.MeasurementType.ToLower() == name ||
            u.MeasurementType.ToLower().Contains(name) ||
            (u.Aliases != null && u.Aliases.ToLower().Contains(name)));
    }

    /// <summary>Legacy async overload — used only by callers that still need DB access.
    /// Prefer FindLabMetadata() with a pre-loaded list wherever possible.</summary>
    private async Task<CommonLabUnit?> GetLabMetadataAsync(string markerName)
    {
        var name = markerName.Trim().ToLower();
        return await _context.CommonLabUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.MeasurementType.ToLower() == name ||
                u.MeasurementType.ToLower().Contains(name) ||
                (u.Aliases != null && u.Aliases.ToLower().Contains(name)));
    }

    private static string InterpretDelta(
        string vitalName,
        double delta,
        IReadOnlyList<CommonLabUnit> allLabUnits)
    {
        var meta = FindLabMetadata(vitalName, allLabUnits);

        if (meta != null)
        {
            return meta.ImprovingDirection switch
            {
                "Lower"   => delta < -0.05 ? "Improved" : delta > 0.05 ? "Degraded" : "Neutral",
                "Higher"  => delta > 0.05  ? "Improved" : delta < -0.05 ? "Degraded" : "Neutral",
                "InRange" when meta.NormalRangeLow.HasValue && meta.NormalRangeHigh.HasValue =>
                    "Neutral",
                _ => delta < -0.05 ? "Improved" : delta > 0.05 ? "Degraded" : "Neutral"
            };
        }

        // Fallback for standard vitals not in DB
        return vitalName.ToLower() switch
        {
            var n when n.Contains("systolic") || n.Contains("diastolic") =>
                delta < -2 ? "Improved" : delta > 2 ? "Degraded" : "Neutral",
            var n when n.Contains("egfr") || n.Contains("spo2") =>
                delta > 0.5 ? "Improved" : delta < -0.5 ? "Degraded" : "Neutral",
            _ => Math.Abs(delta) < 0.05 ? "Neutral" : delta < 0 ? "Improved" : "Degraded"
        };
    }

    /// <summary>Async overload kept for backward compatibility with any direct callers.</summary>
    private async Task<string> InterpretDeltaAsync(string vitalName, double delta)
    {
        var allLabUnits = await LoadLabUnitsAsync();
        return InterpretDelta(vitalName, delta, allLabUnits);
    }

    private Dictionary<string, List<(DateTime Date, double Value, decimal? Min, decimal? Max, bool? IsAbnormal, string? Unit, string? Section)>>
        ExtractCustomAttributes(List<PatientHealthRecord> records)
    {
        var result = new Dictionary<string, List<(DateTime Date, double Value, decimal? Min, decimal? Max, bool? IsAbnormal, string? Unit, string? Section)>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            if (record.CustomAttributes == null) continue;

            foreach (var attr in record.CustomAttributes
                .Where(a => (int)a.FieldType == 0 || (int)a.FieldType == 1)) // Numeric or parseable Text
            {
                if (!double.TryParse(attr.FieldValue, 
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var numericValue))
                    continue;

                var key = attr.FieldLabel;
                if (!result.ContainsKey(key))
                    result[key] = new List<(DateTime, double, decimal?, decimal?, bool?, string?, string?)>();

                result[key].Add((
                    record.RecordDate,
                    numericValue,
                    attr.NormalRangeMin,
                    attr.NormalRangeMax,
                    attr.IsAbnormal,
                    attr.FieldUnit,
                    attr.SectionName
                ));
            }
        }

        // Sort each attribute's data points chronologically
        foreach (var key in result.Keys)
            result[key] = result[key].OrderBy(x => x.Date).ToList();

        return result;
    }

    private bool IsCustomAttributeAbnormal(
        double value, 
        decimal? min, 
        decimal? max, 
        bool? preComputedFlag)
    {
        if (preComputedFlag.HasValue)
            return preComputedFlag.Value;

        if (min.HasValue && value < (double)min.Value) return true;
        if (max.HasValue && value > (double)max.Value) return true;
        return false;
    }

    public async Task<List<VitalTrendDto>> GetVitalTrendsAsync(Guid patientId)
    {
        // Load records + baseline + all lab metadata sequentially (EF Core context is not thread-safe)
        var records     = await LoadRecordsAsync(patientId);
        var baseline    = await LoadBaselineAsync(patientId);
        var allLabUnits = await LoadLabUnitsAsync();
        
        var vitalsMapping = new Dictionary<string, Func<PatientHealthRecord, double?>>
        {
            { "Systolic", r => r.BloodPressureSystolic },
            { "Diastolic", r => r.BloodPressureDiastolic },
            { "HeartRate", r => r.HeartRate },
            { "SpO2", r => (double?)r.SpO2 },
            { "Temperature", r => (double?)r.Temperature },
            { "BMI", r => (double?)r.BMI }
        };

        var baselineMapping = new Dictionary<string, double?>
        {
            { "Systolic", baseline?.AvgSystolic },
            { "Diastolic", baseline?.AvgDiastolic },
            { "HeartRate", baseline?.AvgHeartRate },
            { "SpO2", baseline?.AvgSpo2 },
            { "Temperature", baseline?.AvgTemperature },
            { "BMI", baseline?.AvgBmi }
        };

        var globalMedians = new Dictionary<string, double>
        {
            { "Systolic", 115 }, { "Diastolic", 75 }, { "HeartRate", 80 }, { "SpO2", 98 }, { "Temperature", 98.6 }, { "BMI", 24 }
        };

        var trends = new List<VitalTrendDto>();

        foreach (var kvp in vitalsMapping)
        {
            string vName = kvp.Key;
            var extractor = kvp.Value;

            var validPoints = records
                .Where(r => extractor(r).HasValue)
                .Select(r => (date: r.RecordDate, value: extractor(r)!.Value))
                .ToList();

            if (validPoints.Count < 1) continue;

            var values = validPoints.Select(p => p.value).ToList();
            double slope = LinearRegressionSlope(values);
            
            string direction = values.Count < 2 ? "Baseline" : "Stable";
            if (values.Count >= 2)
            {
                if (vName == "HeartRate")
                {
                    if (Math.Abs(values.Last() - values.First()) > 5) direction = slope > 0 ? "Degrading" : "Improving";
                }
                else
                {
                    if (Math.Abs(slope) < 0.1) direction = "Stable";
                    else if (slope > 0.1) direction = (vName == "SpO2") ? "Improving" : "Degrading";
                    else if (slope < -0.1) direction = (vName == "SpO2") ? "Degrading" : "Improving";
                }
            }

            double bValue = baselineMapping[vName] ?? globalMedians[vName];
            double currentVal = values.Last();

            var normalRange = ClinicalNormals.ContainsKey(vName) ? ClinicalNormals[vName] : (Min: (double?)null, Max: (double?)null);

            trends.Add(new VitalTrendDto
            {
                VitalName = vName,
                Direction = direction,
                Slope = slope,
                Volatility = StandardDeviation(values),
                CurrentValue = currentVal,
                BaselineValue = baselineMapping[vName],
                PercentChangeFromBaseline = (baselineMapping[vName].HasValue && baselineMapping[vName].Value != 0)
                    ? ((currentVal - baselineMapping[vName]!.Value) / baselineMapping[vName]!.Value) * 100 
                    : null,
                StabilityWindows = validPoints.Select(p => new StabilityWindowDto { From = p.date, To = p.date, AverageValue = p.value }).ToList(),
                NormalMin = normalRange.Min,
                NormalMax = normalRange.Max,
                HumanInterpretation = GenerateHumanInterpretation(vName, direction, slope, currentVal),
                ActionStep = GenerateActionStep(vName, direction),
                SectionName = "Vital Signs",
                VitalUnit = vName switch
                {
                    "Systolic" or "Diastolic" => "mmHg",
                    "HeartRate" => "bpm",
                    "SpO2" => "%",
                    "Temperature" => "°F",
                    "BMI" => "kg/m²",
                    _ => null
                }
            });
        }

        // --- Custom Attribute Trends ---
        var customAttributes = ExtractCustomAttributes(records);

        foreach (var (attrName, dataPoints) in customAttributes)
        {
            if (dataPoints.Count < 1) continue;

            var values = dataPoints.Select(d => d.Value).ToList();
            var slope = LinearRegressionSlope(values);
            var volatility = StandardDeviation(values);
            var current = values.Last();

            // Determine baseline: average of first 3 data points for this attribute
            var attrBaseline = dataPoints.Take(3).Average(d => d.Value);

            // Determine direction
            var min = dataPoints.Last().Min;
            var max = dataPoints.Last().Max;
            var section = dataPoints.Last().Section;
            string direction = dataPoints.Count < 2 ? "Baseline" : "Stable";

            if (values.Count >= 2)
            {
                if (Math.Abs(slope) < 0.05)
                {
                    direction = "Stable";
                }
                else if (min.HasValue && max.HasValue)
                {
                    var midpoint = ((double)min.Value + (double)max.Value) / 2.0;
                    direction = (slope > 0 && current < midpoint) || 
                                (slope < 0 && current > midpoint)
                        ? "Improving" : "Degrading";
                }
                else
                {
                    // In-memory direction lookup (no DB call — allLabUnits already loaded)
                    var meta = FindLabMetadata(attrName, allLabUnits);
                    string improvingDir = meta?.ImprovingDirection ?? "Lower";

                    direction = improvingDir switch
                    {
                        "Higher" => slope > 0.05 ? "Improving" : slope < -0.05 ? "Degrading" : "Stable",
                        "InRange" when min.HasValue && max.HasValue =>
                            // slope moving toward midpoint = improving
                            (slope > 0 && current < ((double)min.Value + (double)max.Value) / 2.0) ||
                            (slope < 0 && current > ((double)min.Value + (double)max.Value) / 2.0)
                            ? "Improving" : "Degrading",
                        _ => slope < -0.05 ? "Improving" : slope > 0.05 ? "Degrading" : "Stable"
                    };
                }
            }

            var stabilityCenter = (min.HasValue && max.HasValue)
                ? ((double)min.Value + (double)max.Value) / 2.0
                : attrBaseline;

            var trendPoints = dataPoints.Select(d => new StabilityWindowDto { From = d.Date, To = d.Date, AverageValue = d.Value }).ToList();

            var percentChange = attrBaseline != 0
                ? ((current - attrBaseline) / attrBaseline) * 100
                : (double?)null;

            trends.Add(new VitalTrendDto
            {
                VitalName = attrName,
                Direction = direction,
                Slope = slope,
                Volatility = volatility,
                CurrentValue = current,
                BaselineValue = attrBaseline,
                PercentChangeFromBaseline = percentChange,
                StabilityWindows = trendPoints,
                NormalMin = min.HasValue ? (double)min.Value : null,
                NormalMax = max.HasValue ? (double)max.Value : null,
                HumanInterpretation = GenerateHumanInterpretation(attrName, direction, slope, current),
                ActionStep = GenerateActionStep(attrName, direction),
                SectionName = section ?? "Laboratory Results",
                VitalUnit = dataPoints.Last().Unit
            });
        }

        return trends;
    }

    public async Task<List<MedicationCorrelationDto>> GetMedicationCorrelationsAsync(Guid patientId)
    {
        // Pre-load both CommonLabUnits and MasterMedications sequentially (EF Core context is not thread-safe)
        var records       = await LoadRecordsAsync(patientId);
        var allLabUnits   = await LoadLabUnitsAsync();
        var allMasterMeds = await LoadMasterMedsAsync();
        var correlations = new List<MedicationCorrelationDto>();
        
        var medMap = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in records)
        {
            foreach (var p in r.Prescriptions)
            {
                if (!medMap.ContainsKey(p.MedicationName))
                {
                    medMap[p.MedicationName] = r.RecordDate;
                }
            }
        }

        var vitalsMapping = new Dictionary<string, Func<PatientHealthRecord, double?>>
        {
            { "Systolic", r => r.BloodPressureSystolic },
            { "Diastolic", r => r.BloodPressureDiastolic },
            { "HeartRate", r => r.HeartRate },
            { "SpO2", r => (double?)r.SpO2 },
            { "BMI", r => (double?)r.BMI }
        };

        // Build lookup dictionaries from pre-loaded data (no further DB hits needed)
        var masterMedsByNameLower = allMasterMeds
            .ToDictionary(m => m.Name.ToLower(), m => m, StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in medMap)
        {
            string medName = kvp.Key;
            DateTime introducedAt = kvp.Value;

            var beforeRecords = records.Where(r => r.RecordDate < introducedAt).TakeLast(5).ToList();
            var afterRecords  = records.Where(r => r.RecordDate >= introducedAt).Take(8).ToList();

            if (afterRecords.Count > 0 && afterRecords[0].RecordDate == introducedAt)
            {
                // Previously we required at least 1 follow-up visit AFTER the introduction.
                // We're relaxing this to allow comparing the "Introduction" visit itself against the "Before" history.
                // However, we still need at least one record in the 'after' set (which includes intro).
                if (afterRecords.Count == 0) continue; 
            }

            // Look up metadata from pre-loaded dictionary instead of hitting DB again
            masterMedsByNameLower.TryGetValue(medName.ToLower(), out var medMeta);
            // Also try alias lookup if direct name lookup fails
            if (medMeta == null)
            {
                medMeta = allMasterMeds.FirstOrDefault(m =>
                    !string.IsNullOrEmpty(m.Aliases) &&
                    (System.Text.Json.JsonSerializer
                        .Deserialize<List<string>>(m.Aliases) ?? new List<string>())
                        .Any(a => a.Equals(medName, StringComparison.OrdinalIgnoreCase)));
            }

            var dto = new MedicationCorrelationDto
            {
                MedicationName = medName,
                IntroducedAt = introducedAt,
                DrugCategory = medMeta?.DrugCategory ?? "General",
                PrimaryMarkers = medMeta?.PrimaryMarkers != null
                    ? System.Text.Json.JsonSerializer
                        .Deserialize<List<string>>(medMeta.PrimaryMarkers)
                          ?? new List<string>()
                    : new List<string>()
            };

            // Population of activity metrics
            var lastRecordWithMed = records.Where(r => r.Prescriptions.Any(p => p.MedicationName.Equals(medName, StringComparison.OrdinalIgnoreCase)))
                                           .OrderByDescending(r => r.RecordDate)
                                           .FirstOrDefault();
            if (lastRecordWithMed != null)
            {
                dto.LastSeenAt = lastRecordWithMed.RecordDate;
                dto.IsCurrentlyActive = lastRecordWithMed.RecordDate == records.Last().RecordDate;
            }

            foreach (var vMap in vitalsMapping)
            {
                var beforeVals = beforeRecords.Where(r => vMap.Value(r).HasValue).Select(r => vMap.Value(r)!.Value).ToList();
                var afterVals = afterRecords.Where(r => vMap.Value(r).HasValue).Select(r => vMap.Value(r)!.Value).ToList();

                if (afterVals.Count == 0) continue;

                double avgB, avgA, delta;

                if (beforeVals.Count > 0)
                {
                    avgB = beforeVals.Average();
                    avgA = afterVals.Average();
                    delta = avgA - avgB;
                }
                else
                {
                    // No history: Use first introduction reading as baseline
                    avgB = afterVals.First();
                    avgA = afterVals.Count > 1 ? afterVals.Skip(1).Average() : avgB;
                    delta = avgA - avgB;
                }

                dto.VitalDeltas.Add(new VitalCorrelationDeltaDto
                {
                    VitalName = vMap.Key,
                    AvgBefore = avgB,
                    AvgAfter = avgA,
                    Delta = delta,
                    Interpretation = InterpretDelta(vMap.Key, delta, allLabUnits),
                    VisitsBeforeCount = beforeVals.Count,
                    VisitsAfterCount = afterVals.Count
                });
            }

            // --- Custom Attribute Correlations ---
            var allCustomAttributes = ExtractCustomAttributes(records);

            foreach (var (attrName, allPoints) in allCustomAttributes)
            {
                var beforePoints = allPoints
                    .Where(p => p.Date < introducedAt)
                    .TakeLast(5)
                    .ToList();

                var afterPoints = allPoints
                    .Where(p => p.Date >= introducedAt)
                    .Take(6)
                    .ToList();

                // Skip if we have no after-data at all — nothing to show
                if (afterPoints.Count == 0) continue;

                double avgBefore, avgAfter, delta;

                if (beforePoints.Count > 0)
                {
                    // Normal case: data exists both before and after
                    avgBefore = beforePoints.Average(p => p.Value);
                    avgAfter  = afterPoints.Average(p => p.Value);
                    delta     = avgAfter - avgBefore;
                }
                else
                {
                    // Lab was only recorded after medication – use first post-intro
                    // reading as the "before" baseline and the rest as "after"
                    avgBefore = afterPoints.First().Value;
                    avgAfter  = afterPoints.Count > 1
                        ? afterPoints.Skip(1).Average(p => p.Value)
                        : afterPoints.First().Value;
                    delta = avgAfter - avgBefore;
                }

                // For custom attributes: trending toward normal range midpoint = Improved
                var min = allPoints.Last().Min;
                var max = allPoints.Last().Max;
                string interpretation;

                if (min.HasValue && max.HasValue)
                {
                    var midpoint = ((double)min.Value + (double)max.Value) / 2.0;
                    var beforeDistanceFromMid = Math.Abs(avgBefore - midpoint);
                    var afterDistanceFromMid  = Math.Abs(avgAfter  - midpoint);
                    interpretation = afterDistanceFromMid < beforeDistanceFromMid
                        ? "Improved"
                        : afterDistanceFromMid > beforeDistanceFromMid
                            ? "Degraded"
                            : "Neutral";
                }
                else
                {
                    interpretation = InterpretDelta(attrName, delta, allLabUnits); // sync, no DB hit
                }

                dto.VitalDeltas.Add(new VitalCorrelationDeltaDto
                {
                    VitalName = attrName,
                    AvgBefore = Math.Round(avgBefore, 2),
                    AvgAfter  = Math.Round(avgAfter, 2),
                    Delta     = Math.Round(delta, 2),
                    Interpretation    = interpretation,
                    VisitsBeforeCount = beforePoints.Count,
                    VisitsAfterCount  = afterPoints.Count
                });
            }


            // Promote PrimaryMarkers to top of the list
            if (dto.PrimaryMarkers.Count > 0)
            {
                dto.VitalDeltas = dto.VitalDeltas
                    .OrderBy(d => dto.PrimaryMarkers
                        .Contains(d.VitalName,
                            StringComparer.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(d => d.VitalName)
                    .ToList();
            }

            if (dto.VitalDeltas.Count > 0)
                correlations.Add(dto);
        }

        return correlations;
    }

    public async Task<List<AbnormalityPatternDto>> GetAbnormalityPatternsAsync(Guid patientId)
    {
        var records = await LoadRecordsAsync(patientId);
        var baseline = await LoadBaselineAsync(patientId);
        var patterns = new List<AbnormalityPatternDto>();

        var vitalsMapping = new Dictionary<string, Func<PatientHealthRecord, double?>>
        {
            { "Systolic", r => r.BloodPressureSystolic },
            { "Diastolic", r => r.BloodPressureDiastolic },
            { "HeartRate", r => r.HeartRate },
            { "SpO2", r => (double?)r.SpO2 },
            { "Temperature", r => (double?)r.Temperature },
            { "BMI", r => (double?)r.BMI }
        };

        foreach (var kvp in vitalsMapping)
        {
            string vName = kvp.Key;
            var extractor = kvp.Value;

            var pattern = new AbnormalityPatternDto { VitalName = vName };
            var currentStreakValues = new List<double>();
            int currentStreakCount = 0;
            DateTime? streakStart = null;
            DateTime? streakEnd = null;

            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                var val = extractor(r);
                if (!val.HasValue) continue;

                bool isAbn = IsVitalAbnormal(vName, val.Value, baseline);

                if (isAbn)
                {
                    if (currentStreakCount == 0) streakStart = r.RecordDate;
                    currentStreakValues.Add(val.Value);
                    streakEnd = r.RecordDate;
                    currentStreakCount++;
                }
                else
                {
                    if (currentStreakCount >= 2)
                    {
                        pattern.Streaks.Add(new AbnormalStreakDto
                        {
                            From = streakStart!.Value,
                            To = streakEnd!.Value,
                            ConsecutiveCount = currentStreakCount,
                            Values = new List<double>(currentStreakValues)
                        });
                        pattern.MaxConsecutiveAbnormalVisits = Math.Max(pattern.MaxConsecutiveAbnormalVisits, currentStreakCount);
                    }
                    currentStreakCount = 0;
                    currentStreakValues.Clear();
                }
            }

            if (currentStreakCount >= 2)
            {
                pattern.Streaks.Add(new AbnormalStreakDto
                {
                    From = streakStart!.Value,
                    To = streakEnd!.Value,
                    ConsecutiveCount = currentStreakCount,
                    Values = new List<double>(currentStreakValues)
                });
                pattern.MaxConsecutiveAbnormalVisits = Math.Max(pattern.MaxConsecutiveAbnormalVisits, currentStreakCount);
            }

            if (pattern.Streaks.Count > 0)
                patterns.Add(pattern);
        }

        // --- Custom Attribute Abnormality Patterns ---
        var customAttributes = ExtractCustomAttributes(records);

        foreach (var (attrName, dataPoints) in customAttributes)
        {
            if (dataPoints.Count < 2) continue;

            var streaks = new List<AbnormalStreakDto>();
            var currentStreak = new List<(DateTime Date, double Value)>();

            foreach (var point in dataPoints)
            {
                var isAbnormal = IsCustomAttributeAbnormal(
                    point.Value, point.Min, point.Max, point.IsAbnormal);

                if (isAbnormal)
                {
                    currentStreak.Add((point.Date, point.Value));
                }
                else
                {
                    if (currentStreak.Count >= 2)
                    {
                        streaks.Add(new AbnormalStreakDto
                        {
                            From = currentStreak.First().Date,
                            To = currentStreak.Last().Date,
                            ConsecutiveCount = currentStreak.Count,
                            Values = currentStreak.Select(x => x.Value).ToList()
                        });
                    }
                    currentStreak.Clear();
                }
            }

            // Catch streak that runs to the end
            if (currentStreak.Count >= 2)
            {
                streaks.Add(new AbnormalStreakDto
                {
                    From = currentStreak.First().Date,
                    To = currentStreak.Last().Date,
                    ConsecutiveCount = currentStreak.Count,
                    Values = currentStreak.Select(x => x.Value).ToList()
                });
            }

            if (streaks.Count == 0) continue;

            patterns.Add(new AbnormalityPatternDto
            {
                VitalName = attrName,
                MaxConsecutiveAbnormalVisits = streaks.Max(s => s.ConsecutiveCount),
                Streaks = streaks
            });
        }

        return patterns;
    }

    public async Task<StabilityTimelineDto> GetStabilityTimelineAsync(Guid patientId)
    {
        var records = await LoadRecordsAsync(patientId);
        var baseline = await LoadBaselineAsync(patientId);

        var dto = new StabilityTimelineDto();
        if (records.Count == 0) return dto;

        var groupedByQuarter = records.GroupBy(r => 
        {
            int q = (r.RecordDate.Month - 1) / 3 + 1;
            return $"Q{q} {r.RecordDate.Year}";
        }).ToList();

        var vitalsMapping = new Dictionary<string, Func<PatientHealthRecord, double?>>
        {
            { "Systolic", r => r.BloodPressureSystolic },
            { "Diastolic", r => r.BloodPressureDiastolic },
            { "HeartRate", r => r.HeartRate },
            { "SpO2", r => (double?)r.SpO2 },
            { "Temperature", r => (double?)r.Temperature },
            { "BMI", r => (double?)r.BMI }
        };

        DateTime? previousQuarterLastVisit = null;

        foreach (var g in groupedByQuarter)
        {
            var qRecords = g.OrderBy(r => r.RecordDate).ToList();
            int totalVisits = qRecords.Count;
            int abnCount = 0;
            bool hasLongGap = false;

            if (previousQuarterLastVisit.HasValue && (qRecords.First().RecordDate - previousQuarterLastVisit.Value).TotalDays > 90)
            {
                hasLongGap = true;
            }

            for (int i = 0; i < qRecords.Count; i++)
            {
                var r = qRecords[i];
                if (i > 0 && (r.RecordDate - qRecords[i - 1].RecordDate).TotalDays > 90)
                {
                    hasLongGap = true;
                }

                foreach (var kvp in vitalsMapping)
                {
                    var val = kvp.Value(r);
                    if (val.HasValue && IsVitalAbnormal(kvp.Key, val.Value, baseline))
                    {
                        abnCount++;
                    }
                }
            }

            // After existing standard vital abnormality counting, add:
            var customAttributes = ExtractCustomAttributes(qRecords);

            foreach (var (attrName, dataPoints) in customAttributes)
            {
                foreach (var point in dataPoints)
                {
                    if (IsCustomAttributeAbnormal(
                        point.Value, point.Min, point.Max, point.IsAbnormal))
                    {
                        abnCount++;
                    }
                }
            }

            double score = 100.0 - (abnCount * 10.0);
            if (hasLongGap) score -= 15.0;
            if (score < 0) score = 0;

            string interp = score >= 85 ? "Excellent" : (score >= 65 ? "Good" : (score >= 40 ? "Fair" : "Poor"));

            dto.Quarters.Add(new QuarterlyStabilityDto
            {
                Quarter = g.Key,
                TotalVisits = totalVisits,
                AbnormalReadingCount = abnCount,
                HasLongGap = hasLongGap,
                StabilityScore = score,
                ScoreInterpretation = interp
            });

            previousQuarterLastVisit = qRecords.Last().RecordDate;
        }

        // To chronological order, let's just rely on the existing grouping since records are sorted
        return dto;
    }

    public async Task<AnalysisSummaryDto> GetAnalysisSummaryAsync(Guid patientId)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == patientId);
        if (patient == null) return new AnalysisSummaryDto();

        var records = await LoadRecordsAsync(patientId);
        if (records.Count == 0) return new AnalysisSummaryDto { PatientId = patientId };

        // Load dependencies sequentially (EF Core context is not thread-safe for parallel reads)
        var trends = await GetVitalTrendsAsync(patientId);
        var timeline = await GetStabilityTimelineAsync(patientId);
        var patterns = await GetAbnormalityPatternsAsync(patientId);
        var correlations = await GetMedicationCorrelationsAsync(patientId);
        var baseline = await LoadBaselineAsync(patientId);

        var latestRecord = records.Last();

        var dto = new AnalysisSummaryDto
        {
            PatientId = patientId,
            PatientAge = (int)((DateTime.UtcNow - patient.DateOfBirth).TotalDays / 365),
            Gender = patient.Gender,
            BloodType = patient.BloodType,
            TotalVisits = records.Count,
            FirstVisit = records.First().RecordDate,
            LastVisit = latestRecord.RecordDate,
            VitalTrends = trends,
            LatestStabilityScore = timeline.Quarters.LastOrDefault()?.StabilityScore ?? 0.0,
            HasMissedFollowUp = latestRecord.FollowUpScheduled
                                && latestRecord.FollowUpDate.HasValue
                                && latestRecord.FollowUpDate.Value < DateTime.UtcNow
                                && !records.Any(r => r.RecordDate > latestRecord.FollowUpDate.Value),
            NextFollowUpDate = latestRecord.FollowUpDate,
            BaselineReliabilityWarning = baseline != null && baseline.RecordsUsedForBaseline <= 3
        };

        int improvingCount = trends.Count(t => t.Direction == "Improving");
        int degradingCount = trends.Count(t => t.Direction == "Degrading");

        if (improvingCount > degradingCount) dto.OverallHealthTrend = "Improving";
        else if (degradingCount > improvingCount) dto.OverallHealthTrend = "Degrading";
        else if (improvingCount == 0 && degradingCount == 0) dto.OverallHealthTrend = "Stable";
        else dto.OverallHealthTrend = "Mixed";

        if (latestRecord.Prescriptions.Any())
        {
            dto.ActiveMedications = latestRecord.Prescriptions.Select(p => p.MedicationName).Distinct().ToList();
        }

        foreach (var t in trends.Where(x => Math.Abs(x.Slope) > 0.5))
        {
            dto.KeyInsights.Add($"Marker {t.VitalName} shows significant trend slope ({t.Slope:F2}).");
        }

        foreach (var p in patterns)
        {
            foreach (var s in p.Streaks.Where(x => x.ConsecutiveCount >= 3))
            {
                dto.KeyInsights.Add($"Significant streak of {s.ConsecutiveCount} abnormal {p.VitalName} readings observed from {s.From:MMM yyyy} to {s.To:MMM yyyy}.");
            }
        }

        // --- Custom Attribute Insights ---
        var customAttributes = ExtractCustomAttributes(records);

        foreach (var (attrName, dataPoints) in customAttributes)
        {
            if (dataPoints.Count < 2) continue;

            var values = dataPoints.Select(d => d.Value).ToList();
            var slope = LinearRegressionSlope(values);

            if (Math.Abs(slope) > 0.5)
            {
                dto.KeyInsights.Add($"Marker {attrName} shows significant trend slope ({slope:F2}).");
            }

            // Check if latest value is abnormal
            var latestAttr = dataPoints.Last();
            var isLatestAbnormal = IsCustomAttributeAbnormal(
                latestAttr.Value, latestAttr.Min, latestAttr.Max, latestAttr.IsAbnormal);

            if (isLatestAbnormal)
            {
                var unit = latestAttr.Unit ?? string.Empty;
                dto.KeyInsights.Add($"{attrName} latest reading ({latestAttr.Value:F2} {unit}) is outside the normal range.");
            }
        }

        foreach (var c in correlations.Where(x => (DateTime.UtcNow - x.IntroducedAt).TotalDays <= 180))
        {
            dto.KeyInsights.Add($"Medication {c.MedicationName} was recently introduced ({c.IntroducedAt:MMM dd, yyyy}).");
        }

        if (dto.LatestStabilityScore < 40)
        {
            dto.KeyInsights.Add("Recent health stability is Poor — review recommended.");
        }

        // --- Priority Attention Integration ---
        dto.ItemsNeedingAttention = DetectAttentionItems(dto, records, patterns);

        return dto;
    }
}
