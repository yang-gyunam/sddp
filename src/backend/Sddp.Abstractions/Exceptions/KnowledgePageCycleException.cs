namespace Sddp.Abstractions.Exceptions;

public sealed class KnowledgePageCycleException : SddpException
{
    public KnowledgePageCycleException()
        : base(
            "KNOWLEDGE_PAGE_CYCLE",
            "A page cannot be moved below itself or its descendants.")
    {
    }
}
