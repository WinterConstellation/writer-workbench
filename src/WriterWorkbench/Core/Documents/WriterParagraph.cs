namespace WriterWorkbench.Core.Documents;

public sealed record WriterParagraph(
    string Id,
    string Text,
    string Style,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Notes
);
