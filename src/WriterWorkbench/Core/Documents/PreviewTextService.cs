namespace WriterWorkbench.Core.Documents;

public static class PreviewTextService
{
    public const int DefaultMaxCharacters = 12_000;

    public static string CreatePreview(string plainText, int maxCharacters = DefaultMaxCharacters)
    {
        if (maxCharacters < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCharacters), "Preview limit must be positive.");
        }

        if (plainText.Length <= maxCharacters)
        {
            return plainText;
        }

        return plainText[..maxCharacters] +
               $"{Environment.NewLine}{Environment.NewLine}[미리보기는 {maxCharacters:N0}자에서 잘렸습니다. 편집 데이터는 원문을 유지합니다.]";
    }
}
