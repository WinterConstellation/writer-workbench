namespace WriterWorkbench.Core.Documents;

public static class StarterDocumentRepairService
{
    public static WriterDocument RepairIfEmpty(WriterDocument document)
    {
        if (document.Paragraphs.Count > 0)
        {
            return document;
        }

        return document with
        {
            Paragraphs =
            [
                new WriterParagraph(
                    "p-0001",
                    "여기에 원고를 씁니다.",
                    "body",
                    [],
                    [])
            ]
        };
    }
}
