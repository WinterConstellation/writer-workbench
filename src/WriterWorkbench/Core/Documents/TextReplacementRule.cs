namespace WriterWorkbench.Core.Documents;

public sealed record TextReplacementRule(
    string Id,
    string Source,
    string Replacement,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
