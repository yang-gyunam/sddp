using Sddp.Abstractions.Enums;
using Sddp.Abstractions.ValueObjects;

namespace Sddp.Abstractions.Constants;

/// <summary>
/// Well-known identifiers shared by SQL and EF authorization seed paths.
/// </summary>
public static class AuthorizationSeedIds
{
    private static readonly IReadOnlyDictionary<RoleType, string> RoleIdValues =
        new Dictionary<RoleType, string>
        {
            [RoleType.Admin] = "00000000-0000-0000-0002-000000000001",
            [RoleType.ProductOwner] = "00000000-0000-0000-0002-000000000002",
            [RoleType.DomainExpert] = "00000000-0000-0000-0002-000000000003",
            [RoleType.Developer] = "00000000-0000-0000-0002-000000000004",
            [RoleType.Reviewer] = "00000000-0000-0000-0002-000000000005",
            [RoleType.QATester] = "00000000-0000-0000-0002-000000000006",

        };

    private static readonly IReadOnlyDictionary<string, string> PermissionIdValues =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [PermissionCodes.SpecCreate] = "00000000-0000-0000-0003-000000000001",
            [PermissionCodes.SpecRead] = "00000000-0000-0000-0003-000000000002",
            [PermissionCodes.SpecUpdate] = "00000000-0000-0000-0003-000000000003",
            [PermissionCodes.SpecDelete] = "00000000-0000-0000-0003-000000000004",
            [PermissionCodes.SpecApprove] = "00000000-0000-0000-0003-000000000005",
            [PermissionCodes.SpecLock] = "00000000-0000-0000-0003-000000000006",
            [PermissionCodes.ConversationCreate] = "00000000-0000-0000-0003-000000000011",
            [PermissionCodes.ConversationRead] = "00000000-0000-0000-0003-000000000012",
            [PermissionCodes.ConversationPost] = "00000000-0000-0000-0003-000000000013",
            [PermissionCodes.ConversationClose] = "00000000-0000-0000-0003-000000000014",
            [PermissionCodes.RequirementCreate] = "00000000-0000-0000-0003-000000000021",
            [PermissionCodes.RequirementRead] = "00000000-0000-0000-0003-000000000022",
            [PermissionCodes.RequirementUpdate] = "00000000-0000-0000-0003-000000000023",
            [PermissionCodes.RequirementDelete] = "00000000-0000-0000-0003-000000000024",
            [PermissionCodes.GenerationExecute] = "00000000-0000-0000-0003-000000000031",
            [PermissionCodes.GenerationRollback] = "00000000-0000-0000-0003-000000000032",
            [PermissionCodes.GlossaryCreate] = "00000000-0000-0000-0003-000000000041",
            [PermissionCodes.GlossaryRead] = "00000000-0000-0000-0003-000000000042",
            [PermissionCodes.GlossaryUpdate] = "00000000-0000-0000-0003-000000000043",
            [PermissionCodes.GlossaryDeprecate] = "00000000-0000-0000-0003-000000000044",
            [PermissionCodes.UserManage] = "00000000-0000-0000-0003-000000000051",
            [PermissionCodes.RoleAssign] = "00000000-0000-0000-0003-000000000061",
            [PermissionCodes.AuditRead] = "00000000-0000-0000-0003-000000000062",
            [PermissionCodes.ProjectCreate] = "00000000-0000-0000-0003-000000000071",
            [PermissionCodes.ProjectRead] = "00000000-0000-0000-0003-000000000072",
            [PermissionCodes.ProjectUpdate] = "00000000-0000-0000-0003-000000000073",
            [PermissionCodes.ProjectDelete] = "00000000-0000-0000-0003-000000000074",
            [PermissionCodes.TaskCreate] = "00000000-0000-0000-0003-000000000081",
            [PermissionCodes.TaskRead] = "00000000-0000-0000-0003-000000000082",
            [PermissionCodes.TaskUpdate] = "00000000-0000-0000-0003-000000000083",
            [PermissionCodes.TaskDelete] = "00000000-0000-0000-0003-000000000084",
            [PermissionCodes.KnowledgeRead] = "00000000-0000-0000-0003-000000000091",
            [PermissionCodes.KnowledgeCreate] = "00000000-0000-0000-0003-000000000092",
            [PermissionCodes.KnowledgeUpdate] = "00000000-0000-0000-0003-000000000093",
            [PermissionCodes.KnowledgeArchive] = "00000000-0000-0000-0003-000000000094",
            [PermissionCodes.KnowledgePublish] = "00000000-0000-0000-0003-000000000095",
            [PermissionCodes.KnowledgeAdmin] = "00000000-0000-0000-0003-000000000096"
        };

    public static IReadOnlyCollection<RoleType> SystemRoleTypes { get; } =
        RoleIdValues.Keys.ToArray();

    public static IReadOnlyCollection<string> SystemPermissionCodes { get; } =
        PermissionIdValues.Keys.ToArray();

    public static GlobalUniqueId GetRoleId(RoleType roleType) =>
        GlobalUniqueId.Parse(RoleIdValues.TryGetValue(roleType, out var value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(roleType), roleType, "Unknown system role type."));

    public static GlobalUniqueId GetPermissionId(string permissionCode) =>
        GlobalUniqueId.Parse(PermissionIdValues.TryGetValue(permissionCode, out var value)
            ? value
            : throw new ArgumentOutOfRangeException(
                nameof(permissionCode), permissionCode, "Unknown system permission code."));
}
