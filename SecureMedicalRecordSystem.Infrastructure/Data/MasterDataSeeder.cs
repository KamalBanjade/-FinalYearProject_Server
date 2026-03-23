using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.Entities;
using System.Text.Json;

namespace SecureMedicalRecordSystem.Infrastructure.Data;

public static class MasterDataSeeder
{
    public static async Task SeedCommonLabUnitsAsync(ApplicationDbContext context)
    {
        if (await context.CommonLabUnits.AnyAsync())
        {
            return;
        }

        var units = new List<CommonLabUnit>
        {
            // Blood Glucose & Diabetes
            new CommonLabUnit { 
                MeasurementType = "Blood Glucose", 
                CommonUnits = "[\"mg/dL\", \"mmol/L\"]", 
                DefaultUnit = "mg/dL", 
                NormalRangeLow = 70, 
                NormalRangeHigh = 99, 
                NormalRangeUnit = "mg/dL",
                Aliases = "[\"glucose\", \"blood sugar\"]",
                Category = "Diabetes Monitoring"
            },
            new CommonLabUnit { 
                MeasurementType = "HbA1c", 
                CommonUnits = "[\"%\", \"mmol/mol\"]", 
                DefaultUnit = "%", 
                NormalRangeLow = 4.0m, 
                NormalRangeHigh = 5.6m, 
                NormalRangeUnit = "%",
                Aliases = "[\"a1c\", \"glycated hemoglobin\"]",
                Category = "Diabetes Monitoring"
            },
            new CommonLabUnit { 
                MeasurementType = "Insulin Level", 
                CommonUnits = "[\"µIU/mL\", \"pmol/L\"]", 
                DefaultUnit = "µIU/mL", 
                NormalRangeLow = 2.6m, 
                NormalRangeHigh = 24.9m, 
                NormalRangeUnit = "µIU/mL",
                Category = "Diabetes Monitoring"
            },

            // Electrolytes
            new CommonLabUnit { 
                MeasurementType = "Potassium", 
                CommonUnits = "[\"mEq/L\", \"mmol/L\"]", 
                DefaultUnit = "mEq/L", 
                NormalRangeLow = 3.5m, 
                NormalRangeHigh = 5.1m, 
                NormalRangeUnit = "mEq/L",
                Aliases = "[\"K\"]",
                Category = "Electrolytes"
            },
            new CommonLabUnit { 
                MeasurementType = "Sodium", 
                CommonUnits = "[\"mEq/L\", \"mmol/L\"]", 
                DefaultUnit = "mEq/L", 
                NormalRangeLow = 136, 
                NormalRangeHigh = 145, 
                NormalRangeUnit = "mEq/L",
                Aliases = "[\"Na\"]",
                Category = "Electrolytes"
            },
            new CommonLabUnit { 
                MeasurementType = "Chloride", 
                CommonUnits = "[\"mEq/L\", \"mmol/L\"]", 
                DefaultUnit = "mEq/L", 
                NormalRangeLow = 98, 
                NormalRangeHigh = 107, 
                NormalRangeUnit = "mEq/L",
                Aliases = "[\"Cl\"]",
                Category = "Electrolytes"
            },
            new CommonLabUnit { 
                MeasurementType = "Calcium (Total)", 
                CommonUnits = "[\"mg/dL\", \"mmol/L\"]", 
                DefaultUnit = "mg/dL", 
                NormalRangeLow = 8.5m, 
                NormalRangeHigh = 10.2m, 
                NormalRangeUnit = "mg/dL",
                Aliases = "[\"Ca\"]",
                Category = "Electrolytes"
            },
            new CommonLabUnit { 
                MeasurementType = "Magnesium", 
                CommonUnits = "[\"mg/dL\", \"mmol/L\"]", 
                DefaultUnit = "mg/dL", 
                NormalRangeLow = 1.7m, 
                NormalRangeHigh = 2.2m, 
                NormalRangeUnit = "mg/dL",
                Aliases = "[\"Mg\"]",
                Category = "Electrolytes"
            },

            // Kidney Function
            new CommonLabUnit { 
                MeasurementType = "Creatinine", 
                CommonUnits = "[\"mg/dL\", \"µmol/L\"]", 
                DefaultUnit = "mg/dL", 
                NormalRangeLow = 0.7m, 
                NormalRangeHigh = 1.3m, 
                NormalRangeUnit = "mg/dL",
                Category = "Kidney Function"
            },
            new CommonLabUnit { 
                MeasurementType = "BUN (Blood Urea Nitrogen)", 
                CommonUnits = "[\"mg/dL\", \"mmol/L\"]", 
                DefaultUnit = "mg/dL", 
                NormalRangeLow = 7, 
                NormalRangeHigh = 20, 
                NormalRangeUnit = "mg/dL",
                Category = "Kidney Function"
            },
            new CommonLabUnit { 
                MeasurementType = "eGFR", 
                CommonUnits = "[\"mL/min/1.73m²\"]", 
                DefaultUnit = "mL/min/1.73m²", 
                NormalRangeLow = 90, 
                NormalRangeHigh = 120, 
                NormalRangeUnit = "mL/min/1.73m²",
                Aliases = "[\"Glomerular Filtration Rate\"]",
                Category = "Kidney Function"
            },

            // Liver Function
            new CommonLabUnit { 
                MeasurementType = "ALT (Alanine Aminotransferase)", 
                CommonUnits = "[\"U/L\"]", 
                DefaultUnit = "U/L", 
                NormalRangeLow = 7, 
                NormalRangeHigh = 55, 
                NormalRangeUnit = "U/L",
                Category = "Liver Function"
            },
            new CommonLabUnit { 
                MeasurementType = "AST (Aspartate Aminotransferase)", 
                CommonUnits = "[\"U/L\"]", 
                DefaultUnit = "U/L", 
                NormalRangeLow = 8, 
                NormalRangeHigh = 48, 
                NormalRangeUnit = "U/L",
                Category = "Liver Function"
            },
            new CommonLabUnit { 
                MeasurementType = "ALP (Alkaline Phosphatase)", 
                CommonUnits = "[\"U/L\"]", 
                DefaultUnit = "U/L", 
                NormalRangeLow = 40, 
                NormalRangeHigh = 129, 
                NormalRangeUnit = "U/L",
                Category = "Liver Function"
            },
            new CommonLabUnit { 
                MeasurementType = "Bilirubin (Total)", 
                CommonUnits = "[\"mg/dL\", \"µmol/L\"]", 
                DefaultUnit = "mg/dL", 
                NormalRangeLow = 0.1m, 
                NormalRangeHigh = 1.2m, 
                NormalRangeUnit = "mg/dL",
                Category = "Liver Function"
            },

            // Lipids
            new CommonLabUnit { 
                MeasurementType = "Cholesterol (Total)", 
                CommonUnits = "[\"mg/dL\", \"mmol/L\"]", 
                DefaultUnit = "mg/dL", 
                NormalRangeLow = 125, 
                NormalRangeHigh = 200, 
                NormalRangeUnit = "mg/dL",
                Category = "Lipid Profile"
            },
            new CommonLabUnit { 
                MeasurementType = "HDL Cholesterol", 
                CommonUnits = "[\"mg/dL\", \"mmol/L\"]", 
                DefaultUnit = "mg/dL", 
                NormalRangeLow = 40, 
                NormalRangeHigh = 60, 
                NormalRangeUnit = "mg/dL",
                Aliases = "[\"Good Cholesterol\"]",
                Category = "Lipid Profile"
            },
            new CommonLabUnit { 
                MeasurementType = "LDL Cholesterol", 
                CommonUnits = "[\"mg/dL\", \"mmol/L\"]", 
                DefaultUnit = "mg/dL", 
                NormalRangeHigh = 100, 
                NormalRangeUnit = "mg/dL",
                Aliases = "[\"Bad Cholesterol\"]",
                Category = "Lipid Profile"
            },
            new CommonLabUnit { 
                MeasurementType = "Triglycerides", 
                CommonUnits = "[\"mg/dL\", \"mmol/L\"]", 
                DefaultUnit = "mg/dL", 
                NormalRangeHigh = 150, 
                NormalRangeUnit = "mg/dL",
                Category = "Lipid Profile"
            },

            // CBC (Complete Blood Count)
            new CommonLabUnit { 
                MeasurementType = "Hemoglobin", 
                CommonUnits = "[\"g/dL\", \"g/L\"]", 
                DefaultUnit = "g/dL", 
                NormalRangeLow = 13.5m, 
                NormalRangeHigh = 17.5m, 
                NormalRangeUnit = "g/dL",
                Aliases = "[\"Hgb\", \"Hb\"]",
                Category = "Complete Blood Count"
            },
            new CommonLabUnit { 
                MeasurementType = "Hematocrit", 
                CommonUnits = "[\"%\", \"L/L\"]", 
                DefaultUnit = "%", 
                NormalRangeLow = 38.8m, 
                NormalRangeHigh = 50.0m, 
                NormalRangeUnit = "%",
                Aliases = "[\"Hct\"]",
                Category = "Complete Blood Count"
            },
            new CommonLabUnit { 
                MeasurementType = "White Blood Cell Count (WBC)", 
                CommonUnits = "[\"x10³/µL\", \"x10⁹/L\"]", 
                DefaultUnit = "x10³/µL", 
                NormalRangeLow = 4.5m, 
                NormalRangeHigh = 11.0m, 
                NormalRangeUnit = "x10³/µL",
                Category = "Complete Blood Count"
            },
            new CommonLabUnit { 
                MeasurementType = "Platelet Count", 
                CommonUnits = "[\"x10³/µL\", \"x10⁹/L\"]", 
                DefaultUnit = "x10³/µL", 
                NormalRangeLow = 150, 
                NormalRangeHigh = 450, 
                NormalRangeUnit = "x10³/µL",
                Category = "Complete Blood Count"
            },

            // Thyroid
            new CommonLabUnit { 
                MeasurementType = "TSH (Thyroid Stimulating Hormone)", 
                CommonUnits = "[\"mIU/L\", \"µIU/mL\"]", 
                DefaultUnit = "mIU/L", 
                NormalRangeLow = 0.45m, 
                NormalRangeHigh = 4.5m, 
                NormalRangeUnit = "mIU/L",
                Category = "Thyroid Function"
            },
            new CommonLabUnit { 
                MeasurementType = "Free T4", 
                CommonUnits = "[\"ng/dL\", \"pmol/L\"]", 
                DefaultUnit = "ng/dL", 
                NormalRangeLow = 0.8m, 
                NormalRangeHigh = 1.8m, 
                NormalRangeUnit = "ng/dL",
                Category = "Thyroid Function"
            },
            new CommonLabUnit { 
                MeasurementType = "Total T3", 
                CommonUnits = "[\"ng/dL\", \"nmol/L\"]", 
                DefaultUnit = "ng/dL", 
                NormalRangeLow = 80, 
                NormalRangeHigh = 200, 
                NormalRangeUnit = "ng/dL",
                Category = "Thyroid Function"
            },

            // Vitamins & Minerals
            new CommonLabUnit { 
                MeasurementType = "Vitamin D (25-Hydroxy)", 
                CommonUnits = "[\"ng/mL\", \"nmol/L\"]", 
                DefaultUnit = "ng/mL", 
                NormalRangeLow = 30, 
                NormalRangeHigh = 100, 
                NormalRangeUnit = "ng/mL",
                Category = "Vitamins"
            },
            new CommonLabUnit { 
                MeasurementType = "Vitamin B12", 
                CommonUnits = "[\"pg/mL\", \"pmol/L\"]", 
                DefaultUnit = "pg/mL", 
                NormalRangeLow = 200, 
                NormalRangeHigh = 900, 
                NormalRangeUnit = "pg/mL",
                Category = "Vitamins"
            },
            new CommonLabUnit { 
                MeasurementType = "Folate", 
                CommonUnits = "[\"ng/mL\", \"nmol/L\"]", 
                DefaultUnit = "ng/mL", 
                NormalRangeLow = 2.7m, 
                NormalRangeHigh = 17.0m, 
                NormalRangeUnit = "ng/mL",
                Category = "Vitamins"
            },
            new CommonLabUnit { 
                MeasurementType = "Ferritin", 
                CommonUnits = "[\"ng/mL\", \"µg/L\"]", 
                DefaultUnit = "ng/mL", 
                NormalRangeLow = 20, 
                NormalRangeHigh = 500, 
                NormalRangeUnit = "ng/mL",
                Category = "Vitamins"
            },

            // Cardiac Markers
            new CommonLabUnit { 
                MeasurementType = "Troponin I", 
                CommonUnits = "[\"ng/mL\", \"µg/L\"]", 
                DefaultUnit = "ng/mL", 
                NormalRangeHigh = 0.04m, 
                NormalRangeUnit = "ng/mL",
                Category = "Cardiac Markers"
            },
            new CommonLabUnit { 
                MeasurementType = "BNP (B-type Natriuretic Peptide)", 
                CommonUnits = "[\"pg/mL\"]", 
                DefaultUnit = "pg/mL", 
                NormalRangeHigh = 100, 
                NormalRangeUnit = "pg/mL",
                Category = "Cardiac Markers"
            }
        };

        // Add more to reach 100+ if needed, but these cover the major areas.
        // I will add another batch to ensure high coverage.
        
        var inflammationBatch = new List<CommonLabUnit>
        {
            new CommonLabUnit { MeasurementType = "CRP (C-Reactive Protein)", CommonUnits = "[\"mg/L\", \"mg/dL\"]", DefaultUnit = "mg/L", NormalRangeHigh = 3.0m, NormalRangeUnit = "mg/L", Category = "Inflammation" },
            new CommonLabUnit { MeasurementType = "ESR (Sedimentation Rate)", CommonUnits = "[\"mm/hr\"]", DefaultUnit = "mm/hr", NormalRangeLow = 0, NormalRangeHigh = 20, NormalRangeUnit = "mm/hr", Category = "Inflammation" }
        };
        units.AddRange(inflammationBatch);

        // Urine Tests
        var urineBatch = new List<CommonLabUnit>
        {
            new CommonLabUnit { MeasurementType = "Urine Specific Gravity", CommonUnits = "[\"\"]", DefaultUnit = "", NormalRangeLow = 1.005m, NormalRangeHigh = 1.030m, Category = "Urinalysis" },
            new CommonLabUnit { MeasurementType = "Urine pH", CommonUnits = "[\"\"]", DefaultUnit = "", NormalRangeLow = 4.5m, NormalRangeHigh = 8.0m, Category = "Urinalysis" },
            new CommonLabUnit { MeasurementType = "Urine Protein", CommonUnits = "[\"mg/dL\", \"g/L\"]", DefaultUnit = "mg/dL", NormalRangeHigh = 30, NormalRangeUnit = "mg/dL", Category = "Urinalysis" }
        };
        units.AddRange(urineBatch);

        // Electrolytes (More)
        var electroBatch2 = new List<CommonLabUnit>
        {
            new CommonLabUnit { MeasurementType = "Phosphorus", CommonUnits = "[\"mg/dL\", \"mmol/L\"]", DefaultUnit = "mg/dL", NormalRangeLow = 2.5m, NormalRangeHigh = 4.5m, NormalRangeUnit = "mg/dL", Category = "Electrolytes" },
            new CommonLabUnit { MeasurementType = "Uric Acid", CommonUnits = "[\"mg/dL\", \"µmol/L\"]", DefaultUnit = "mg/dL", NormalRangeLow = 3.5m, NormalRangeHigh = 7.2m, NormalRangeUnit = "mg/dL", Category = "Electrolytes" }
        };
        units.AddRange(electroBatch2);

        await context.CommonLabUnits.AddRangeAsync(units);
        await context.SaveChangesAsync();
    }
}
