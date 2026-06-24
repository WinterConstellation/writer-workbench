using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Core.Story;

public sealed class StoryStructureStore(ProjectPaths paths)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<StoryStructureDocument> LoadOrCreateAsync(CancellationToken token)
    {
        Directory.CreateDirectory(paths.StoryPath);
        if (!File.Exists(paths.StoryStructurePath))
        {
            var created = StoryStructureDocument.Empty(DateTimeOffset.UtcNow);
            await SaveAsync(created, token);
            return created;
        }

        await using var stream = File.OpenRead(paths.StoryStructurePath);
        var loaded = await JsonSerializer.DeserializeAsync<StoryStructureDocument>(stream, JsonOptions, token);
        return Normalize(loaded ?? StoryStructureDocument.Empty(DateTimeOffset.UtcNow));
    }

    public async Task SaveAsync(StoryStructureDocument document, CancellationToken token)
    {
        Directory.CreateDirectory(paths.StoryPath);
        var normalized = Normalize(document);
        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        await WriteUtf8AtomicAsync(paths.StoryStructurePath, json, token);
    }

    public async Task<StoryStructureNode> AddNodeAsync(
        string name,
        string kind,
        string summary,
        IEnumerable<string> tags,
        IEnumerable<string> linkedSceneIds,
        CancellationToken token)
    {
        var document = await LoadOrCreateAsync(token);
        var now = DateTimeOffset.UtcNow;
        var node = new StoryStructureNode(
            NextId("node", document.Nodes.Select(node => node.Id)),
            NormalizeText(name, "새 구조"),
            NormalizeText(kind, "PlotPoint"),
            summary.Trim(),
            NormalizeTextList(tags),
            NormalizeTextList(linkedSceneIds),
            document.Nodes.Select(node => node.Order).DefaultIfEmpty(0).Max() + 10,
            now,
            now);

        await SaveAsync(document with
        {
            Nodes = document.Nodes.Append(node).ToList(),
            UpdatedAt = now
        }, token);
        return node;
    }

    public async Task<RelationshipLink> AddRelationshipAsync(
        string sourceNodeId,
        string targetNodeId,
        string kind,
        string summary,
        int strength,
        IEnumerable<string> tags,
        CancellationToken token)
    {
        var document = await LoadOrCreateAsync(token);
        var nodeIds = document.Nodes.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var source = sourceNodeId.Trim();
        var target = targetNodeId.Trim();
        if (!nodeIds.Contains(source) || !nodeIds.Contains(target))
        {
            throw new InvalidOperationException("Relationship endpoints must exist in the story structure.");
        }

        var now = DateTimeOffset.UtcNow;
        var relationship = new RelationshipLink(
            NextId("rel", document.Relationships.Select(relationship => relationship.Id)),
            source,
            target,
            NormalizeText(kind, "related"),
            summary.Trim(),
            Math.Clamp(strength, 0, 5),
            NormalizeTextList(tags),
            now,
            now);

        await SaveAsync(document with
        {
            Relationships = document.Relationships.Append(relationship).ToList(),
            UpdatedAt = now
        }, token);
        return relationship;
    }

    private static StoryStructureDocument Normalize(StoryStructureDocument document)
    {
        var now = DateTimeOffset.UtcNow;
        var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nodes = new List<StoryStructureNode>();
        foreach (var node in document.Nodes ?? [])
        {
            var id = NormalizeText(node.Id, NextId("node", nodeIds));
            if (!nodeIds.Add(id))
            {
                continue;
            }

            var createdAt = node.CreatedAt == default ? now : node.CreatedAt;
            nodes.Add(node with
            {
                Id = id,
                Name = NormalizeText(node.Name, id),
                Kind = NormalizeText(node.Kind, "PlotPoint"),
                Summary = node.Summary?.Trim() ?? "",
                Tags = NormalizeTextList(node.Tags ?? []),
                LinkedSceneIds = NormalizeTextList(node.LinkedSceneIds ?? []),
                CreatedAt = createdAt,
                UpdatedAt = node.UpdatedAt == default ? createdAt : node.UpdatedAt
            });
        }

        nodes = nodes
            .OrderBy(node => node.Order)
            .ThenBy(node => node.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var normalizedNodeIds = nodes.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var relationshipIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var relationships = new List<RelationshipLink>();
        foreach (var relationship in document.Relationships ?? [])
        {
            var source = relationship.SourceNodeId?.Trim() ?? "";
            var target = relationship.TargetNodeId?.Trim() ?? "";
            if (!normalizedNodeIds.Contains(source) || !normalizedNodeIds.Contains(target))
            {
                continue;
            }

            var id = NormalizeText(relationship.Id, NextId("rel", relationshipIds));
            if (!relationshipIds.Add(id))
            {
                continue;
            }

            var createdAt = relationship.CreatedAt == default ? now : relationship.CreatedAt;
            relationships.Add(relationship with
            {
                Id = id,
                SourceNodeId = source,
                TargetNodeId = target,
                Kind = NormalizeText(relationship.Kind, "related"),
                Summary = relationship.Summary?.Trim() ?? "",
                Strength = Math.Clamp(relationship.Strength, 0, 5),
                Tags = NormalizeTextList(relationship.Tags ?? []),
                CreatedAt = createdAt,
                UpdatedAt = relationship.UpdatedAt == default ? createdAt : relationship.UpdatedAt
            });
        }

        return document with
        {
            SchemaVersion = Math.Max(document.SchemaVersion, StoryStructureDocument.CurrentSchemaVersion),
            Nodes = nodes,
            Relationships = relationships,
            UpdatedAt = document.UpdatedAt == default ? now : document.UpdatedAt
        };
    }

    private static string NextId(string prefix, IEnumerable<string> existingIds)
    {
        var max = existingIds
            .Select(id => id.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase) ? id[(prefix.Length + 1)..] : id)
            .Select(suffix => int.TryParse(suffix, out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();
        return $"{prefix}-{max + 1:0000}";
    }

    private static string NormalizeText(string value, string fallback)
    {
        var trimmed = value?.Trim() ?? "";
        return trimmed.Length == 0 ? fallback : trimmed;
    }

    private static IReadOnlyList<string> NormalizeTextList(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task WriteUtf8AtomicAsync(string targetPath, string content, CancellationToken token)
    {
        var tempPath = targetPath + ".tmp";
        await using (var stream = new FileStream(
                         tempPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 16 * 1024,
                         useAsync: true))
        {
            var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(content);
            await stream.WriteAsync(bytes, token);
            await stream.FlushAsync(token);
            stream.Flush(flushToDisk: true);
        }

        File.Move(tempPath, targetPath, overwrite: true);
    }
}
