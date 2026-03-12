namespace Sddp.Abstractions.Enums;

/// <summary>
/// Glossary term category
/// </summary>
public enum TermCategory
{
    /// <summary>Technical term - related to technical concepts and implementation</summary>
    Technical = 0,

    /// <summary>Business term - related to domain and business operations</summary>
    Business = 1,

    /// <summary>Abbreviation - acronyms and shortened forms</summary>
    Abbreviation = 2,

    /// <summary>Domain term - specialized terminology for a specific domain</summary>
    Domain = 3,

    /// <summary>Architecture term</summary>
    Architecture = 4,

    /// <summary>Infrastructure term</summary>
    Infrastructure = 5,

    /// <summary>Security term</summary>
    Security = 6,

    /// <summary>Compliance term</summary>
    Compliance = 7,

    /// <summary>Design pattern term</summary>
    DesignPattern = 8
}
