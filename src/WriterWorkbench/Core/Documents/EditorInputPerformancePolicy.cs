namespace WriterWorkbench.Core.Documents;

public static class EditorInputPerformancePolicy
{
    public const int ImmediatePreviewCharacterLimit = 5_000;
    public const int LivePreviewCharacterLimit = 40_000;

    public static PreviewUpdateMode GetPreviewUpdateMode(int characterCount)
    {
        if (characterCount <= ImmediatePreviewCharacterLimit)
        {
            return PreviewUpdateMode.Immediate;
        }

        return characterCount <= LivePreviewCharacterLimit
            ? PreviewUpdateMode.Debounced
            : PreviewUpdateMode.Paused;
    }

    public static string CreatePausedPreviewMessage(int characterCount)
    {
        return $"입력 중 미리보기 갱신 중지 ({characterCount:N0}자). 저장하거나 다시 불러오면 미리보기가 갱신됩니다.";
    }
}
