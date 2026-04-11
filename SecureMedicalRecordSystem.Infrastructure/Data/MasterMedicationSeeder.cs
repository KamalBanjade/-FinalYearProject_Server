using Microsoft.EntityFrameworkCore;
using SecureMedicalRecordSystem.Core.Entities;

namespace SecureMedicalRecordSystem.Infrastructure.Data;

public static class MasterMedicationSeeder
{
    public static async Task SeedMasterMedicationsAsync(ApplicationDbContext context)
    {
        // Check if already seeded — medications are stable reference data, no force re-seed
        if (await context.MasterMedications.AnyAsync())
            return;

        var medications = new List<MasterMedication>
        {
            // ── ANTIDIABETICS ──
            new MasterMedication
            {
                Name = "Metformin", DrugCategory = "Antidiabetic",
                Aliases = """["Glucophage","metformin HCl","metformin hydrochloride","glycomet","metformin 500mg","metformin 850mg","metformin 1000mg"]""",
                PrimaryMarkers = """["HbA1c","Blood Glucose (Fasting)","Blood Glucose (Postprandial)","Weight","BMI"]""",
                SecondaryMarkers = """["Creatinine (Serum)","eGFR","Insulin Level (Fasting)","HOMA-IR (Insulin Resistance Index)"]"""
            },
            new MasterMedication
            {
                Name = "Glibenclamide", DrugCategory = "Antidiabetic",
                Aliases = """["glyburide","daonil","euglucon"]""",
                PrimaryMarkers = """["Blood Glucose (Fasting)","Blood Glucose (Postprandial)","HbA1c"]""",
                SecondaryMarkers = """["Weight","Insulin Level (Fasting)"]"""
            },
            new MasterMedication
            {
                Name = "Glimepiride", DrugCategory = "Antidiabetic",
                Aliases = """["amaryl","glimpid"]""",
                PrimaryMarkers = """["Blood Glucose (Fasting)","HbA1c","Blood Glucose (Postprandial)"]""",
                SecondaryMarkers = """["Weight","Insulin Level (Fasting)"]"""
            },
            new MasterMedication
            {
                Name = "Sitagliptin", DrugCategory = "Antidiabetic",
                Aliases = """["januvia","sitagliptin phosphate"]""",
                PrimaryMarkers = """["HbA1c","Blood Glucose (Fasting)","Blood Glucose (Postprandial)"]""",
                SecondaryMarkers = """["Weight"]"""
            },
            new MasterMedication
            {
                Name = "Empagliflozin", DrugCategory = "Antidiabetic",
                Aliases = """["jardiance","empagliflozin 10mg","empagliflozin 25mg"]""",
                PrimaryMarkers = """["HbA1c","Blood Glucose (Fasting)","Weight","BMI"]""",
                SecondaryMarkers = """["Systolic Blood Pressure","eGFR","Urine Glucose"]"""
            },
            new MasterMedication
            {
                Name = "Dapagliflozin", DrugCategory = "Antidiabetic",
                Aliases = """["farxiga","forxiga","dapa"]""",
                PrimaryMarkers = """["HbA1c","Blood Glucose (Fasting)","Weight","BMI"]""",
                SecondaryMarkers = """["Systolic Blood Pressure","eGFR"]"""
            },
            new MasterMedication
            {
                Name = "Semaglutide", DrugCategory = "Antidiabetic",
                Aliases = """["ozempic","wegovy","rybelsus","semaglutide injection"]""",
                PrimaryMarkers = """["HbA1c","Blood Glucose (Fasting)","Weight","BMI"]""",
                SecondaryMarkers = """["Cholesterol (Total)","LDL Cholesterol","Systolic Blood Pressure"]"""
            },
            new MasterMedication
            {
                Name = "Liraglutide", DrugCategory = "Antidiabetic",
                Aliases = """["victoza","saxenda"]""",
                PrimaryMarkers = """["HbA1c","Blood Glucose (Fasting)","Weight","BMI"]""",
                SecondaryMarkers = """["Systolic Blood Pressure","Cholesterol (Total)"]"""
            },
            new MasterMedication
            {
                Name = "Insulin Glargine", DrugCategory = "Antidiabetic",
                Aliases = """["lantus","basaglar","toujeo","insulin glargine U-100","long-acting insulin","basal insulin"]""",
                PrimaryMarkers = """["Blood Glucose (Fasting)","HbA1c"]""",
                SecondaryMarkers = """["Weight","Potassium"]"""
            },
            new MasterMedication
            {
                Name = "Insulin Regular", DrugCategory = "Antidiabetic",
                Aliases = """["actrapid","humulin R","short-acting insulin","regular insulin","soluble insulin","insulin"]""",
                PrimaryMarkers = """["Blood Glucose (Postprandial)","Blood Glucose (Random)"]""",
                SecondaryMarkers = """["Potassium","Weight"]"""
            },
            new MasterMedication
            {
                Name = "Pioglitazone", DrugCategory = "Antidiabetic",
                Aliases = """["actos","pioglit"]""",
                PrimaryMarkers = """["HbA1c","Blood Glucose (Fasting)","Insulin Level (Fasting)"]""",
                SecondaryMarkers = """["HDL Cholesterol","Triglycerides","Weight"]"""
            },

            // ── STATINS ──
            new MasterMedication
            {
                Name = "Atorvastatin", DrugCategory = "Statin",
                Aliases = """["lipitor","atorva","atorvastatin calcium","atorvastatin 10mg","atorvastatin 20mg","atorvastatin 40mg","atorvastatin 80mg"]""",
                PrimaryMarkers = """["LDL Cholesterol","Cholesterol (Total)","Triglycerides"]""",
                SecondaryMarkers = """["HDL Cholesterol","ALT (Alanine Aminotransferase)","AST (Aspartate Aminotransferase)","CK (Total Creatine Kinase)"]"""
            },
            new MasterMedication
            {
                Name = "Rosuvastatin", DrugCategory = "Statin",
                Aliases = """["crestor","rozavel","rosuvastatin calcium","rosuvastatin 5mg","rosuvastatin 10mg","rosuvastatin 20mg"]""",
                PrimaryMarkers = """["LDL Cholesterol","Cholesterol (Total)","Triglycerides"]""",
                SecondaryMarkers = """["HDL Cholesterol","ALT (Alanine Aminotransferase)","CK (Total Creatine Kinase)"]"""
            },
            new MasterMedication
            {
                Name = "Simvastatin", DrugCategory = "Statin",
                Aliases = """["zocor","simvastatin 20mg","simvastatin 40mg"]""",
                PrimaryMarkers = """["LDL Cholesterol","Cholesterol (Total)","Triglycerides"]""",
                SecondaryMarkers = """["HDL Cholesterol","ALT (Alanine Aminotransferase)"]"""
            },
            new MasterMedication
            {
                Name = "Pravastatin", DrugCategory = "Statin",
                Aliases = """["pravachol"]""",
                PrimaryMarkers = """["LDL Cholesterol","Cholesterol (Total)"]""",
                SecondaryMarkers = """["HDL Cholesterol","Triglycerides"]"""
            },

            // ── ACE INHIBITORS ──
            new MasterMedication
            {
                Name = "Lisinopril", DrugCategory = "ACE Inhibitor",
                Aliases = """["zestril","prinivil","lisinopril 5mg","lisinopril 10mg","lisinopril 20mg"]""",
                PrimaryMarkers = """["Systolic Blood Pressure","Diastolic Blood Pressure"]""",
                SecondaryMarkers = """["Creatinine (Serum)","eGFR","Potassium","Urine Microalbumin"]"""
            },
            new MasterMedication
            {
                Name = "Enalapril", DrugCategory = "ACE Inhibitor",
                Aliases = """["vasotec","enalapril maleate"]""",
                PrimaryMarkers = """["Systolic Blood Pressure","Diastolic Blood Pressure"]""",
                SecondaryMarkers = """["Creatinine (Serum)","Potassium"]"""
            },
            new MasterMedication
            {
                Name = "Ramipril", DrugCategory = "ACE Inhibitor",
                Aliases = """["altace","cardace","ramipril 2.5mg","ramipril 5mg","ramipril 10mg"]""",
                PrimaryMarkers = """["Systolic Blood Pressure","Diastolic Blood Pressure"]""",
                SecondaryMarkers = """["Creatinine (Serum)","Potassium","Urine Microalbumin"]"""
            },

            // ── ARBs ──
            new MasterMedication
            {
                Name = "Losartan", DrugCategory = "ARB",
                Aliases = """["cozaar","losartan potassium","losartan 25mg","losartan 50mg","losartan 100mg"]""",
                PrimaryMarkers = """["Systolic Blood Pressure","Diastolic Blood Pressure"]""",
                SecondaryMarkers = """["Creatinine (Serum)","Potassium","Urine Microalbumin"]"""
            },
            new MasterMedication
            {
                Name = "Telmisartan", DrugCategory = "ARB",
                Aliases = """["micardis","telma"]""",
                PrimaryMarkers = """["Systolic Blood Pressure","Diastolic Blood Pressure"]""",
                SecondaryMarkers = """["Blood Glucose (Fasting)","Creatinine (Serum)"]"""
            },
            new MasterMedication
            {
                Name = "Valsartan", DrugCategory = "ARB",
                Aliases = """["diovan","valsartan 80mg","valsartan 160mg"]""",
                PrimaryMarkers = """["Systolic Blood Pressure","Diastolic Blood Pressure"]""",
                SecondaryMarkers = """["Creatinine (Serum)","Potassium"]"""
            },

            // ── CALCIUM CHANNEL BLOCKERS ──
            new MasterMedication
            {
                Name = "Amlodipine", DrugCategory = "Calcium Channel Blocker",
                Aliases = """["norvasc","amlodipine besylate","amlodipine 5mg","amlodipine 10mg","amlo"]""",
                PrimaryMarkers = """["Systolic Blood Pressure","Diastolic Blood Pressure","Heart Rate"]""",
                SecondaryMarkers = """[]"""
            },
            new MasterMedication
            {
                Name = "Nifedipine", DrugCategory = "Calcium Channel Blocker",
                Aliases = """["adalat","procardia","nifedipine XL"]""",
                PrimaryMarkers = """["Systolic Blood Pressure","Diastolic Blood Pressure"]""",
                SecondaryMarkers = """["Heart Rate"]"""
            },

            // ── BETA BLOCKERS ──
            new MasterMedication
            {
                Name = "Metoprolol", DrugCategory = "Beta Blocker",
                Aliases = """["lopressor","toprol","metoprolol succinate","metoprolol tartrate","metoprolol 25mg","metoprolol 50mg","metoprolol 100mg"]""",
                PrimaryMarkers = """["Heart Rate","Systolic Blood Pressure","Diastolic Blood Pressure"]""",
                SecondaryMarkers = """["Blood Glucose (Fasting)","Triglycerides"]"""
            },
            new MasterMedication
            {
                Name = "Atenolol", DrugCategory = "Beta Blocker",
                Aliases = """["tenormin","atenolol 25mg","atenolol 50mg","atenolol 100mg"]""",
                PrimaryMarkers = """["Heart Rate","Systolic Blood Pressure","Diastolic Blood Pressure"]""",
                SecondaryMarkers = """[]"""
            },
            new MasterMedication
            {
                Name = "Carvedilol", DrugCategory = "Beta Blocker",
                Aliases = """["coreg","carvedilol 3.125mg","carvedilol 6.25mg","carvedilol 12.5mg"]""",
                PrimaryMarkers = """["Heart Rate","Systolic Blood Pressure"]""",
                SecondaryMarkers = """["Blood Glucose (Fasting)"]"""
            },

            // ── DIURETICS ──
            new MasterMedication
            {
                Name = "Furosemide", DrugCategory = "Diuretic",
                Aliases = """["lasix","frusemide","furosemide 20mg","furosemide 40mg","furosemide 80mg"]""",
                PrimaryMarkers = """["Systolic Blood Pressure","Diastolic Blood Pressure"]""",
                SecondaryMarkers = """["Potassium","Sodium","Creatinine (Serum)","eGFR"]"""
            },
            new MasterMedication
            {
                Name = "Hydrochlorothiazide", DrugCategory = "Diuretic",
                Aliases = """["HCTZ","microzide","hydrochlorothiazide 12.5mg","hydrochlorothiazide 25mg"]""",
                PrimaryMarkers = """["Systolic Blood Pressure","Diastolic Blood Pressure"]""",
                SecondaryMarkers = """["Potassium","Sodium","Blood Glucose (Fasting)","Uric Acid"]"""
            },
            new MasterMedication
            {
                Name = "Spironolactone", DrugCategory = "Diuretic",
                Aliases = """["aldactone","spiro"]""",
                PrimaryMarkers = """["Systolic Blood Pressure","Diastolic Blood Pressure"]""",
                SecondaryMarkers = """["Potassium","Creatinine (Serum)","Aldosterone"]"""
            },

            // ── THYROID ──
            new MasterMedication
            {
                Name = "Levothyroxine", DrugCategory = "Thyroid",
                Aliases = """["synthroid","eltroxin","thyrox","levothyroxine sodium","T4 supplement","levo-T"]""",
                PrimaryMarkers = """["TSH (Thyroid Stimulating Hormone)","Free T4 (fT4)","Free T3 (fT3)"]""",
                SecondaryMarkers = """["Heart Rate","BMI","Cholesterol (Total)","LDL Cholesterol"]"""
            },
            new MasterMedication
            {
                Name = "Carbimazole", DrugCategory = "Thyroid",
                Aliases = """["neomercazole","carbimazole 5mg","carbimazole 10mg"]""",
                PrimaryMarkers = """["TSH (Thyroid Stimulating Hormone)","Free T4 (fT4)","Free T3 (fT3)"]""",
                SecondaryMarkers = """["WBC Count"]"""
            },

            // ── NSAIDs & ANALGESICS ──
            new MasterMedication
            {
                Name = "Aspirin", DrugCategory = "NSAID",
                Aliases = """["acetylsalicylic acid","ASA","disprin","ecosprin","aspirin 75mg","aspirin 150mg","aspirin 325mg","low-dose aspirin","baby aspirin"]""",
                PrimaryMarkers = """["CRP (C-Reactive Protein)","hs-CRP (High-Sensitivity CRP)"]""",
                SecondaryMarkers = """["Platelet Count","Bleeding Time","aPTT (Activated Partial Thromboplastin Time)"]"""
            },
            new MasterMedication
            {
                Name = "Ibuprofen", DrugCategory = "NSAID",
                Aliases = """["brufen","advil","nurofen","ibuprofen 200mg","ibuprofen 400mg","ibuprofen 600mg"]""",
                PrimaryMarkers = """["CRP (C-Reactive Protein)","ESR (Erythrocyte Sedimentation Rate)"]""",
                SecondaryMarkers = """["Creatinine (Serum)","Systolic Blood Pressure"]"""
            },
            new MasterMedication
            {
                Name = "Paracetamol", DrugCategory = "Analgesic",
                Aliases = """["acetaminophen","tylenol","calpol","panadol","paracetamol 500mg","paracetamol 650mg","paracetamol 1000mg"]""",
                PrimaryMarkers = """["ALT (Alanine Aminotransferase)","AST (Aspartate Aminotransferase)"]""",
                SecondaryMarkers = """["Bilirubin (Total)","INR (International Normalised Ratio)"]"""
            },
            new MasterMedication
            {
                Name = "Diclofenac", DrugCategory = "NSAID",
                Aliases = """["voltaren","voveran","diclofenac sodium","diclofenac potassium"]""",
                PrimaryMarkers = """["CRP (C-Reactive Protein)","ESR (Erythrocyte Sedimentation Rate)"]""",
                SecondaryMarkers = """["Creatinine (Serum)","ALT (Alanine Aminotransferase)"]"""
            },

            // ── ANTIBIOTICS ──
            new MasterMedication
            {
                Name = "Amoxicillin", DrugCategory = "Antibiotic",
                Aliases = """["amoxil","trimox","amoxicillin 250mg","amoxicillin 500mg","amoxicillin 875mg"]""",
                PrimaryMarkers = """["WBC Count","CRP (C-Reactive Protein)","ESR (Erythrocyte Sedimentation Rate)"]""",
                SecondaryMarkers = """["Neutrophils (%)","Procalcitonin (PCT)"]"""
            },
            new MasterMedication
            {
                Name = "Azithromycin", DrugCategory = "Antibiotic",
                Aliases = """["zithromax","azithral","azee","azithromycin 250mg","azithromycin 500mg","Z-pack"]""",
                PrimaryMarkers = """["WBC Count","CRP (C-Reactive Protein)"]""",
                SecondaryMarkers = """["Neutrophils (%)","Procalcitonin (PCT)"]"""
            },
            new MasterMedication
            {
                Name = "Ciprofloxacin", DrugCategory = "Antibiotic",
                Aliases = """["cipro","cifran","ciplox","ciprofloxacin 250mg","ciprofloxacin 500mg","ciprofloxacin 750mg"]""",
                PrimaryMarkers = """["WBC Count","CRP (C-Reactive Protein)","Procalcitonin (PCT)"]""",
                SecondaryMarkers = """["Creatinine (Serum)"]"""
            },
            new MasterMedication
            {
                Name = "Doxycycline", DrugCategory = "Antibiotic",
                Aliases = """["vibramycin","doxycycline hyclate","doxycycline 100mg"]""",
                PrimaryMarkers = """["WBC Count","CRP (C-Reactive Protein)"]""",
                SecondaryMarkers = """[]"""
            },

            // ── PPIs ──
            new MasterMedication
            {
                Name = "Omeprazole", DrugCategory = "PPI",
                Aliases = """["prilosec","losec","omez","omeprazole 20mg","omeprazole 40mg"]""",
                PrimaryMarkers = """["ALT (Alanine Aminotransferase)"]""",
                SecondaryMarkers = """["Magnesium","Vitamin B12 (Cobalamin)","Calcium (Total)"]"""
            },
            new MasterMedication
            {
                Name = "Pantoprazole", DrugCategory = "PPI",
                Aliases = """["protonix","pantop","pantoprazole 40mg"]""",
                PrimaryMarkers = """["ALT (Alanine Aminotransferase)"]""",
                SecondaryMarkers = """["Magnesium","Vitamin B12 (Cobalamin)"]"""
            },
            new MasterMedication
            {
                Name = "Esomeprazole", DrugCategory = "PPI",
                Aliases = """["nexium","esomep","esomeprazole 20mg","esomeprazole 40mg"]""",
                PrimaryMarkers = """["ALT (Alanine Aminotransferase)"]""",
                SecondaryMarkers = """["Magnesium","Vitamin B12 (Cobalamin)"]"""
            },

            // ── ANTICOAGULANTS ──
            new MasterMedication
            {
                Name = "Warfarin", DrugCategory = "Anticoagulant",
                Aliases = """["coumadin","warfarin sodium","warfarin 1mg","warfarin 2mg","warfarin 5mg"]""",
                PrimaryMarkers = """["INR (International Normalised Ratio)","Prothrombin Time (PT)"]""",
                SecondaryMarkers = """["aPTT (Activated Partial Thromboplastin Time)","D-Dimer"]"""
            },
            new MasterMedication
            {
                Name = "Rivaroxaban", DrugCategory = "Anticoagulant",
                Aliases = """["xarelto","rivaroxaban 10mg","rivaroxaban 15mg","rivaroxaban 20mg"]""",
                PrimaryMarkers = """["D-Dimer","INR (International Normalised Ratio)"]""",
                SecondaryMarkers = """["Creatinine (Serum)","Hemoglobin"]"""
            },
            new MasterMedication
            {
                Name = "Heparin", DrugCategory = "Anticoagulant",
                Aliases = """["unfractionated heparin","UFH","heparin sodium","heparin infusion"]""",
                PrimaryMarkers = """["aPTT (Activated Partial Thromboplastin Time)","Platelet Count"]""",
                SecondaryMarkers = """["D-Dimer","Potassium"]"""
            },

            // ── SSRIs ──
            new MasterMedication
            {
                Name = "Sertraline", DrugCategory = "SSRI",
                Aliases = """["zoloft","lustral","sertraline 25mg","sertraline 50mg","sertraline 100mg"]""",
                PrimaryMarkers = """["Sodium"]""",
                SecondaryMarkers = """["Platelet Count","Blood Glucose (Fasting)"]"""
            },
            new MasterMedication
            {
                Name = "Fluoxetine", DrugCategory = "SSRI",
                Aliases = """["prozac","fluoxetine 10mg","fluoxetine 20mg","fluoxetine 40mg"]""",
                PrimaryMarkers = """["Sodium"]""",
                SecondaryMarkers = """["Blood Glucose (Fasting)","Weight"]"""
            },

            // ── RESPIRATORY ──
            new MasterMedication
            {
                Name = "Salbutamol", DrugCategory = "Bronchodilator",
                Aliases = """["albuterol","ventolin","salbutamol inhaler","proventil","salbutamol 2.5mg nebule"]""",
                PrimaryMarkers = """["SpO2 (Oxygen Saturation)","Respiratory Rate"]""",
                SecondaryMarkers = """["Heart Rate","Potassium"]"""
            },
            new MasterMedication
            {
                Name = "Fluticasone", DrugCategory = "Inhaled Corticosteroid",
                Aliases = """["flixotide","flovent","fluticasone propionate","fluticasone inhaler"]""",
                PrimaryMarkers = """["SpO2 (Oxygen Saturation)","Eosinophils (%)"]""",
                SecondaryMarkers = """["Blood Glucose (Fasting)","Cortisol (Morning)"]"""
            },
            new MasterMedication
            {
                Name = "Montelukast", DrugCategory = "Leukotriene Antagonist",
                Aliases = """["singulair","montelukast 5mg","montelukast 10mg"]""",
                PrimaryMarkers = """["Eosinophils (%)","SpO2 (Oxygen Saturation)"]""",
                SecondaryMarkers = """["IgE (Total)"]"""
            },

            // ── SUPPLEMENTS ──
            new MasterMedication
            {
                Name = "Vitamin D3", DrugCategory = "Supplement",
                Aliases = """["cholecalciferol","vitamin D","calciferol","vitD","vitamin D3 1000IU","vitamin D3 2000IU","vitamin D3 5000IU","D3"]""",
                PrimaryMarkers = """["Vitamin D (25-Hydroxy)"]""",
                SecondaryMarkers = """["Calcium (Total)","PTH (Parathyroid Hormone)"]"""
            },
            new MasterMedication
            {
                Name = "Folic Acid", DrugCategory = "Supplement",
                Aliases = """["folate","vitamin B9","folic acid 400mcg","folic acid 5mg"]""",
                PrimaryMarkers = """["Folate (Folic Acid)","Hemoglobin"]""",
                SecondaryMarkers = """["MCV (Mean Corpuscular Volume)","Homocysteine"]"""
            },
            new MasterMedication
            {
                Name = "Ferrous Sulfate", DrugCategory = "Supplement",
                Aliases = """["iron supplement","ferrous sulphate","iron tablet","iron 325mg","feosol","fer-in-sol"]""",
                PrimaryMarkers = """["Hemoglobin","Ferritin","Iron (Serum)"]""",
                SecondaryMarkers = """["MCV (Mean Corpuscular Volume)","Hematocrit","Transferrin Saturation"]"""
            },
            new MasterMedication
            {
                Name = "Calcium Carbonate", DrugCategory = "Supplement",
                Aliases = """["caltrate","tums","calcium supplement","calcium 500mg","calcium 1000mg"]""",
                PrimaryMarkers = """["Calcium (Total)"]""",
                SecondaryMarkers = """["PTH (Parathyroid Hormone)","Vitamin D (25-Hydroxy)"]"""
            },
        };

        context.MasterMedications.AddRange(medications);
        await context.SaveChangesAsync();
    }
}
