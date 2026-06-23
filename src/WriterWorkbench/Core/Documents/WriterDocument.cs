namespace WriterWorkbench.Core.Documents;

public sealed record WriterDocument(
    string Id,
    string Title,
    IReadOnlyList<WriterParagraph> Paragraphs
);
