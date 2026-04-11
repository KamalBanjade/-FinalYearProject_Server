namespace SecureMedicalRecordSystem.Core.Entities;

public class MasterMedication
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    // Canonical name, e.g. "Metformin" — this is what gets saved to
    // Prescription.MedicationName after normalization

    public string? Aliases { get; set; }
    // JSON array of alternate names/brands/spellings the doctor might type
    // e.g. ["Glucophage", "metformin HCl", "metformin hydrochloride"]

    public string DrugCategory { get; set; } = string.Empty;
    // e.g. "Antidiabetic", "Statin", "ACE Inhibitor", "Beta Blocker"

    public string? PrimaryMarkers { get; set; }
    // JSON array of CommonLabUnit.MeasurementType values this drug primarily affects

    public string? SecondaryMarkers { get; set; }
    // JSON array of secondary affected markers
}
