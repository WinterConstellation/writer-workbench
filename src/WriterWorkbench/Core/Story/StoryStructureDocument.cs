namespace WriterWorkbench.Core.Story;

public sealed record StoryEntity(
    string Id,
    StoryEntityType Type,
    string Name,
    string Role,
    string Summary,
    string Color,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public string Display => $"{Name} | {Type} | {Role}";
}

public enum StoryEntityType
{
    Character,
    Faction,
    Place,
    Event,
    Item,
    Concept
}

public enum StorySettingsBookCategory
{
    Character,
    World,
    Place,
    Plot,
    Reference,
    Other
}

public sealed record StoryRelationship(
    string Id,
    string SourceEntityId,
    string TargetEntityId,
    string Label,
    string Notes,
    bool IsDirectional,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public string Display => $"{SourceEntityId} -> {TargetEntityId} | {Label}";
}

public sealed record StoryMapNodeLayout(
    string EntityId,
    double X,
    double Y);

public sealed record SceneEntityLink(
    string SceneId,
    string EntityId,
    string Role,
    string Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record StorySettingsBookItem(
    string Id,
    StorySettingsBookCategory Category,
    string Title,
    string Body,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
