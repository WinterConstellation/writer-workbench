namespace WriterWorkbench.Core.Story;

public sealed record StoryStructureDocument(
    int SchemaVersion,
    IReadOnlyList<StoryStructureNode> Nodes,
    IReadOnlyList<RelationshipLink> Relationships,
    DateTimeOffset UpdatedAt)
{
    public const int CurrentSchemaVersion = 1;

    public static StoryStructureDocument Empty(DateTimeOffset updatedAt)
    {
        return new StoryStructureDocument(CurrentSchemaVersion, [], [], updatedAt);
    }
}

public sealed record StoryStructureNode(
    string Id,
    string Name,
    string Kind,
    string Summary,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> LinkedSceneIds,
    int Order,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public string Display => $"{Id} | {Name} | {Kind}";
}

public sealed record RelationshipLink(
    string Id,
    string SourceNodeId,
    string TargetNodeId,
    string Kind,
    string Summary,
    int Strength,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public string Display => $"{Id} | {SourceNodeId} -> {TargetNodeId} | {Kind}";
}
