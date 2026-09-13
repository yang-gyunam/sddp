using Sddp.Abstractions.Enums;

namespace Sddp.Abstractions.Constants;

/// <summary>
/// Public distribution role-permission matrix.
/// </summary>
public static class RolePermissionMatrix
{
    public static IReadOnlyCollection<string> GetPermissions(RoleType roleType) => roleType switch
    {
        RoleType.Admin => PermissionCodes.All,
        RoleType.ProductOwner =>
        [
            PermissionCodes.SpecCreate, PermissionCodes.SpecRead, PermissionCodes.SpecUpdate,
            PermissionCodes.SpecDelete, PermissionCodes.SpecApprove, PermissionCodes.SpecLock,
            PermissionCodes.ConversationCreate, PermissionCodes.ConversationRead,
            PermissionCodes.ConversationPost, PermissionCodes.ConversationClose,
            PermissionCodes.RequirementCreate, PermissionCodes.RequirementRead,
            PermissionCodes.RequirementUpdate, PermissionCodes.RequirementDelete,
            PermissionCodes.GenerationExecute, PermissionCodes.GenerationRollback,
            PermissionCodes.GlossaryCreate, PermissionCodes.GlossaryRead,
            PermissionCodes.GlossaryUpdate, PermissionCodes.GlossaryDeprecate,
            PermissionCodes.RoleAssign, PermissionCodes.AuditRead,
            PermissionCodes.ProjectCreate, PermissionCodes.ProjectRead, PermissionCodes.ProjectUpdate,
            PermissionCodes.TaskCreate, PermissionCodes.TaskRead,
            PermissionCodes.TaskUpdate, PermissionCodes.TaskDelete,
            PermissionCodes.KnowledgeRead, PermissionCodes.KnowledgeCreate,
            PermissionCodes.KnowledgeUpdate, PermissionCodes.KnowledgeArchive,
            PermissionCodes.KnowledgePublish, PermissionCodes.KnowledgeAdmin
        ],
        RoleType.DomainExpert =>
        [
            PermissionCodes.SpecCreate, PermissionCodes.SpecRead, PermissionCodes.SpecUpdate,
            PermissionCodes.ConversationCreate, PermissionCodes.ConversationRead,
            PermissionCodes.ConversationPost, PermissionCodes.ConversationClose,
            PermissionCodes.RequirementCreate, PermissionCodes.RequirementRead,
            PermissionCodes.RequirementUpdate,
            PermissionCodes.GlossaryCreate, PermissionCodes.GlossaryRead,
            PermissionCodes.GlossaryUpdate, PermissionCodes.GlossaryDeprecate,
            PermissionCodes.ProjectRead,
            PermissionCodes.TaskCreate, PermissionCodes.TaskRead, PermissionCodes.TaskUpdate,
            PermissionCodes.KnowledgeRead, PermissionCodes.KnowledgeCreate,
            PermissionCodes.KnowledgeUpdate, PermissionCodes.KnowledgePublish
        ],
        RoleType.Developer =>
        [
            PermissionCodes.SpecCreate, PermissionCodes.SpecRead, PermissionCodes.SpecUpdate,
            PermissionCodes.ConversationCreate, PermissionCodes.ConversationRead,
            PermissionCodes.ConversationPost, PermissionCodes.ConversationClose,
            PermissionCodes.RequirementCreate, PermissionCodes.RequirementRead,
            PermissionCodes.RequirementUpdate,
            PermissionCodes.GenerationExecute,
            PermissionCodes.GlossaryCreate, PermissionCodes.GlossaryRead, PermissionCodes.GlossaryUpdate,
            PermissionCodes.ProjectRead,
            PermissionCodes.TaskCreate, PermissionCodes.TaskRead, PermissionCodes.TaskUpdate,
            PermissionCodes.KnowledgeRead, PermissionCodes.KnowledgeCreate,
            PermissionCodes.KnowledgeUpdate
        ],
        RoleType.Reviewer =>
        [
            PermissionCodes.SpecRead, PermissionCodes.SpecApprove,
            PermissionCodes.ConversationCreate, PermissionCodes.ConversationRead,
            PermissionCodes.ConversationPost, PermissionCodes.ConversationClose,
            PermissionCodes.RequirementRead, PermissionCodes.GlossaryRead,
            PermissionCodes.ProjectRead, PermissionCodes.TaskRead,
            PermissionCodes.KnowledgeRead, PermissionCodes.KnowledgePublish
        ],
        RoleType.QATester =>
        [
            PermissionCodes.SpecRead,
            PermissionCodes.ConversationCreate, PermissionCodes.ConversationRead,
            PermissionCodes.ConversationPost,
            PermissionCodes.RequirementRead, PermissionCodes.GlossaryRead,
            PermissionCodes.ProjectRead,
            PermissionCodes.TaskCreate, PermissionCodes.TaskRead, PermissionCodes.TaskUpdate,
            PermissionCodes.KnowledgeRead
        ],
        _ => []
    };
}
