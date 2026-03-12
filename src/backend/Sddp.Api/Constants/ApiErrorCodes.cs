namespace Sddp.Api.Constants;

/// <summary>
/// API error code constants used consistently across controllers.
/// </summary>
public static class ApiErrorCodes
{
    /// <summary>Header-related errors.</summary>
    public static class Header
    {
        public const string MissingHeader = "MISSING_HEADER";
        public const string MissingHeaders = "MISSING_HEADERS";
        public const string InvalidTenant = "INVALID_TENANT";
        public const string InvalidProject = "INVALID_PROJECT";
    }

    /// <summary>Authentication and authorization errors.</summary>
    public static class Auth
    {
        public const string InvalidUser = "INVALID_USER";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string NotAllowed = "NOT_ALLOWED";
    }

    /// <summary>Common CRUD errors.</summary>
    public static class Crud
    {
        public const string NotFound = "NOT_FOUND";
        public const string CreateFailed = "CREATE_FAILED";
        public const string UpdateFailed = "UPDATE_FAILED";
        public const string UpdateError = "UPDATE_ERROR";
        public const string DeleteFailed = "DELETE_FAILED";
        public const string DeleteError = "DELETE_ERROR";
        public const string SaveFailed = "SAVE_FAILED";
        public const string SetFailed = "SET_FAILED";
        public const string ResetFailed = "RESET_FAILED";
        public const string PostFailed = "POST_FAILED";
    }

    /// <summary>Validation errors.</summary>
    public static class Validation
    {
        public const string Error = "VALIDATION_ERROR";
        public const string InvalidId = "INVALID_ID";
        public const string InvalidQuery = "INVALID_QUERY";
        public const string InvalidRequest = "INVALID_REQUEST";
        public const string InvalidGroup = "INVALID_GROUP";
        public const string InvalidPath = "INVALID_PATH";
        public const string InvalidScope = "INVALID_SCOPE";
        public const string InvalidInput = "INVALID_INPUT";
        public const string InvalidDate = "INVALID_DATE";
        public const string MissingPath = "MISSING_PATH";
        public const string TitleRequired = "TITLE_REQUIRED";
        public const string DecisionRequired = "DECISION_REQUIRED";
    }

    /// <summary>Entity ID validation errors.</summary>
    public static class InvalidEntity
    {
        public const string Project = "INVALID_PROJECT";
        public const string TopicId = "INVALID_TOPIC_ID";
        public const string ConversationId = "INVALID_CONVERSATION_ID";
        public const string SpecId = "INVALID_SPEC_ID";
        public const string RequirementId = "INVALID_REQUIREMENT_ID";
        public const string ReplyTo = "INVALID_REPLY_TO";
        public const string ReplacementId = "INVALID_REPLACEMENT_ID";
        public const string ExcludeId = "INVALID_EXCLUDE_ID";
        public const string RelatedId = "INVALID_RELATED_ID";
        public const string TargetUser = "INVALID_TARGET_USER";
        public const string Target = "INVALID_TARGET";
        public const string Actor = "INVALID_ACTOR";
        public const string ResourceType = "INVALID_RESOURCE_TYPE";
        public const string EntityType = "INVALID_ENTITY_TYPE";
        public const string FromEntityType = "INVALID_FROM_ENTITY_TYPE";
        public const string ToEntityType = "INVALID_TO_ENTITY_TYPE";
        public const string RelationType = "INVALID_RELATION_TYPE";
        public const string FromEntityId = "INVALID_FROM_ENTITY_ID";
        public const string ToEntityId = "INVALID_TO_ENTITY_ID";
        public const string Term = "INVALID_TERM";
        public const string Example = "INVALID_EXAMPLE";
        public const string StartDate = "INVALID_START_DATE";
        public const string EndDate = "INVALID_END_DATE";
    }

    /// <summary>Spec domain errors.</summary>
    public static class Spec
    {
        public const string TransitionError = "TRANSITION_ERROR";
        public const string SubmitError = "SUBMIT_ERROR";
        public const string ApproveError = "APPROVE_ERROR";
        public const string RejectError = "REJECT_ERROR";
        public const string LockError = "LOCK_ERROR";
        public const string LockFailed = "LOCK_FAILED";
        public const string SignOffError = "SIGN_OFF_ERROR";
        public const string LinkError = "LINK_ERROR";
        public const string VersionError = "VERSION_ERROR";
        public const string DiffError = "DIFF_ERROR";
    }

    /// <summary>Glossary domain errors.</summary>
    public static class Glossary
    {
        public const string ApprovalError = "APPROVAL_ERROR";
        public const string DeprecationError = "DEPRECATION_ERROR";
        public const string AddRelatedError = "ADD_RELATED_ERROR";
        public const string AddExampleError = "ADD_EXAMPLE_ERROR";
    }

    /// <summary>Conversation domain errors.</summary>
    public static class Conversation
    {
        public const string CloseFailed = "CLOSE_FAILED";
        public const string ReopenFailed = "REOPEN_FAILED";
        public const string ArchiveFailed = "ARCHIVE_FAILED";
        public const string PinFailed = "PIN_FAILED";
    }

    /// <summary>Task domain errors.</summary>
    public static class Task
    {
        public const string PositionError = "POSITION_ERROR";
    }

    /// <summary>Artifact domain errors.</summary>
    public static class Artifact
    {
        public const string RegenerateError = "REGENERATE_ERROR";
    }

    /// <summary>Project domain errors.</summary>
    public static class Project
    {
        public const string MemberLimitExceeded = "PROJECT_MEMBER_LIMIT_EXCEEDED";
        public const string ActiveProjectLimitExceeded = "ACTIVE_PROJECT_LIMIT_EXCEEDED";
        public const string AddMemberFailed = "ADD_MEMBER_FAILED";
    }

    /// <summary>System-level errors.</summary>
    public static class System
    {
        public const string OperationError = "OPERATION_ERROR";
        public const string RequestCancelled = "REQUEST_CANCELLED";
        public const string InternalError = "INTERNAL_ERROR";
    }
}
