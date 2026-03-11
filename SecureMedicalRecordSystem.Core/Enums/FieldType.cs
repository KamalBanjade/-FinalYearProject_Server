namespace SecureMedicalRecordSystem.Core.Enums;

public enum FieldType
{
    Number = 0,      // Numeric value with optional unit
    Text = 1,        // Short text (single line)
    LongText = 2,    // Long text (multiple lines)
    Boolean = 3,     // Yes/No, True/False
    Date = 4,        // Date only
    DateTime = 5,    // Date and time
    Dropdown = 6     // Select from predefined options
}
