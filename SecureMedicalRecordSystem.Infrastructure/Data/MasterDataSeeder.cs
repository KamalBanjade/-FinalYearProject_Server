using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.Entities;

namespace SecureMedicalRecordSystem.Infrastructure.Data;

public static class MasterDataSeeder
{
    public static async Task SeedCommonLabUnitsAsync(ApplicationDbContext context)
    {
        // Force re-seed by clearing existing records
        if (await context.CommonLabUnits.AnyAsync())
        {
            context.CommonLabUnits.RemoveRange(context.CommonLabUnits);
            await context.SaveChangesAsync();
        }

        var units = new List<CommonLabUnit>();

        // ─────────────────────────────────────────────────────────────────
        // 1. DIABETES MONITORING
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "Blood Glucose (Fasting)",
                CommonUnits     = "[\"mg/dL\", \"mmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 70,
                NormalRangeHigh = 99,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"fasting glucose\", \"FBG\", \"FPG\"]",
                Category        = "Diabetes Monitoring"
            },
            new CommonLabUnit {
                MeasurementType = "Blood Glucose (Postprandial)",
                CommonUnits     = "[\"mg/dL\", \"mmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 70,
                NormalRangeHigh = 139,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"2-hour postprandial\", \"PPG\", \"post-meal glucose\"]",
                Category        = "Diabetes Monitoring"
            },
            new CommonLabUnit {
                MeasurementType = "Blood Glucose (Random)",
                CommonUnits     = "[\"mg/dL\", \"mmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 70,
                NormalRangeHigh = 200,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"random blood sugar\", \"RBS\"]",
                Category        = "Diabetes Monitoring"
            },
            new CommonLabUnit {
                MeasurementType = "HbA1c",
                CommonUnits     = "[\"%\", \"mmol/mol\"]",
                DefaultUnit     = "%",
                NormalRangeLow  = 4.0m,
                NormalRangeHigh = 5.6m,
                NormalRangeUnit = "%",
                Aliases         = "[\"A1c\", \"glycated hemoglobin\", \"glycosylated hemoglobin\"]",
                Category        = "Diabetes Monitoring"
            },
            new CommonLabUnit {
                MeasurementType = "Insulin Level (Fasting)",
                CommonUnits     = "[\"µIU/mL\", \"pmol/L\"]",
                DefaultUnit     = "µIU/mL",
                NormalRangeLow  = 2.6m,
                NormalRangeHigh = 24.9m,
                NormalRangeUnit = "µIU/mL",
                Aliases         = "[\"fasting insulin\", \"serum insulin\"]",
                Category        = "Diabetes Monitoring"
            },
            new CommonLabUnit {
                MeasurementType = "C-Peptide",
                CommonUnits     = "[\"ng/mL\", \"pmol/L\"]",
                DefaultUnit     = "ng/mL",
                NormalRangeLow  = 0.5m,
                NormalRangeHigh = 2.0m,
                NormalRangeUnit = "ng/mL",
                Aliases         = "[\"connecting peptide\"]",
                Category        = "Diabetes Monitoring"
            },
            new CommonLabUnit {
                MeasurementType = "HOMA-IR (Insulin Resistance Index)",
                CommonUnits     = "[\"\"]",
                DefaultUnit     = "",
                NormalRangeLow  = 0.5m,
                NormalRangeHigh = 2.5m,
                NormalRangeUnit = "",
                Aliases         = "[\"homeostatic model assessment\", \"insulin resistance\"]",
                Category        = "Diabetes Monitoring"
            },
            new CommonLabUnit {
                MeasurementType = "Fructosamine",
                CommonUnits     = "[\"µmol/L\"]",
                DefaultUnit     = "µmol/L",
                NormalRangeLow  = 200,
                NormalRangeHigh = 285,
                NormalRangeUnit = "µmol/L",
                Aliases         = "[\"glycated albumin\"]",
                Category        = "Diabetes Monitoring"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 2. COMPLETE BLOOD COUNT (CBC)
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "Hemoglobin",
                CommonUnits     = "[\"g/dL\", \"g/L\"]",
                DefaultUnit     = "g/dL",
                NormalRangeLow  = 12.0m,
                NormalRangeHigh = 17.5m,
                NormalRangeUnit = "g/dL",
                Aliases         = "[\"Hgb\", \"Hb\"]",
                Category        = "Complete Blood Count"
            },
            new CommonLabUnit {
                MeasurementType = "Hematocrit",
                CommonUnits     = "[\"%\", \"L/L\"]",
                DefaultUnit     = "%",
                NormalRangeLow  = 36.0m,
                NormalRangeHigh = 50.0m,
                NormalRangeUnit = "%",
                Aliases         = "[\"Hct\", \"packed cell volume\", \"PCV\"]",
                Category        = "Complete Blood Count"
            },
            new CommonLabUnit {
                MeasurementType = "RBC Count",
                CommonUnits     = "[\"x10⁶/µL\", \"x10¹²/L\"]",
                DefaultUnit     = "x10⁶/µL",
                NormalRangeLow  = 4.2m,
                NormalRangeHigh = 5.9m,
                NormalRangeUnit = "x10⁶/µL",
                Aliases         = "[\"red blood cell count\", \"erythrocyte count\"]",
                Category        = "Complete Blood Count"
            },
            new CommonLabUnit {
                MeasurementType = "WBC Count",
                CommonUnits     = "[\"x10³/µL\", \"x10⁹/L\"]",
                DefaultUnit     = "x10³/µL",
                NormalRangeLow  = 4.5m,
                NormalRangeHigh = 11.0m,
                NormalRangeUnit = "x10³/µL",
                Aliases         = "[\"white blood cell count\", \"leukocyte count\"]",
                Category        = "Complete Blood Count"
            },
            new CommonLabUnit {
                MeasurementType = "Platelet Count",
                CommonUnits     = "[\"x10³/µL\", \"x10⁹/L\"]",
                DefaultUnit     = "x10³/µL",
                NormalRangeLow  = 150,
                NormalRangeHigh = 450,
                NormalRangeUnit = "x10³/µL",
                Aliases         = "[\"PLT\", \"thrombocyte count\"]",
                Category        = "Complete Blood Count"
            },
            new CommonLabUnit {
                MeasurementType = "MCV (Mean Corpuscular Volume)",
                CommonUnits     = "[\"fL\"]",
                DefaultUnit     = "fL",
                NormalRangeLow  = 80,
                NormalRangeHigh = 100,
                NormalRangeUnit = "fL",
                Aliases         = "[\"mean cell volume\"]",
                Category        = "Complete Blood Count"
            },
            new CommonLabUnit {
                MeasurementType = "MCH (Mean Corpuscular Hemoglobin)",
                CommonUnits     = "[\"pg\"]",
                DefaultUnit     = "pg",
                NormalRangeLow  = 27,
                NormalRangeHigh = 33,
                NormalRangeUnit = "pg",
                Aliases         = "[\"mean cell hemoglobin\"]",
                Category        = "Complete Blood Count"
            },
            new CommonLabUnit {
                MeasurementType = "MCHC (Mean Corpuscular Hemoglobin Concentration)",
                CommonUnits     = "[\"g/dL\"]",
                DefaultUnit     = "g/dL",
                NormalRangeLow  = 33,
                NormalRangeHigh = 36,
                NormalRangeUnit = "g/dL",
                Category        = "Complete Blood Count"
            },
            new CommonLabUnit {
                MeasurementType = "RDW (Red Cell Distribution Width)",
                CommonUnits     = "[\"%\"]",
                DefaultUnit     = "%",
                NormalRangeLow  = 11.5m,
                NormalRangeHigh = 14.5m,
                NormalRangeUnit = "%",
                Aliases         = "[\"red blood cell distribution width\"]",
                Category        = "Complete Blood Count"
            },
            new CommonLabUnit {
                MeasurementType = "Neutrophils (%)",
                CommonUnits     = "[\"%\"]",
                DefaultUnit     = "%",
                NormalRangeLow  = 50,
                NormalRangeHigh = 70,
                NormalRangeUnit = "%",
                Category        = "Complete Blood Count"
            },
            new CommonLabUnit {
                MeasurementType = "Lymphocytes (%)",
                CommonUnits     = "[\"%\"]",
                DefaultUnit     = "%",
                NormalRangeLow  = 20,
                NormalRangeHigh = 40,
                NormalRangeUnit = "%",
                Category        = "Complete Blood Count"
            },
            new CommonLabUnit {
                MeasurementType = "Monocytes (%)",
                CommonUnits     = "[\"%\"]",
                DefaultUnit     = "%",
                NormalRangeLow  = 2,
                NormalRangeHigh = 8,
                NormalRangeUnit = "%",
                Category        = "Complete Blood Count"
            },
            new CommonLabUnit {
                MeasurementType = "Eosinophils (%)",
                CommonUnits     = "[\"%\"]",
                DefaultUnit     = "%",
                NormalRangeLow  = 1,
                NormalRangeHigh = 4,
                NormalRangeUnit = "%",
                Category        = "Complete Blood Count"
            },
            new CommonLabUnit {
                MeasurementType = "Basophils (%)",
                CommonUnits     = "[\"%\"]",
                DefaultUnit     = "%",
                NormalRangeLow  = 0,
                NormalRangeHigh = 1,
                NormalRangeUnit = "%",
                Category        = "Complete Blood Count"
            },
            new CommonLabUnit {
                MeasurementType = "Reticulocyte Count",
                CommonUnits     = "[\"%\", \"x10³/µL\"]",
                DefaultUnit     = "%",
                NormalRangeLow  = 0.5m,
                NormalRangeHigh = 2.5m,
                NormalRangeUnit = "%",
                Aliases         = "[\"retic count\"]",
                Category        = "Complete Blood Count"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 3. LIPID PROFILE
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "Cholesterol (Total)",
                CommonUnits     = "[\"mg/dL\", \"mmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 125,
                NormalRangeHigh = 200,
                NormalRangeUnit = "mg/dL",
                Category        = "Lipid Profile"
            },
            new CommonLabUnit {
                MeasurementType = "HDL Cholesterol",
                CommonUnits     = "[\"mg/dL\", \"mmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 40,
                NormalRangeHigh = 60,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"good cholesterol\", \"high-density lipoprotein\"]",
                Category        = "Lipid Profile"
            },
            new CommonLabUnit {
                MeasurementType = "LDL Cholesterol",
                CommonUnits     = "[\"mg/dL\", \"mmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 50,
                NormalRangeHigh = 100,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"bad cholesterol\", \"low-density lipoprotein\"]",
                Category        = "Lipid Profile"
            },
            new CommonLabUnit {
                MeasurementType = "Triglycerides",
                CommonUnits     = "[\"mg/dL\", \"mmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 0,
                NormalRangeHigh = 150,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"TG\", \"TRIG\"]",
                Category        = "Lipid Profile"
            },
            new CommonLabUnit {
                MeasurementType = "VLDL Cholesterol",
                CommonUnits     = "[\"mg/dL\", \"mmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 2,
                NormalRangeHigh = 30,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"very low-density lipoprotein\"]",
                Category        = "Lipid Profile"
            },
            new CommonLabUnit {
                MeasurementType = "Non-HDL Cholesterol",
                CommonUnits     = "[\"mg/dL\", \"mmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 0,
                NormalRangeHigh = 130,
                NormalRangeUnit = "mg/dL",
                Category        = "Lipid Profile"
            },
            new CommonLabUnit {
                MeasurementType = "Apolipoprotein A1",
                CommonUnits     = "[\"mg/dL\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 120,
                NormalRangeHigh = 180,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"ApoA1\", \"Apo A-I\"]",
                Category        = "Lipid Profile"
            },
            new CommonLabUnit {
                MeasurementType = "Apolipoprotein B",
                CommonUnits     = "[\"mg/dL\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 52,
                NormalRangeHigh = 109,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"ApoB\", \"Apo B-100\"]",
                Category        = "Lipid Profile"
            },
            new CommonLabUnit {
                MeasurementType = "Lipoprotein(a)",
                CommonUnits     = "[\"mg/dL\", \"nmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 0,
                NormalRangeHigh = 30,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"Lp(a)\"]",
                Category        = "Lipid Profile"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 4. KIDNEY FUNCTION
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "Creatinine (Serum)",
                CommonUnits     = "[\"mg/dL\", \"µmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 0.6m,
                NormalRangeHigh = 1.3m,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"serum creatinine\", \"SCr\"]",
                Category        = "Kidney Function"
            },
            new CommonLabUnit {
                MeasurementType = "BUN (Blood Urea Nitrogen)",
                CommonUnits     = "[\"mg/dL\", \"mmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 7,
                NormalRangeHigh = 20,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"urea nitrogen\", \"serum urea\"]",
                Category        = "Kidney Function"
            },
            new CommonLabUnit {
                MeasurementType = "BUN/Creatinine Ratio",
                CommonUnits     = "[\"\"]",
                DefaultUnit     = "",
                NormalRangeLow  = 10,
                NormalRangeHigh = 20,
                NormalRangeUnit = "",
                Category        = "Kidney Function"
            },
            new CommonLabUnit {
                MeasurementType = "eGFR",
                CommonUnits     = "[\"mL/min/1.73m²\"]",
                DefaultUnit     = "mL/min/1.73m²",
                NormalRangeLow  = 90,
                NormalRangeHigh = 120,
                NormalRangeUnit = "mL/min/1.73m²",
                Aliases         = "[\"glomerular filtration rate\", \"GFR\", \"CKD-EPI\"]",
                Category        = "Kidney Function"
            },
            new CommonLabUnit {
                MeasurementType = "Urine Microalbumin",
                CommonUnits     = "[\"mg/g\", \"mg/24hr\"]",
                DefaultUnit     = "mg/g",
                NormalRangeLow  = 0,
                NormalRangeHigh = 30,
                NormalRangeUnit = "mg/g",
                Aliases         = "[\"albumin-to-creatinine ratio\", \"ACR\", \"microalbuminuria\"]",
                Category        = "Kidney Function"
            },
            new CommonLabUnit {
                MeasurementType = "Cystatin C",
                CommonUnits     = "[\"mg/L\"]",
                DefaultUnit     = "mg/L",
                NormalRangeLow  = 0.62m,
                NormalRangeHigh = 1.15m,
                NormalRangeUnit = "mg/L",
                Category        = "Kidney Function"
            },
            new CommonLabUnit {
                MeasurementType = "24-Hour Urine Protein",
                CommonUnits     = "[\"mg/24hr\", \"g/24hr\"]",
                DefaultUnit     = "mg/24hr",
                NormalRangeLow  = 0,
                NormalRangeHigh = 150,
                NormalRangeUnit = "mg/24hr",
                Aliases         = "[\"24h urine protein\", \"daily proteinuria\"]",
                Category        = "Kidney Function"
            },
            new CommonLabUnit {
                MeasurementType = "Uric Acid",
                CommonUnits     = "[\"mg/dL\", \"µmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 3.5m,
                NormalRangeHigh = 7.2m,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"serum urate\", \"urate\"]",
                Category        = "Kidney Function"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 5. LIVER FUNCTION
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "ALT (Alanine Aminotransferase)",
                CommonUnits     = "[\"U/L\"]",
                DefaultUnit     = "U/L",
                NormalRangeLow  = 7,
                NormalRangeHigh = 55,
                NormalRangeUnit = "U/L",
                Aliases         = "[\"SGPT\", \"alanine transaminase\"]",
                Category        = "Liver Function"
            },
            new CommonLabUnit {
                MeasurementType = "AST (Aspartate Aminotransferase)",
                CommonUnits     = "[\"U/L\"]",
                DefaultUnit     = "U/L",
                NormalRangeLow  = 8,
                NormalRangeHigh = 48,
                NormalRangeUnit = "U/L",
                Aliases         = "[\"SGOT\", \"aspartate transaminase\"]",
                Category        = "Liver Function"
            },
            new CommonLabUnit {
                MeasurementType = "ALP (Alkaline Phosphatase)",
                CommonUnits     = "[\"U/L\"]",
                DefaultUnit     = "U/L",
                NormalRangeLow  = 40,
                NormalRangeHigh = 129,
                NormalRangeUnit = "U/L",
                Category        = "Liver Function"
            },
            new CommonLabUnit {
                MeasurementType = "GGT (Gamma-Glutamyl Transferase)",
                CommonUnits     = "[\"U/L\"]",
                DefaultUnit     = "U/L",
                NormalRangeLow  = 8,
                NormalRangeHigh = 61,
                NormalRangeUnit = "U/L",
                Aliases         = "[\"gamma-GT\", \"GGTP\"]",
                Category        = "Liver Function"
            },
            new CommonLabUnit {
                MeasurementType = "Bilirubin (Total)",
                CommonUnits     = "[\"mg/dL\", \"µmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 0.1m,
                NormalRangeHigh = 1.2m,
                NormalRangeUnit = "mg/dL",
                Category        = "Liver Function"
            },
            new CommonLabUnit {
                MeasurementType = "Bilirubin (Direct / Conjugated)",
                CommonUnits     = "[\"mg/dL\", \"µmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 0,
                NormalRangeHigh = 0.3m,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"direct bilirubin\", \"conjugated bilirubin\"]",
                Category        = "Liver Function"
            },
            new CommonLabUnit {
                MeasurementType = "Bilirubin (Indirect)",
                CommonUnits     = "[\"mg/dL\", \"µmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeHigh = 0.8m,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"indirect bilirubin\", \"unconjugated bilirubin\"]",
                Category        = "Liver Function"
            },
            new CommonLabUnit {
                MeasurementType = "Albumin (Serum)",
                CommonUnits     = "[\"g/dL\", \"g/L\"]",
                DefaultUnit     = "g/dL",
                NormalRangeLow  = 3.4m,
                NormalRangeHigh = 5.4m,
                NormalRangeUnit = "g/dL",
                Aliases         = "[\"serum albumin\"]",
                Category        = "Liver Function"
            },
            new CommonLabUnit {
                MeasurementType = "Total Protein (Serum)",
                CommonUnits     = "[\"g/dL\", \"g/L\"]",
                DefaultUnit     = "g/dL",
                NormalRangeLow  = 6.3m,
                NormalRangeHigh = 8.2m,
                NormalRangeUnit = "g/dL",
                Category        = "Liver Function"
            },
            new CommonLabUnit {
                MeasurementType = "Prothrombin Time (PT)",
                CommonUnits     = "[\"seconds\"]",
                DefaultUnit     = "seconds",
                NormalRangeLow  = 11,
                NormalRangeHigh = 13.5m,
                NormalRangeUnit = "seconds",
                Aliases         = "[\"PT\", \"clotting time\"]",
                Category        = "Liver Function"
            },
            new CommonLabUnit {
                MeasurementType = "INR (International Normalised Ratio)",
                CommonUnits     = "[\"\"]",
                DefaultUnit     = "",
                NormalRangeLow  = 0.8m,
                NormalRangeHigh = 1.1m,
                NormalRangeUnit = "",
                Aliases         = "[\"PT/INR\"]",
                Category        = "Liver Function"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 6. ELECTROLYTES
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "Sodium",
                CommonUnits     = "[\"mEq/L\", \"mmol/L\"]",
                DefaultUnit     = "mEq/L",
                NormalRangeLow  = 136,
                NormalRangeHigh = 145,
                NormalRangeUnit = "mEq/L",
                Aliases         = "[\"Na\", \"Na+\", \"serum sodium\"]",
                Category        = "Electrolytes"
            },
            new CommonLabUnit {
                MeasurementType = "Potassium",
                CommonUnits     = "[\"mEq/L\", \"mmol/L\"]",
                DefaultUnit     = "mEq/L",
                NormalRangeLow  = 3.5m,
                NormalRangeHigh = 5.1m,
                NormalRangeUnit = "mEq/L",
                Aliases         = "[\"K\", \"K+\", \"serum potassium\"]",
                Category        = "Electrolytes"
            },
            new CommonLabUnit {
                MeasurementType = "Chloride",
                CommonUnits     = "[\"mEq/L\", \"mmol/L\"]",
                DefaultUnit     = "mEq/L",
                NormalRangeLow  = 98,
                NormalRangeHigh = 107,
                NormalRangeUnit = "mEq/L",
                Aliases         = "[\"Cl\", \"Cl-\"]",
                Category        = "Electrolytes"
            },
            new CommonLabUnit {
                MeasurementType = "Bicarbonate",
                CommonUnits     = "[\"mEq/L\", \"mmol/L\"]",
                DefaultUnit     = "mEq/L",
                NormalRangeLow  = 22,
                NormalRangeHigh = 29,
                NormalRangeUnit = "mEq/L",
                Aliases         = "[\"HCO3\", \"CO2 (total)\", \"serum bicarbonate\"]",
                Category        = "Electrolytes"
            },
            new CommonLabUnit {
                MeasurementType = "Calcium (Total)",
                CommonUnits     = "[\"mg/dL\", \"mmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 8.5m,
                NormalRangeHigh = 10.2m,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"Ca\", \"total calcium\"]",
                Category        = "Electrolytes"
            },
            new CommonLabUnit {
                MeasurementType = "Calcium (Ionised)",
                CommonUnits     = "[\"mg/dL\", \"mmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 4.6m,
                NormalRangeHigh = 5.3m,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"free calcium\", \"ionized calcium\"]",
                Category        = "Electrolytes"
            },
            new CommonLabUnit {
                MeasurementType = "Magnesium",
                CommonUnits     = "[\"mg/dL\", \"mmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 1.7m,
                NormalRangeHigh = 2.2m,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"Mg\", \"Mg2+\", \"serum magnesium\"]",
                Category        = "Electrolytes"
            },
            new CommonLabUnit {
                MeasurementType = "Phosphorus",
                CommonUnits     = "[\"mg/dL\", \"mmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 2.5m,
                NormalRangeHigh = 4.5m,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"phosphate\", \"inorganic phosphate\", \"Pi\"]",
                Category        = "Electrolytes"
            },
            new CommonLabUnit {
                MeasurementType = "Anion Gap",
                CommonUnits     = "[\"mEq/L\"]",
                DefaultUnit     = "mEq/L",
                NormalRangeLow  = 8,
                NormalRangeHigh = 16,
                NormalRangeUnit = "mEq/L",
                Aliases         = "[\"AG\"]",
                Category        = "Electrolytes"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 7. THYROID FUNCTION
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "TSH (Thyroid Stimulating Hormone)",
                CommonUnits     = "[\"mIU/L\", \"µIU/mL\"]",
                DefaultUnit     = "mIU/L",
                NormalRangeLow  = 0.45m,
                NormalRangeHigh = 4.5m,
                NormalRangeUnit = "mIU/L",
                Aliases         = "[\"thyrotropin\", \"thyroid stimulating hormone\"]",
                Category        = "Thyroid Function"
            },
            new CommonLabUnit {
                MeasurementType = "Free T4 (fT4)",
                CommonUnits     = "[\"ng/dL\", \"pmol/L\"]",
                DefaultUnit     = "ng/dL",
                NormalRangeLow  = 0.8m,
                NormalRangeHigh = 1.8m,
                NormalRangeUnit = "ng/dL",
                Aliases         = "[\"free thyroxine\", \"FT4\"]",
                Category        = "Thyroid Function"
            },
            new CommonLabUnit {
                MeasurementType = "Free T3 (fT3)",
                CommonUnits     = "[\"pg/mL\", \"pmol/L\"]",
                DefaultUnit     = "pg/mL",
                NormalRangeLow  = 2.3m,
                NormalRangeHigh = 4.2m,
                NormalRangeUnit = "pg/mL",
                Aliases         = "[\"free triiodothyronine\", \"FT3\"]",
                Category        = "Thyroid Function"
            },
            new CommonLabUnit {
                MeasurementType = "Total T4",
                CommonUnits     = "[\"µg/dL\", \"nmol/L\"]",
                DefaultUnit     = "µg/dL",
                NormalRangeLow  = 4.5m,
                NormalRangeHigh = 12.0m,
                NormalRangeUnit = "µg/dL",
                Aliases         = "[\"thyroxine\", \"T4 total\"]",
                Category        = "Thyroid Function"
            },
            new CommonLabUnit {
                MeasurementType = "Total T3",
                CommonUnits     = "[\"ng/dL\", \"nmol/L\"]",
                DefaultUnit     = "ng/dL",
                NormalRangeLow  = 80,
                NormalRangeHigh = 200,
                NormalRangeUnit = "ng/dL",
                Aliases         = "[\"triiodothyronine\", \"T3 total\"]",
                Category        = "Thyroid Function"
            },
            new CommonLabUnit {
                MeasurementType = "Anti-TPO Antibody",
                CommonUnits     = "[\"IU/mL\"]",
                DefaultUnit     = "IU/mL",
                NormalRangeHigh = 35,
                NormalRangeUnit = "IU/mL",
                Aliases         = "[\"thyroid peroxidase antibody\", \"TPOAb\"]",
                Category        = "Thyroid Function"
            },
            new CommonLabUnit {
                MeasurementType = "Anti-Thyroglobulin Antibody",
                CommonUnits     = "[\"IU/mL\"]",
                DefaultUnit     = "IU/mL",
                NormalRangeHigh = 115,
                NormalRangeUnit = "IU/mL",
                Aliases         = "[\"TgAb\", \"thyroglobulin antibody\"]",
                Category        = "Thyroid Function"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 8. CARDIAC MARKERS
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "Troponin I (Cardiac)",
                CommonUnits     = "[\"ng/mL\", \"µg/L\"]",
                DefaultUnit     = "ng/mL",
                NormalRangeLow  = 0,
                NormalRangeHigh = 0.04m,
                NormalRangeUnit = "ng/mL",
                Aliases         = "[\"cTnI\", \"cardiac troponin I\"]",
                Category        = "Cardiac Markers"
            },
            new CommonLabUnit {
                MeasurementType = "Troponin T (High-Sensitivity)",
                CommonUnits     = "[\"ng/L\"]",
                DefaultUnit     = "ng/L",
                NormalRangeHigh = 14,
                NormalRangeUnit = "ng/L",
                Aliases         = "[\"hsTnT\", \"hs-TnT\", \"high-sensitivity troponin T\"]",
                Category        = "Cardiac Markers"
            },
            new CommonLabUnit {
                MeasurementType = "BNP (B-type Natriuretic Peptide)",
                CommonUnits     = "[\"pg/mL\"]",
                DefaultUnit     = "pg/mL",
                NormalRangeHigh = 100,
                NormalRangeUnit = "pg/mL",
                Aliases         = "[\"brain natriuretic peptide\"]",
                Category        = "Cardiac Markers"
            },
            new CommonLabUnit {
                MeasurementType = "NT-proBNP",
                CommonUnits     = "[\"pg/mL\"]",
                DefaultUnit     = "pg/mL",
                NormalRangeHigh = 125,
                NormalRangeUnit = "pg/mL",
                Aliases         = "[\"N-terminal pro-BNP\", \"proBNP\"]",
                Category        = "Cardiac Markers"
            },
            new CommonLabUnit {
                MeasurementType = "CK-MB (Creatine Kinase-MB)",
                CommonUnits     = "[\"ng/mL\", \"U/L\"]",
                DefaultUnit     = "ng/mL",
                NormalRangeHigh = 5.0m,
                NormalRangeUnit = "ng/mL",
                Aliases         = "[\"creatine kinase MB\", \"CK-MB fraction\"]",
                Category        = "Cardiac Markers"
            },
            new CommonLabUnit {
                MeasurementType = "CK (Total Creatine Kinase)",
                CommonUnits     = "[\"U/L\"]",
                DefaultUnit     = "U/L",
                NormalRangeLow  = 30,
                NormalRangeHigh = 200,
                NormalRangeUnit = "U/L",
                Aliases         = "[\"CPK\", \"total CK\", \"creatine phosphokinase\"]",
                Category        = "Cardiac Markers"
            },
            new CommonLabUnit {
                MeasurementType = "Myoglobin",
                CommonUnits     = "[\"ng/mL\", \"µg/L\"]",
                DefaultUnit     = "ng/mL",
                NormalRangeLow  = 25,
                NormalRangeHigh = 72,
                NormalRangeUnit = "ng/mL",
                Category        = "Cardiac Markers"
            },
            new CommonLabUnit {
                MeasurementType = "LDH (Lactate Dehydrogenase)",
                CommonUnits     = "[\"U/L\"]",
                DefaultUnit     = "U/L",
                NormalRangeLow  = 140,
                NormalRangeHigh = 280,
                NormalRangeUnit = "U/L",
                Aliases         = "[\"lactate dehydrogenase\", \"LD\"]",
                Category        = "Cardiac Markers"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 9. INFLAMMATION & INFECTION MARKERS
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "CRP (C-Reactive Protein)",
                CommonUnits     = "[\"mg/L\", \"mg/dL\"]",
                DefaultUnit     = "mg/L",
                NormalRangeHigh = 3.0m,
                NormalRangeUnit = "mg/L",
                Aliases         = "[\"C-reactive protein\"]",
                Category        = "Inflammation & Infection"
            },
            new CommonLabUnit {
                MeasurementType = "hs-CRP (High-Sensitivity CRP)",
                CommonUnits     = "[\"mg/L\"]",
                DefaultUnit     = "mg/L",
                NormalRangeHigh = 1.0m,
                NormalRangeUnit = "mg/L",
                Aliases         = "[\"high sensitivity CRP\", \"hsCRP\"]",
                Category        = "Inflammation & Infection"
            },
            new CommonLabUnit {
                MeasurementType = "ESR (Erythrocyte Sedimentation Rate)",
                CommonUnits     = "[\"mm/hr\"]",
                DefaultUnit     = "mm/hr",
                NormalRangeLow  = 0,
                NormalRangeHigh = 20,
                NormalRangeUnit = "mm/hr",
                Aliases         = "[\"sedimentation rate\", \"sed rate\", \"Westergren\"]",
                Category        = "Inflammation & Infection"
            },
            new CommonLabUnit {
                MeasurementType = "Procalcitonin (PCT)",
                CommonUnits     = "[\"ng/mL\", \"µg/L\"]",
                DefaultUnit     = "ng/mL",
                NormalRangeHigh = 0.1m,
                NormalRangeUnit = "ng/mL",
                Aliases         = "[\"PCT\", \"procalcitonin\"]",
                Category        = "Inflammation & Infection"
            },
            new CommonLabUnit {
                MeasurementType = "Ferritin",
                CommonUnits     = "[\"ng/mL\", \"µg/L\"]",
                DefaultUnit     = "ng/mL",
                NormalRangeLow  = 20,
                NormalRangeHigh = 500,
                NormalRangeUnit = "ng/mL",
                Aliases         = "[\"serum ferritin\"]",
                Category        = "Inflammation & Infection"
            },
            new CommonLabUnit {
                MeasurementType = "Fibrinogen",
                CommonUnits     = "[\"mg/dL\", \"g/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 200,
                NormalRangeHigh = 400,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"plasma fibrinogen\", \"clotting factor I\"]",
                Category        = "Inflammation & Infection"
            },
            new CommonLabUnit {
                MeasurementType = "IL-6 (Interleukin-6)",
                CommonUnits     = "[\"pg/mL\"]",
                DefaultUnit     = "pg/mL",
                NormalRangeHigh = 7,
                NormalRangeUnit = "pg/mL",
                Aliases         = "[\"interleukin 6\", \"IL6\"]",
                Category        = "Inflammation & Infection"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 10. VITAMINS & MINERALS
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "Vitamin D (25-Hydroxy)",
                CommonUnits     = "[\"ng/mL\", \"nmol/L\"]",
                DefaultUnit     = "ng/mL",
                NormalRangeLow  = 30,
                NormalRangeHigh = 100,
                NormalRangeUnit = "ng/mL",
                Aliases         = "[\"25-OH Vitamin D\", \"calcidiol\", \"cholecalciferol\"]",
                Category        = "Vitamins & Minerals"
            },
            new CommonLabUnit {
                MeasurementType = "Vitamin B12 (Cobalamin)",
                CommonUnits     = "[\"pg/mL\", \"pmol/L\"]",
                DefaultUnit     = "pg/mL",
                NormalRangeLow  = 200,
                NormalRangeHigh = 900,
                NormalRangeUnit = "pg/mL",
                Aliases         = "[\"cobalamin\", \"cyanocobalamin\"]",
                Category        = "Vitamins & Minerals"
            },
            new CommonLabUnit {
                MeasurementType = "Folate (Folic Acid)",
                CommonUnits     = "[\"ng/mL\", \"nmol/L\"]",
                DefaultUnit     = "ng/mL",
                NormalRangeLow  = 2.7m,
                NormalRangeHigh = 17.0m,
                NormalRangeUnit = "ng/mL",
                Aliases         = "[\"folic acid\", \"serum folate\", \"vitamin B9\"]",
                Category        = "Vitamins & Minerals"
            },
            new CommonLabUnit {
                MeasurementType = "Vitamin B1 (Thiamine)",
                CommonUnits     = "[\"nmol/L\", \"µg/dL\"]",
                DefaultUnit     = "nmol/L",
                NormalRangeLow  = 70,
                NormalRangeHigh = 180,
                NormalRangeUnit = "nmol/L",
                Aliases         = "[\"thiamine\", \"aneurin\"]",
                Category        = "Vitamins & Minerals"
            },
            new CommonLabUnit {
                MeasurementType = "Vitamin C (Ascorbic Acid)",
                CommonUnits     = "[\"mg/dL\", \"µmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 0.4m,
                NormalRangeHigh = 2.0m,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"ascorbate\", \"ascorbic acid\"]",
                Category        = "Vitamins & Minerals"
            },
            new CommonLabUnit {
                MeasurementType = "Iron (Serum)",
                CommonUnits     = "[\"µg/dL\", \"µmol/L\"]",
                DefaultUnit     = "µg/dL",
                NormalRangeLow  = 60,
                NormalRangeHigh = 170,
                NormalRangeUnit = "µg/dL",
                Aliases         = "[\"serum iron\", \"Fe\"]",
                Category        = "Vitamins & Minerals"
            },
            new CommonLabUnit {
                MeasurementType = "TIBC (Total Iron Binding Capacity)",
                CommonUnits     = "[\"µg/dL\", \"µmol/L\"]",
                DefaultUnit     = "µg/dL",
                NormalRangeLow  = 250,
                NormalRangeHigh = 370,
                NormalRangeUnit = "µg/dL",
                Aliases         = "[\"total iron binding capacity\", \"transferrin capacity\"]",
                Category        = "Vitamins & Minerals"
            },
            new CommonLabUnit {
                MeasurementType = "Transferrin Saturation",
                CommonUnits     = "[\"%\"]",
                DefaultUnit     = "%",
                NormalRangeLow  = 20,
                NormalRangeHigh = 50,
                NormalRangeUnit = "%",
                Aliases         = "[\"iron saturation\", \"Tsat\"]",
                Category        = "Vitamins & Minerals"
            },
            new CommonLabUnit {
                MeasurementType = "Zinc",
                CommonUnits     = "[\"µg/dL\", \"µmol/L\"]",
                DefaultUnit     = "µg/dL",
                NormalRangeLow  = 70,
                NormalRangeHigh = 120,
                NormalRangeUnit = "µg/dL",
                Aliases         = "[\"serum zinc\"]",
                Category        = "Vitamins & Minerals"
            },
            new CommonLabUnit {
                MeasurementType = "Copper",
                CommonUnits     = "[\"µg/dL\", \"µmol/L\"]",
                DefaultUnit     = "µg/dL",
                NormalRangeLow  = 70,
                NormalRangeHigh = 140,
                NormalRangeUnit = "µg/dL",
                Aliases         = "[\"serum copper\", \"Cu\"]",
                Category        = "Vitamins & Minerals"
            },
            new CommonLabUnit {
                MeasurementType = "Selenium",
                CommonUnits     = "[\"µg/L\", \"nmol/L\"]",
                DefaultUnit     = "µg/L",
                NormalRangeLow  = 70,
                NormalRangeHigh = 150,
                NormalRangeUnit = "µg/L",
                Aliases         = "[\"serum selenium\", \"Se\"]",
                Category        = "Vitamins & Minerals"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 11. URINALYSIS
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "Urine Specific Gravity",
                CommonUnits     = "[\"\"]",
                DefaultUnit     = "",
                NormalRangeLow  = 1.005m,
                NormalRangeHigh = 1.030m,
                NormalRangeUnit = "",
                Category        = "Urinalysis"
            },
            new CommonLabUnit {
                MeasurementType = "Urine pH",
                CommonUnits     = "[\"\"]",
                DefaultUnit     = "",
                NormalRangeLow  = 4.5m,
                NormalRangeHigh = 8.0m,
                NormalRangeUnit = "",
                Category        = "Urinalysis"
            },
            new CommonLabUnit {
                MeasurementType = "Urine Protein",
                CommonUnits     = "[\"mg/dL\", \"g/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeHigh = 30,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"dipstick protein\", \"urine albumin dipstick\"]",
                Category        = "Urinalysis"
            },
            new CommonLabUnit {
                MeasurementType = "Urine Glucose",
                CommonUnits     = "[\"mg/dL\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeHigh = 15,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"glucosuria\"]",
                Category        = "Urinalysis"
            },
            new CommonLabUnit {
                MeasurementType = "Urine Ketones",
                CommonUnits     = "[\"mg/dL\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeHigh = 5,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"ketonuria\", \"urine acetone\"]",
                Category        = "Urinalysis"
            },
            new CommonLabUnit {
                MeasurementType = "Urine RBC (Microscopy)",
                CommonUnits     = "[\"/HPF\"]",
                DefaultUnit     = "/HPF",
                NormalRangeHigh = 5,
                NormalRangeUnit = "/HPF",
                Aliases         = "[\"urine red blood cells\", \"hematuria\"]",
                Category        = "Urinalysis"
            },
            new CommonLabUnit {
                MeasurementType = "Urine WBC (Microscopy)",
                CommonUnits     = "[\"/HPF\"]",
                DefaultUnit     = "/HPF",
                NormalRangeHigh = 5,
                NormalRangeUnit = "/HPF",
                Aliases         = "[\"urine white blood cells\", \"pyuria\"]",
                Category        = "Urinalysis"
            },
            new CommonLabUnit {
                MeasurementType = "Urine Creatinine",
                CommonUnits     = "[\"mg/dL\", \"mmol/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 40,
                NormalRangeHigh = 300,
                NormalRangeUnit = "mg/dL",
                Category        = "Urinalysis"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 12. COAGULATION / HAEMATOLOGY
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "aPTT (Activated Partial Thromboplastin Time)",
                CommonUnits     = "[\"seconds\"]",
                DefaultUnit     = "seconds",
                NormalRangeLow  = 25,
                NormalRangeHigh = 35,
                NormalRangeUnit = "seconds",
                Aliases         = "[\"partial thromboplastin time\", \"PTT\"]",
                Category        = "Coagulation"
            },
            new CommonLabUnit {
                MeasurementType = "D-Dimer",
                CommonUnits     = "[\"ng/mL FEU\", \"µg/L\"]",
                DefaultUnit     = "ng/mL FEU",
                NormalRangeHigh = 500,
                NormalRangeUnit = "ng/mL FEU",
                Aliases         = "[\"fibrin degradation product\", \"FDP\"]",
                Category        = "Coagulation"
            },
            new CommonLabUnit {
                MeasurementType = "Fibrinogen",
                CommonUnits     = "[\"mg/dL\", \"g/L\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 200,
                NormalRangeHigh = 400,
                NormalRangeUnit = "mg/dL",
                Category        = "Coagulation"
            },
            new CommonLabUnit {
                MeasurementType = "Bleeding Time",
                CommonUnits     = "[\"minutes\"]",
                DefaultUnit     = "minutes",
                NormalRangeLow  = 2,
                NormalRangeHigh = 7,
                NormalRangeUnit = "minutes",
                Category        = "Coagulation"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 13. HORMONES & ENDOCRINOLOGY
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "Cortisol (Morning)",
                CommonUnits     = "[\"µg/dL\", \"nmol/L\"]",
                DefaultUnit     = "µg/dL",
                NormalRangeLow  = 6,
                NormalRangeHigh = 23,
                NormalRangeUnit = "µg/dL",
                Aliases         = "[\"serum cortisol\", \"hydrocortisone\", \"8am cortisol\"]",
                Category        = "Hormones & Endocrinology"
            },
            new CommonLabUnit {
                MeasurementType = "ACTH (Adrenocorticotropic Hormone)",
                CommonUnits     = "[\"pg/mL\", \"pmol/L\"]",
                DefaultUnit     = "pg/mL",
                NormalRangeLow  = 7,
                NormalRangeHigh = 63,
                NormalRangeUnit = "pg/mL",
                Aliases         = "[\"corticotropin\"]",
                Category        = "Hormones & Endocrinology"
            },
            new CommonLabUnit {
                MeasurementType = "Growth Hormone (GH)",
                CommonUnits     = "[\"ng/mL\", \"µg/L\"]",
                DefaultUnit     = "ng/mL",
                NormalRangeHigh = 3.0m,
                NormalRangeUnit = "ng/mL",
                Aliases         = "[\"somatotropin\", \"HGH\"]",
                Category        = "Hormones & Endocrinology"
            },
            new CommonLabUnit {
                MeasurementType = "IGF-1 (Insulin-like Growth Factor 1)",
                CommonUnits     = "[\"ng/mL\", \"nmol/L\"]",
                DefaultUnit     = "ng/mL",
                NormalRangeLow  = 100,
                NormalRangeHigh = 300,
                NormalRangeUnit = "ng/mL",
                Aliases         = "[\"somatomedin C\", \"IGF1\"]",
                Category        = "Hormones & Endocrinology"
            },
            new CommonLabUnit {
                MeasurementType = "Prolactin",
                CommonUnits     = "[\"ng/mL\", \"mIU/L\"]",
                DefaultUnit     = "ng/mL",
                NormalRangeLow  = 2,
                NormalRangeHigh = 18,
                NormalRangeUnit = "ng/mL",
                Aliases         = "[\"PRL\", \"lactotropin\"]",
                Category        = "Hormones & Endocrinology"
            },
            new CommonLabUnit {
                MeasurementType = "FSH (Follicle Stimulating Hormone)",
                CommonUnits     = "[\"mIU/mL\", \"IU/L\"]",
                DefaultUnit     = "mIU/mL",
                NormalRangeLow  = 1.5m,
                NormalRangeHigh = 12.4m,
                NormalRangeUnit = "mIU/mL",
                Aliases         = "[\"follitropin\"]",
                Category        = "Hormones & Endocrinology"
            },
            new CommonLabUnit {
                MeasurementType = "LH (Luteinizing Hormone)",
                CommonUnits     = "[\"mIU/mL\", \"IU/L\"]",
                DefaultUnit     = "mIU/mL",
                NormalRangeLow  = 1.7m,
                NormalRangeHigh = 8.6m,
                NormalRangeUnit = "mIU/mL",
                Aliases         = "[\"lutropin\"]",
                Category        = "Hormones & Endocrinology"
            },
            new CommonLabUnit {
                MeasurementType = "Testosterone (Total)",
                CommonUnits     = "[\"ng/dL\", \"nmol/L\"]",
                DefaultUnit     = "ng/dL",
                NormalRangeLow  = 300,
                NormalRangeHigh = 1000,
                NormalRangeUnit = "ng/dL",
                Aliases         = "[\"serum testosterone\"]",
                Category        = "Hormones & Endocrinology"
            },
            new CommonLabUnit {
                MeasurementType = "Estradiol (E2)",
                CommonUnits     = "[\"pg/mL\", \"pmol/L\"]",
                DefaultUnit     = "pg/mL",
                NormalRangeLow  = 15,
                NormalRangeHigh = 350,
                NormalRangeUnit = "pg/mL",
                Aliases         = "[\"oestradiol\", \"E2\"]",
                Category        = "Hormones & Endocrinology"
            },
            new CommonLabUnit {
                MeasurementType = "Progesterone",
                CommonUnits     = "[\"ng/mL\", \"nmol/L\"]",
                DefaultUnit     = "ng/mL",
                NormalRangeLow  = 0.1m,
                NormalRangeHigh = 25.0m,
                NormalRangeUnit = "ng/mL",
                Aliases         = "[\"serum progesterone\"]",
                Category        = "Hormones & Endocrinology"
            },
            new CommonLabUnit {
                MeasurementType = "DHEA-S",
                CommonUnits     = "[\"µg/dL\", \"µmol/L\"]",
                DefaultUnit     = "µg/dL",
                NormalRangeLow  = 80,
                NormalRangeHigh = 560,
                NormalRangeUnit = "µg/dL",
                Aliases         = "[\"dehydroepiandrosterone sulfate\", \"DHEA-SO4\"]",
                Category        = "Hormones & Endocrinology"
            },
            new CommonLabUnit {
                MeasurementType = "Aldosterone",
                CommonUnits     = "[\"ng/dL\", \"pmol/L\"]",
                DefaultUnit     = "ng/dL",
                NormalRangeLow  = 1,
                NormalRangeHigh = 21,
                NormalRangeUnit = "ng/dL",
                Category        = "Hormones & Endocrinology"
            },
            new CommonLabUnit {
                MeasurementType = "Renin Activity (Plasma)",
                CommonUnits     = "[\"ng/mL/hr\"]",
                DefaultUnit     = "ng/mL/hr",
                NormalRangeLow  = 0.5m,
                NormalRangeHigh = 4.0m,
                NormalRangeUnit = "ng/mL/hr",
                Aliases         = "[\"PRA\", \"plasma renin activity\"]",
                Category        = "Hormones & Endocrinology"
            },
            new CommonLabUnit {
                MeasurementType = "PTH (Parathyroid Hormone)",
                CommonUnits     = "[\"pg/mL\", \"pmol/L\"]",
                DefaultUnit     = "pg/mL",
                NormalRangeLow  = 15,
                NormalRangeHigh = 68,
                NormalRangeUnit = "pg/mL",
                Aliases         = "[\"parathyroid hormone\", \"iPTH\", \"intact PTH\"]",
                Category        = "Hormones & Endocrinology"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 14. TUMOUR MARKERS
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "PSA (Prostate Specific Antigen)",
                CommonUnits     = "[\"ng/mL\"]",
                DefaultUnit     = "ng/mL",
                NormalRangeHigh = 4.0m,
                NormalRangeUnit = "ng/mL",
                Aliases         = "[\"prostate specific antigen\", \"total PSA\"]",
                Category        = "Tumour Markers"
            },
            new CommonLabUnit {
                MeasurementType = "CEA (Carcinoembryonic Antigen)",
                CommonUnits     = "[\"ng/mL\"]",
                DefaultUnit     = "ng/mL",
                NormalRangeHigh = 2.5m,
                NormalRangeUnit = "ng/mL",
                Aliases         = "[\"carcinoembryonic antigen\"]",
                Category        = "Tumour Markers"
            },
            new CommonLabUnit {
                MeasurementType = "AFP (Alpha-Fetoprotein)",
                CommonUnits     = "[\"ng/mL\", \"IU/mL\"]",
                DefaultUnit     = "ng/mL",
                NormalRangeHigh = 8.5m,
                NormalRangeUnit = "ng/mL",
                Aliases         = "[\"alpha fetoprotein\"]",
                Category        = "Tumour Markers"
            },
            new CommonLabUnit {
                MeasurementType = "CA-125",
                CommonUnits     = "[\"U/mL\"]",
                DefaultUnit     = "U/mL",
                NormalRangeHigh = 35,
                NormalRangeUnit = "U/mL",
                Aliases         = "[\"cancer antigen 125\", \"ovarian cancer marker\"]",
                Category        = "Tumour Markers"
            },
            new CommonLabUnit {
                MeasurementType = "CA 19-9",
                CommonUnits     = "[\"U/mL\"]",
                DefaultUnit     = "U/mL",
                NormalRangeHigh = 37,
                NormalRangeUnit = "U/mL",
                Aliases         = "[\"cancer antigen 19-9\", \"pancreatic marker\"]",
                Category        = "Tumour Markers"
            },
            new CommonLabUnit {
                MeasurementType = "CA 15-3",
                CommonUnits     = "[\"U/mL\"]",
                DefaultUnit     = "U/mL",
                NormalRangeHigh = 30,
                NormalRangeUnit = "U/mL",
                Aliases         = "[\"cancer antigen 15-3\", \"breast cancer marker\"]",
                Category        = "Tumour Markers"
            },
            new CommonLabUnit {
                MeasurementType = "Beta-HCG",
                CommonUnits     = "[\"mIU/mL\", \"IU/L\"]",
                DefaultUnit     = "mIU/mL",
                NormalRangeLow  = 0,
                NormalRangeHigh = 5,
                NormalRangeUnit = "mIU/mL",
                Aliases         = "[\"human chorionic gonadotropin\", \"hCG\", \"pregnancy hormone\"]",
                Category        = "Tumour Markers"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 15. AUTOIMMUNE & IMMUNOLOGY
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "ANA (Antinuclear Antibody)",
                CommonUnits     = "[\"titer\"]",
                DefaultUnit     = "titer",
                NormalRangeHigh = 80,
                NormalRangeUnit = "titer",
                Aliases         = "[\"antinuclear antibodies\", \"FANA\"]",
                Category        = "Autoimmune & Immunology"
            },
            new CommonLabUnit {
                MeasurementType = "Rheumatoid Factor (RF)",
                CommonUnits     = "[\"IU/mL\", \"U/mL\"]",
                DefaultUnit     = "IU/mL",
                NormalRangeHigh = 20,
                NormalRangeUnit = "IU/mL",
                Aliases         = "[\"RA factor\"]",
                Category        = "Autoimmune & Immunology"
            },
            new CommonLabUnit {
                MeasurementType = "Anti-CCP (Anti-Cyclic Citrullinated Peptide)",
                CommonUnits     = "[\"U/mL\"]",
                DefaultUnit     = "U/mL",
                NormalRangeHigh = 20,
                NormalRangeUnit = "U/mL",
                Aliases         = "[\"anti-CCP antibody\", \"ACPA\"]",
                Category        = "Autoimmune & Immunology"
            },
            new CommonLabUnit {
                MeasurementType = "Complement C3",
                CommonUnits     = "[\"mg/dL\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 90,
                NormalRangeHigh = 180,
                NormalRangeUnit = "mg/dL",
                Category        = "Autoimmune & Immunology"
            },
            new CommonLabUnit {
                MeasurementType = "Complement C4",
                CommonUnits     = "[\"mg/dL\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 16,
                NormalRangeHigh = 47,
                NormalRangeUnit = "mg/dL",
                Category        = "Autoimmune & Immunology"
            },
            new CommonLabUnit {
                MeasurementType = "IgG (Immunoglobulin G)",
                CommonUnits     = "[\"mg/dL\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 700,
                NormalRangeHigh = 1600,
                NormalRangeUnit = "mg/dL",
                Category        = "Autoimmune & Immunology"
            },
            new CommonLabUnit {
                MeasurementType = "IgA (Immunoglobulin A)",
                CommonUnits     = "[\"mg/dL\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 70,
                NormalRangeHigh = 400,
                NormalRangeUnit = "mg/dL",
                Category        = "Autoimmune & Immunology"
            },
            new CommonLabUnit {
                MeasurementType = "IgM (Immunoglobulin M)",
                CommonUnits     = "[\"mg/dL\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 40,
                NormalRangeHigh = 230,
                NormalRangeUnit = "mg/dL",
                Category        = "Autoimmune & Immunology"
            },
            new CommonLabUnit {
                MeasurementType = "IgE (Total)",
                CommonUnits     = "[\"IU/mL\", \"kU/L\"]",
                DefaultUnit     = "IU/mL",
                NormalRangeHigh = 100,
                NormalRangeUnit = "IU/mL",
                Aliases         = "[\"total IgE\", \"atopy marker\"]",
                Category        = "Autoimmune & Immunology"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 16. INFECTIOUS DISEASE SEROLOGY
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "Hepatitis B Surface Antigen (HBsAg)",
                CommonUnits     = "[\"Reactive/NR\"]",
                DefaultUnit     = "Reactive/NR",
                NormalRangeUnit = "",
                Aliases         = "[\"HBsAg\", \"hepatitis B antigen\"]",
                Category        = "Infectious Disease"
            },
            new CommonLabUnit {
                MeasurementType = "Anti-HCV (Hepatitis C Antibody)",
                CommonUnits     = "[\"Reactive/NR\"]",
                DefaultUnit     = "Reactive/NR",
                NormalRangeUnit = "",
                Aliases         = "[\"HCV antibody\", \"hepatitis C\"]",
                Category        = "Infectious Disease"
            },
            new CommonLabUnit {
                MeasurementType = "HIV Ag/Ab Combo",
                CommonUnits     = "[\"Reactive/NR\"]",
                DefaultUnit     = "Reactive/NR",
                NormalRangeUnit = "",
                Aliases         = "[\"HIV test\", \"HIV-1/2\", \"fourth-generation HIV test\"]",
                Category        = "Infectious Disease"
            },
            new CommonLabUnit {
                MeasurementType = "CD4 Count",
                CommonUnits     = "[\"cells/µL\"]",
                DefaultUnit     = "cells/µL",
                NormalRangeLow  = 500,
                NormalRangeHigh = 1500,
                NormalRangeUnit = "cells/µL",
                Aliases         = "[\"T-helper cells\", \"CD4+ T cells\"]",
                Category        = "Infectious Disease"
            },
            new CommonLabUnit {
                MeasurementType = "Widal Test (Typhoid)",
                CommonUnits     = "[\"titer\"]",
                DefaultUnit     = "titer",
                NormalRangeHigh = 80,
                NormalRangeUnit = "titer",
                Aliases         = "[\"S. typhi O/H titer\"]",
                Category        = "Infectious Disease"
            },
            new CommonLabUnit {
                MeasurementType = "Dengue NS1 Antigen",
                CommonUnits     = "[\"Positive/Negative\"]",
                DefaultUnit     = "Positive/Negative",
                NormalRangeUnit = "",
                Aliases         = "[\"NS1\", \"dengue antigen\"]",
                Category        = "Infectious Disease"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 17. BLOOD PRESSURE & VITALS (trackable alongside labs)
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "Systolic Blood Pressure",
                CommonUnits     = "[\"mmHg\"]",
                DefaultUnit     = "mmHg",
                NormalRangeLow  = 90,
                NormalRangeHigh = 120,
                NormalRangeUnit = "mmHg",
                Aliases         = "[\"SBP\", \"systolic BP\", \"upper BP\"]",
                Category        = "Vital Signs"
            },
            new CommonLabUnit {
                MeasurementType = "Diastolic Blood Pressure",
                CommonUnits     = "[\"mmHg\"]",
                DefaultUnit     = "mmHg",
                NormalRangeLow  = 60,
                NormalRangeHigh = 80,
                NormalRangeUnit = "mmHg",
                Aliases         = "[\"DBP\", \"diastolic BP\", \"lower BP\"]",
                Category        = "Vital Signs"
            },
            new CommonLabUnit {
                MeasurementType = "Heart Rate",
                CommonUnits     = "[\"bpm\"]",
                DefaultUnit     = "bpm",
                NormalRangeLow  = 60,
                NormalRangeHigh = 100,
                NormalRangeUnit = "bpm",
                Aliases         = "[\"pulse\", \"pulse rate\", \"HR\"]",
                Category        = "Vital Signs"
            },
            new CommonLabUnit {
                MeasurementType = "SpO2 (Oxygen Saturation)",
                CommonUnits     = "[\"%\"]",
                DefaultUnit     = "%",
                NormalRangeLow  = 95,
                NormalRangeHigh = 100,
                NormalRangeUnit = "%",
                Aliases         = "[\"oxygen saturation\", \"pulse oximetry\", \"SaO2\"]",
                Category        = "Vital Signs"
            },
            new CommonLabUnit {
                MeasurementType = "Body Temperature",
                CommonUnits     = "[\"°C\", \"°F\"]",
                DefaultUnit     = "°C",
                NormalRangeLow  = 36.1m,
                NormalRangeHigh = 37.2m,
                NormalRangeUnit = "°C",
                Aliases         = "[\"temperature\", \"core temp\"]",
                Category        = "Vital Signs"
            },
            new CommonLabUnit {
                MeasurementType = "BMI (Body Mass Index)",
                CommonUnits     = "[\"kg/m²\"]",
                DefaultUnit     = "kg/m²",
                NormalRangeLow  = 18.5m,
                NormalRangeHigh = 24.9m,
                NormalRangeUnit = "kg/m²",
                Aliases         = "[\"body mass index\"]",
                Category        = "Vital Signs"
            },
            new CommonLabUnit {
                MeasurementType = "Weight",
                CommonUnits     = "[\"kg\", \"lbs\"]",
                DefaultUnit     = "kg",
                NormalRangeUnit = "kg",
                Aliases         = "[\"body weight\"]",
                Category        = "Vital Signs"
            },
            new CommonLabUnit {
                MeasurementType = "Waist Circumference",
                CommonUnits     = "[\"cm\", \"inches\"]",
                DefaultUnit     = "cm",
                NormalRangeHigh = 94,
                NormalRangeUnit = "cm",
                Aliases         = "[\"abdominal circumference\", \"waist measurement\"]",
                Category        = "Vital Signs"
            },
            new CommonLabUnit {
                MeasurementType = "Respiratory Rate",
                CommonUnits     = "[\"breaths/min\"]",
                DefaultUnit     = "breaths/min",
                NormalRangeLow  = 12,
                NormalRangeHigh = 20,
                NormalRangeUnit = "breaths/min",
                Aliases         = "[\"RR\", \"breathing rate\"]",
                Category        = "Vital Signs"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 18. ARTERIAL BLOOD GAS (ABG)
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "Blood pH",
                CommonUnits     = "[\"\"]",
                DefaultUnit     = "",
                NormalRangeLow  = 7.35m,
                NormalRangeHigh = 7.45m,
                NormalRangeUnit = "",
                Aliases         = "[\"arterial pH\", \"serum pH\"]",
                Category        = "Arterial Blood Gas"
            },
            new CommonLabUnit {
                MeasurementType = "PaO2 (Arterial Oxygen Pressure)",
                CommonUnits     = "[\"mmHg\"]",
                DefaultUnit     = "mmHg",
                NormalRangeLow  = 75,
                NormalRangeHigh = 100,
                NormalRangeUnit = "mmHg",
                Aliases         = "[\"partial pressure of oxygen\"]",
                Category        = "Arterial Blood Gas"
            },
            new CommonLabUnit {
                MeasurementType = "PaCO2 (Arterial CO2 Pressure)",
                CommonUnits     = "[\"mmHg\"]",
                DefaultUnit     = "mmHg",
                NormalRangeLow  = 35,
                NormalRangeHigh = 45,
                NormalRangeUnit = "mmHg",
                Aliases         = "[\"partial pressure of CO2\"]",
                Category        = "Arterial Blood Gas"
            },
            new CommonLabUnit {
                MeasurementType = "Base Excess",
                CommonUnits     = "[\"mEq/L\"]",
                DefaultUnit     = "mEq/L",
                NormalRangeLow  = -2,
                NormalRangeHigh = 2,
                NormalRangeUnit = "mEq/L",
                Aliases         = "[\"BE\"]",
                Category        = "Arterial Blood Gas"
            },
            new CommonLabUnit {
                MeasurementType = "Lactate (Blood)",
                CommonUnits     = "[\"mmol/L\", \"mg/dL\"]",
                DefaultUnit     = "mmol/L",
                NormalRangeLow  = 0.5m,
                NormalRangeHigh = 2.0m,
                NormalRangeUnit = "mmol/L",
                Aliases         = "[\"serum lactate\", \"lactic acid\"]",
                Category        = "Arterial Blood Gas"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 19. PANCREATIC & GI MARKERS
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "Amylase",
                CommonUnits     = "[\"U/L\"]",
                DefaultUnit     = "U/L",
                NormalRangeLow  = 30,
                NormalRangeHigh = 110,
                NormalRangeUnit = "U/L",
                Aliases         = "[\"serum amylase\", \"pancreatic amylase\"]",
                Category        = "Pancreatic & GI"
            },
            new CommonLabUnit {
                MeasurementType = "Lipase",
                CommonUnits     = "[\"U/L\"]",
                DefaultUnit     = "U/L",
                NormalRangeLow  = 0,
                NormalRangeHigh = 60,
                NormalRangeUnit = "U/L",
                Aliases         = "[\"serum lipase\"]",
                Category        = "Pancreatic & GI"
            },
            new CommonLabUnit {
                MeasurementType = "H. pylori Antigen (Stool)",
                CommonUnits     = "[\"Positive/Negative\"]",
                DefaultUnit     = "Positive/Negative",
                NormalRangeUnit = "",
                Aliases         = "[\"H pylori\", \"Helicobacter pylori\"]",
                Category        = "Pancreatic & GI"
            },
        });

        // ─────────────────────────────────────────────────────────────────
        // 20. NEUROLOGICAL & PSYCHIATRIC
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "CSF Glucose",
                CommonUnits     = "[\"mg/dL\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 45,
                NormalRangeHigh = 80,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"cerebrospinal fluid glucose\", \"CSF sugar\"]",
                Category        = "Neurology"
            },
            new CommonLabUnit {
                MeasurementType = "CSF Protein",
                CommonUnits     = "[\"mg/dL\"]",
                DefaultUnit     = "mg/dL",
                NormalRangeLow  = 15,
                NormalRangeHigh = 45,
                NormalRangeUnit = "mg/dL",
                Aliases         = "[\"cerebrospinal fluid protein\"]",
                Category        = "Neurology"
            },
            new CommonLabUnit {
                MeasurementType = "Homocysteine",
                CommonUnits     = "[\"µmol/L\"]",
                DefaultUnit     = "µmol/L",
                NormalRangeLow  = 5,
                NormalRangeHigh = 15,
                NormalRangeUnit = "µmol/L",
                Aliases         = "[\"total homocysteine\", \"tHcy\"]",
                Category        = "Neurology"
            },
        });
        
        // ─────────────────────────────────────────────────────────────────
        // 21. DIABETES MANAGEMENT & SYMPTOM MONITORING
        // ─────────────────────────────────────────────────────────────────
        units.AddRange(new[]
        {
            new CommonLabUnit {
                MeasurementType = "Polydipsia Score (1-10)",
                CommonUnits     = "[\"Score\"]",
                DefaultUnit     = "Score",
                NormalRangeLow  = 1,
                NormalRangeHigh = 10,
                NormalRangeUnit = "Score",
                Category        = "Symptom Monitoring"
            },
            new CommonLabUnit {
                MeasurementType = "Fatigue Score (1-10)",
                CommonUnits     = "[\"Score\"]",
                DefaultUnit     = "Score",
                NormalRangeLow  = 1,
                NormalRangeHigh = 10,
                NormalRangeUnit = "Score",
                Category        = "Symptom Monitoring"
            },
            new CommonLabUnit {
                MeasurementType = "Vision Blurring Score (1-10)",
                CommonUnits     = "[\"Score\"]",
                DefaultUnit     = "Score",
                NormalRangeLow  = 1,
                NormalRangeHigh = 10,
                NormalRangeUnit = "Score",
                Category        = "Symptom Monitoring"
            },
            new CommonLabUnit {
                MeasurementType = "Foot Tingling Score (1-10)",
                CommonUnits     = "[\"Score\"]",
                DefaultUnit     = "Score",
                NormalRangeLow  = 1,
                NormalRangeHigh = 10,
                NormalRangeUnit = "Score",
                Category        = "Symptom Monitoring"
            },
            new CommonLabUnit {
                MeasurementType = "Diet Compliance Score (0-10)",
                CommonUnits     = "[\"Score\"]",
                DefaultUnit     = "Score",
                NormalRangeLow  = 0,
                NormalRangeHigh = 10,
                NormalRangeUnit = "Score",
                Category        = "Management Adherence"
            },
            new CommonLabUnit {
                MeasurementType = "Medication Adherence (%)",
                CommonUnits     = "[\"%\"]",
                DefaultUnit     = "%",
                NormalRangeLow  = 0,
                NormalRangeHigh = 100,
                NormalRangeUnit = "%",
                Category        = "Management Adherence"
            },
            new CommonLabUnit {
                MeasurementType = "Polyuria Frequency (times/day)",
                CommonUnits     = "[\"times/day\"]",
                DefaultUnit     = "times/day",
                NormalRangeLow  = 0,
                NormalRangeHigh = 10,
                NormalRangeUnit = "times/day",
                Category        = "Symptom Monitoring"
            },
            new CommonLabUnit {
                MeasurementType = "Exercise Duration (min/day)",
                CommonUnits     = "[\"min/day\"]",
                DefaultUnit     = "min/day",
                NormalRangeLow  = 0,
                NormalRangeHigh = 150,
                NormalRangeUnit = "min/day",
                Category        = "Management Adherence"
            },
            new CommonLabUnit {
                MeasurementType = "Clinical Stage",
                CommonUnits     = "[\"Category\"]",
                DefaultUnit     = "Category",
                NormalRangeUnit = "",
                Category        = "Clinical Assessment"
            },
            new CommonLabUnit {
                MeasurementType = "Final Diagnosis",
                CommonUnits     = "[\"Clinical Note\"]",
                DefaultUnit     = "Clinical Note",
                NormalRangeUnit = "",
                Category        = "Clinical Assessment"
            },
            new CommonLabUnit {
                MeasurementType = "Management Plan",
                CommonUnits     = "[\"Clinical Note\"]",
                DefaultUnit     = "Clinical Note",
                NormalRangeUnit = "",
                Category        = "Clinical Assessment"
            },
        });

        await context.CommonLabUnits.AddRangeAsync(units);
        await context.SaveChangesAsync();
    }
}