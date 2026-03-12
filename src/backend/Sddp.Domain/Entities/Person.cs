using Sddp.Abstractions.Base;

namespace Sddp.Domain.Entities;

/// <summary>
/// personal entity (persons)
/// </summary>
public class Person : EntityBase
{
    public string DisplayName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? Organization { get; private set; }
    public string PersonType { get; private set; } = "INTERNAL";
    public string Timezone { get; private set; } = "Asia/Seoul";
    public string Locale { get; private set; } = "ko-KR";

    private Person() { }

    public Person(string displayName, string? email, string personType = "INTERNAL")
    {
        DisplayName = displayName;
        Email = email;
        PersonType = personType;
    }
}
