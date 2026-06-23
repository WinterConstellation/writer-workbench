namespace WriterWorkbench.Core.Documents;

public sealed record DocumentMetrics(
    int ParagraphCount,
    int CharacterCount,
    long PlainTextUtf8Bytes
);
