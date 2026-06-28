using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Core.Documents;

public sealed class TextReplacementStore(ProjectPaths paths)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<IReadOnlyList<TextReplacementRule>> LoadAsync(CancellationToken token)
    {
        if (!File.Exists(paths.TextReplacementsPath))
        {
            return [];
        }

        await using var stream = File.OpenRead(paths.TextReplacementsPath);
        var rules = await JsonSerializer.DeserializeAsync<List<TextReplacementRule>>(stream, JsonOptions, token) ?? [];
        return NormalizeRules(rules);
    }

    public async Task SaveAsync(IEnumerable<TextReplacementRule> rules, CancellationToken token)
    {
        Directory.CreateDirectory(paths.SettingsPath);
        await WriteUtf8AtomicAsync(paths.TextReplacementsPath, NormalizeRules(rules), token);
    }

    public async Task<TextReplacementRule> AddOrUpdateAsync(string source, string replacement, CancellationToken token)
    {
        source = source.Trim();
        if (source.Length == 0)
        {
            throw new InvalidOperationException("Replacement source must not be empty.");
        }

        var rules = (await LoadAsync(token)).ToList();
        var now = DateTimeOffset.UtcNow;
        var index = rules.FindIndex(rule => string.Equals(rule.Source, source, StringComparison.Ordinal));
        var rule = new TextReplacementRule(
            index >= 0 ? rules[index].Id : NextId(rules.Select(item => item.Id)),
            source,
            replacement,
            true,
            index >= 0 ? rules[index].CreatedAt : now,
            now);
        if (index >= 0)
        {
            rules[index] = rule;
        }
        else
        {
            rules.Add(rule);
        }

        await SaveAsync(rules, token);
        return rule;
    }

    public async Task DeleteAsync(string ruleId, CancellationToken token)
    {
        var rules = (await LoadAsync(token)).ToList();
        var index = rules.FindIndex(rule => string.Equals(rule.Id, ruleId.Trim(), StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            throw new KeyNotFoundException($"Text replacement rule not found: {ruleId}");
        }

        rules.RemoveAt(index);
        await SaveAsync(rules, token);
    }

    public static TextReplacementResult Apply(string text, IEnumerable<TextReplacementRule> rules)
    {
        var updated = text ?? "";
        var totalCount = 0;
        foreach (var rule in NormalizeRules(rules).Where(rule => rule.IsEnabled))
        {
            var count = CountOccurrences(updated, rule.Source);
            if (count == 0)
            {
                continue;
            }

            updated = updated.Replace(rule.Source, rule.Replacement, StringComparison.Ordinal);
            totalCount += count;
        }

        return new TextReplacementResult(updated, totalCount);
    }

    private static IReadOnlyList<TextReplacementRule> NormalizeRules(IEnumerable<TextReplacementRule> rules)
    {
        var seenSource = new HashSet<string>(StringComparer.Ordinal);
        var normalized = new List<TextReplacementRule>();
        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Source))
            {
                continue;
            }

            var source = rule.Source.Trim();
            if (!seenSource.Add(source))
            {
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            var createdAt = rule.CreatedAt == default ? now : rule.CreatedAt;
            normalized.Add(rule with
            {
                Id = string.IsNullOrWhiteSpace(rule.Id) ? NextId(normalized.Select(item => item.Id)) : rule.Id.Trim(),
                Source = source,
                Replacement = rule.Replacement ?? "",
                CreatedAt = createdAt,
                UpdatedAt = rule.UpdatedAt == default ? createdAt : rule.UpdatedAt
            });
        }

        return normalized;
    }

    private static int CountOccurrences(string text, string source)
    {
        if (source.Length == 0)
        {
            return 0;
        }

        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(source, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += source.Length;
        }

        return count;
    }

    private static string NextId(IEnumerable<string> existingIds)
    {
        var used = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; index < 100_000; index++)
        {
            var id = $"replace-{index:0000}";
            if (!used.Contains(id))
            {
                return id;
            }
        }

        return $"replace-{Guid.NewGuid():N}";
    }

    private static async Task WriteUtf8AtomicAsync(string targetPath, IReadOnlyList<TextReplacementRule> rules, CancellationToken token)
    {
        var tempPath = targetPath + ".tmp";
        var json = JsonSerializer.Serialize(rules, JsonOptions);
        await using (var stream = new FileStream(
                         tempPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 16 * 1024,
                         useAsync: true))
        {
            var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(json);
            await stream.WriteAsync(bytes, token);
            await stream.FlushAsync(token);
            stream.Flush(flushToDisk: true);
        }

        File.Move(tempPath, targetPath, overwrite: true);
    }
}

public sealed record TextReplacementResult(string Text, int ReplacementCount);
