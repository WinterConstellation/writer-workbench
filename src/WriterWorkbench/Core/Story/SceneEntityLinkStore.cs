using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Core.Story;

public sealed class SceneEntityLinkStore(ProjectPaths paths)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<SceneEntityLink>> LoadAsync(CancellationToken token)
    {
        if (!File.Exists(paths.SceneEntityLinksPath))
        {
            return [];
        }

        var entities = await new StoryStructureStore(paths).LoadEntitiesAsync(token);
        var links = await ReadJsonAsync<List<SceneEntityLink>>(paths.SceneEntityLinksPath, token) ?? [];
        return NormalizeLinks(links, entities);
    }

    public async Task<IReadOnlyList<SceneEntityLink>> LoadForSceneAsync(string sceneId, CancellationToken token)
    {
        var normalizedSceneId = NormalizeText(sceneId, "");
        if (normalizedSceneId.Length == 0)
        {
            return [];
        }

        return (await LoadAsync(token))
            .Where(link => string.Equals(link.SceneId, normalizedSceneId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task SaveAsync(IEnumerable<SceneEntityLink> links, CancellationToken token)
    {
        Directory.CreateDirectory(paths.StoryPath);
        var entities = await new StoryStructureStore(paths).LoadEntitiesAsync(token);
        await WriteUtf8AtomicAsync(paths.SceneEntityLinksPath, NormalizeLinks(links, entities), token);
    }

    public async Task<SceneEntityLink> AddOrUpdateAsync(
        string sceneId,
        string entityId,
        string role,
        string notes,
        CancellationToken token)
    {
        var entities = await new StoryStructureStore(paths).LoadEntitiesAsync(token);
        ValidateEntity(entities, entityId);
        var normalizedSceneId = NormalizeText(sceneId, "");
        var normalizedEntityId = NormalizeText(entityId, "");
        if (normalizedSceneId.Length == 0)
        {
            throw new InvalidOperationException("Scene id is required.");
        }

        var links = (await LoadAsync(token)).ToList();
        var index = links.FindIndex(link =>
            string.Equals(link.SceneId, normalizedSceneId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(link.EntityId, normalizedEntityId, StringComparison.OrdinalIgnoreCase));
        var now = DateTimeOffset.UtcNow;
        var link = NormalizeLink(
            new SceneEntityLink(
                normalizedSceneId,
                normalizedEntityId,
                role,
                notes,
                index >= 0 ? links[index].CreatedAt : now,
                now),
            entities);

        if (index >= 0)
        {
            links[index] = link;
        }
        else
        {
            links.Add(link);
        }

        await SaveAsync(links, token);
        return link;
    }

    public async Task DeleteAsync(string sceneId, string entityId, CancellationToken token)
    {
        var normalizedSceneId = NormalizeText(sceneId, "");
        var normalizedEntityId = NormalizeText(entityId, "");
        var links = (await LoadAsync(token))
            .Where(link =>
                !string.Equals(link.SceneId, normalizedSceneId, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(link.EntityId, normalizedEntityId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        await SaveAsync(links, token);
    }

    public async Task DeleteForEntityAsync(string entityId, CancellationToken token)
    {
        var normalizedEntityId = NormalizeText(entityId, "");
        var links = (await LoadAsync(token))
            .Where(link => !string.Equals(link.EntityId, normalizedEntityId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        await SaveAsync(links, token);
    }

    private static IReadOnlyList<SceneEntityLink> NormalizeLinks(
        IEnumerable<SceneEntityLink> links,
        IEnumerable<StoryEntity> entities)
    {
        var entityIds = entities.Select(entity => entity.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<SceneEntityLink>();
        foreach (var link in links)
        {
            var item = NormalizeLinkOrNull(link, entityIds);
            if (item is not null && seen.Add($"{item.SceneId}\u001f{item.EntityId}"))
            {
                normalized.Add(item);
            }
        }

        return normalized;
    }

    private static SceneEntityLink NormalizeLink(SceneEntityLink link, IEnumerable<StoryEntity> entities)
    {
        var entityIds = entities.Select(entity => entity.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return NormalizeLinkOrNull(link, entityIds)
            ?? throw new InvalidOperationException("Scene entity link target must exist in the story structure.");
    }

    private static SceneEntityLink? NormalizeLinkOrNull(SceneEntityLink link, IReadOnlySet<string> entityIds)
    {
        var sceneId = NormalizeText(link.SceneId, "");
        var entityId = NormalizeText(link.EntityId, "");
        if (sceneId.Length == 0 || !entityIds.Contains(entityId))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var createdAt = link.CreatedAt == default ? now : link.CreatedAt;
        return link with
        {
            SceneId = sceneId,
            EntityId = entityId,
            Role = NormalizeText(link.Role, "appears"),
            Notes = link.Notes?.Trim() ?? "",
            CreatedAt = createdAt,
            UpdatedAt = link.UpdatedAt == default ? createdAt : link.UpdatedAt
        };
    }

    private static void ValidateEntity(IEnumerable<StoryEntity> entities, string entityId)
    {
        var normalizedEntityId = NormalizeText(entityId, "");
        if (!entities.Any(entity => string.Equals(entity.Id, normalizedEntityId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Scene entity link target must exist in the story structure.");
        }
    }

    private static string NormalizeText(string value, string fallback)
    {
        var trimmed = value?.Trim() ?? "";
        return trimmed.Length == 0 ? fallback : trimmed;
    }

    private static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken token)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, token);
    }

    private static async Task WriteUtf8AtomicAsync<T>(string targetPath, T value, CancellationToken token)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var tempPath = targetPath + ".tmp";
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
