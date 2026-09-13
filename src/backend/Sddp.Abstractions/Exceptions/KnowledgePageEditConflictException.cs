namespace Sddp.Abstractions.Exceptions;

public sealed class KnowledgePageEditConflictException : SddpException
{
    public KnowledgePageEditConflictException(int expectedRevision, int currentRevision)
        : base(
            "KNOWLEDGE_PAGE_EDIT_CONFLICT",
            $"The page changed after you opened it. Expected revision {expectedRevision}, current revision {currentRevision}.")
    {
        ExpectedRevision = expectedRevision;
        CurrentRevision = currentRevision;
    }

    public int ExpectedRevision { get; }

    public int CurrentRevision { get; }
}
