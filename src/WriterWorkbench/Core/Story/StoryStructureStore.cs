using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using WriterWorkbench.Core.Storage;

namespace WriterWorkbench.Core.Story;

public sealed class StoryStructureStore(ProjectPaths paths)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<StoryEntity>> LoadEntitiesAsync(CancellationToken token)
    {
        return File.Exists(paths.StoryEntitiesPath)
            ? NormalizeEntities(await ReadJsonAsync<List<StoryEntity>>(paths.StoryEntitiesPath, token) ?? [])
            : [];
    }

    public async Task SaveEntitiesAsync(IEnumerable<StoryEntity> entities, CancellationToken token)
    {
        Directory.CreateDirectory(paths.StoryPath);
        await WriteUtf8AtomicAsync(paths.StoryEntitiesPath, NormalizeEntities(entities), token);
    }

    public async Task<IReadOnlyList<StoryRelationship>> LoadRelationshipsAsync(CancellationToken token)
    {
        if (!File.Exists(paths.StoryRelationshipsPath))
        {
            return [];
        }

        var entities = await LoadEntitiesAsync(token);
        var relationships = await ReadJsonAsync<List<StoryRelationship>>(paths.StoryRelationshipsPath, token) ?? [];
        return NormalizeRelationships(relationships, entities);
    }

    public async Task SaveRelationshipsAsync(IEnumerable<StoryRelationship> relationships, CancellationToken token)
    {
        Directory.CreateDirectory(paths.StoryPath);
        var entities = await LoadEntitiesAsync(token);
        await WriteUtf8AtomicAsync(paths.StoryRelationshipsPath, NormalizeRelationships(relationships, entities), token);
    }

    public async Task<IReadOnlyList<StoryMapNodeLayout>> LoadRelationLayoutAsync(CancellationToken token)
    {
        if (!File.Exists(paths.StoryRelationLayoutPath))
        {
            return [];
        }

        var entities = await LoadEntitiesAsync(token);
        var layout = await ReadJsonAsync<List<StoryMapNodeLayout>>(paths.StoryRelationLayoutPath, token) ?? [];
        return NormalizeLayout(layout, entities);
    }

    public async Task SaveRelationLayoutAsync(IEnumerable<StoryMapNodeLayout> layout, CancellationToken token)
    {
        Directory.CreateDirectory(paths.StoryPath);
        var entities = await LoadEntitiesAsync(token);
        await WriteUtf8AtomicAsync(paths.StoryRelationLayoutPath, NormalizeLayout(layout, entities), token);
    }

    public async Task<StoryEntity> AddEntityAsync(
        StoryEntityType type,
        string name,
        string role,
        string summary,
        string color,
        IEnumerable<string> tags,
        CancellationToken token)
    {
        var entities = (await LoadEntitiesAsync(token)).ToList();
        var now = DateTimeOffset.UtcNow;
        var entity = NormalizeEntity(new StoryEntity(
            NextId("entity", entities.Select(entity => entity.Id)),
            type,
            name,
            role,
            summary,
            color,
            tags.ToList(),
            now,
            now));
        entities.Add(entity);
        await SaveEntitiesAsync(entities, token);
        return entity;
    }

    public async Task<StoryEntity> UpdateEntityAsync(StoryEntity entity, CancellationToken token)
    {
        var entities = (await LoadEntitiesAsync(token)).ToList();
        var index = FindEntityIndex(entities, entity.Id);
        if (index < 0)
        {
            throw new KeyNotFoundException($"Story entity not found: {entity.Id}");
        }

        var existing = entities[index];
        var updated = NormalizeEntity(entity with
        {
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        entities[index] = updated;
        await SaveEntitiesAsync(entities, token);
        return updated;
    }

    public async Task DeleteEntityAsync(string entityId, CancellationToken token)
    {
        var entities = (await LoadEntitiesAsync(token)).ToList();
        var index = FindEntityIndex(entities, entityId);
        if (index < 0)
        {
            throw new KeyNotFoundException($"Story entity not found: {entityId}");
        }

        entities.RemoveAt(index);
        await SaveEntitiesAsync(entities, token);

        var relationships = (await LoadRelationshipsAsync(token))
            .Where(relationship =>
                !string.Equals(relationship.SourceEntityId, entityId, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(relationship.TargetEntityId, entityId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        await SaveRelationshipsAsync(relationships, token);

        var layout = (await LoadRelationLayoutAsync(token))
            .Where(node => !string.Equals(node.EntityId, entityId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        await SaveRelationLayoutAsync(layout, token);

        await new SceneEntityLinkStore(paths).DeleteForEntityAsync(entityId, token);
    }

    public async Task<StoryRelationship> AddRelationshipAsync(
        string sourceEntityId,
        string targetEntityId,
        string label,
        string notes,
        bool isDirectional,
        CancellationToken token)
    {
        var entities = await LoadEntitiesAsync(token);
        ValidateRelationshipEndpoints(entities, sourceEntityId, targetEntityId);
        var relationships = (await LoadRelationshipsAsync(token)).ToList();
        var now = DateTimeOffset.UtcNow;
        var relationship = NormalizeRelationship(
            new StoryRelationship(
                NextId("rel", relationships.Select(relationship => relationship.Id)),
                sourceEntityId,
                targetEntityId,
                label,
                notes,
                isDirectional,
                now,
                now),
            entities);
        relationships.Add(relationship);
        await SaveRelationshipsAsync(relationships, token);
        return relationship;
    }

    public async Task<StoryRelationship> UpdateRelationshipAsync(StoryRelationship relationship, CancellationToken token)
    {
        var entities = await LoadEntitiesAsync(token);
        ValidateRelationshipEndpoints(entities, relationship.SourceEntityId, relationship.TargetEntityId);
        var relationships = (await LoadRelationshipsAsync(token)).ToList();
        var index = FindRelationshipIndex(relationships, relationship.Id);
        if (index < 0)
        {
            throw new KeyNotFoundException($"Story relationship not found: {relationship.Id}");
        }

        var existing = relationships[index];
        var updated = NormalizeRelationship(relationship with
        {
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        }, entities);
        relationships[index] = updated;
        await SaveRelationshipsAsync(relationships, token);
        return updated;
    }

    public async Task DeleteRelationshipAsync(string relationshipId, CancellationToken token)
    {
        var relationships = (await LoadRelationshipsAsync(token)).ToList();
        var index = FindRelationshipIndex(relationships, relationshipId);
        if (index < 0)
        {
            throw new KeyNotFoundException($"Story relationship not found: {relationshipId}");
        }

        relationships.RemoveAt(index);
        await SaveRelationshipsAsync(relationships, token);
    }

    public async Task SaveNodeLayoutAsync(string entityId, double x, double y, CancellationToken token)
    {
        var entities = await LoadEntitiesAsync(token);
        if (!entities.Any(entity => string.Equals(entity.Id, entityId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new KeyNotFoundException($"Story entity not found: {entityId}");
        }

        var layout = (await LoadRelationLayoutAsync(token)).ToList();
        var index = layout.FindIndex(node => string.Equals(node.EntityId, entityId, StringComparison.OrdinalIgnoreCase));
        var node = new StoryMapNodeLayout(entityId.Trim(), Math.Max(0, x), Math.Max(0, y));
        if (index >= 0)
        {
            layout[index] = node;
        }
        else
        {
            layout.Add(node);
        }

        await SaveRelationLayoutAsync(layout, token);
    }

    public async Task<IReadOnlyList<StorySettingsBookItem>> LoadSettingsBookAsync(CancellationToken token)
    {
        return File.Exists(paths.StorySettingsBookPath)
            ? NormalizeSettingsBookItems(await ReadJsonAsync<List<StorySettingsBookItem>>(paths.StorySettingsBookPath, token) ?? [])
            : [];
    }

    public async Task SaveSettingsBookAsync(IEnumerable<StorySettingsBookItem> items, CancellationToken token)
    {
        Directory.CreateDirectory(paths.StoryPath);
        await WriteUtf8AtomicAsync(paths.StorySettingsBookPath, NormalizeSettingsBookItems(items), token);
    }

    public async Task<StorySettingsBookItem> AddSettingsBookItemAsync(
        StorySettingsBookCategory category,
        string title,
        string body,
        IEnumerable<string> tags,
        CancellationToken token)
    {
        var items = (await LoadSettingsBookAsync(token)).ToList();
        var now = DateTimeOffset.UtcNow;
        var item = NormalizeSettingsBookItem(new StorySettingsBookItem(
            NextId("note", items.Select(item => item.Id)),
            category,
            title,
            body,
            tags.ToList(),
            now,
            now));
        items.Add(item);
        await SaveSettingsBookAsync(items, token);
        return item;
    }

    public async Task<StorySettingsBookItem> UpdateSettingsBookItemAsync(StorySettingsBookItem item, CancellationToken token)
    {
        var items = (await LoadSettingsBookAsync(token)).ToList();
        var index = FindSettingsBookItemIndex(items, item.Id);
        if (index < 0)
        {
            throw new KeyNotFoundException($"Story settings book item not found: {item.Id}");
        }

        var existing = items[index];
        var updated = NormalizeSettingsBookItem(item with
        {
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        items[index] = updated;
        await SaveSettingsBookAsync(items, token);
        return updated;
    }

    public async Task DeleteSettingsBookItemAsync(string itemId, CancellationToken token)
    {
        var items = (await LoadSettingsBookAsync(token)).ToList();
        var index = FindSettingsBookItemIndex(items, itemId);
        if (index < 0)
        {
            throw new KeyNotFoundException($"Story settings book item not found: {itemId}");
        }

        items.RemoveAt(index);
        await SaveSettingsBookAsync(items, token);
    }

    private static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken token)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, token);
    }

    private static IReadOnlyList<StoryEntity> NormalizeEntities(IEnumerable<StoryEntity> entities)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<StoryEntity>();
        foreach (var entity in entities)
        {
            var item = NormalizeEntity(entity);
            if (seen.Add(item.Id))
            {
                normalized.Add(item);
            }
        }

        return normalized;
    }

    private static StoryEntity NormalizeEntity(StoryEntity entity)
    {
        var now = DateTimeOffset.UtcNow;
        var createdAt = entity.CreatedAt == default ? now : entity.CreatedAt;
        return entity with
        {
            Id = NormalizeText(entity.Id, "entity-0001"),
            Name = NormalizeText(entity.Name, "새 캐릭터"),
            Role = entity.Role?.Trim() ?? "",
            Summary = entity.Summary?.Trim() ?? "",
            Color = NormalizeColor(entity.Color),
            Tags = NormalizeTextList(entity.Tags ?? []),
            CreatedAt = createdAt,
            UpdatedAt = entity.UpdatedAt == default ? createdAt : entity.UpdatedAt
        };
    }

    private static IReadOnlyList<StoryRelationship> NormalizeRelationships(
        IEnumerable<StoryRelationship> relationships,
        IEnumerable<StoryEntity> entities)
    {
        var entityIds = entities.Select(entity => entity.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<StoryRelationship>();
        foreach (var relationship in relationships)
        {
            var item = NormalizeRelationshipOrNull(relationship, entityIds);
            if (item is not null && seen.Add(item.Id))
            {
                normalized.Add(item);
            }
        }

        return normalized;
    }

    private static StoryRelationship NormalizeRelationship(
        StoryRelationship relationship,
        IEnumerable<StoryEntity> entities)
    {
        var entityIds = entities.Select(entity => entity.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return NormalizeRelationshipOrNull(relationship, entityIds)
            ?? throw new InvalidOperationException("Relationship endpoints must exist in the story structure.");
    }

    private static StoryRelationship? NormalizeRelationshipOrNull(
        StoryRelationship relationship,
        IReadOnlySet<string> entityIds)
    {
        var source = relationship.SourceEntityId?.Trim() ?? "";
        var target = relationship.TargetEntityId?.Trim() ?? "";
        if (!entityIds.Contains(source) || !entityIds.Contains(target))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var createdAt = relationship.CreatedAt == default ? now : relationship.CreatedAt;
        return relationship with
        {
            Id = NormalizeText(relationship.Id, "rel-0001"),
            SourceEntityId = source,
            TargetEntityId = target,
            Label = NormalizeText(relationship.Label, "관계"),
            Notes = relationship.Notes?.Trim() ?? "",
            CreatedAt = createdAt,
            UpdatedAt = relationship.UpdatedAt == default ? createdAt : relationship.UpdatedAt
        };
    }

    private static IReadOnlyList<StoryMapNodeLayout> NormalizeLayout(
        IEnumerable<StoryMapNodeLayout> layout,
        IEnumerable<StoryEntity> entities)
    {
        var entityIds = entities.Select(entity => entity.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<StoryMapNodeLayout>();
        foreach (var node in layout)
        {
            var entityId = node.EntityId?.Trim() ?? "";
            if (!entityIds.Contains(entityId) || !seen.Add(entityId))
            {
                continue;
            }

            normalized.Add(new StoryMapNodeLayout(entityId, Math.Max(0, node.X), Math.Max(0, node.Y)));
        }

        return normalized;
    }

    private static IReadOnlyList<StorySettingsBookItem> NormalizeSettingsBookItems(IEnumerable<StorySettingsBookItem> items)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<StorySettingsBookItem>();
        foreach (var item in items)
        {
            var normalizedItem = NormalizeSettingsBookItem(item);
            if (seen.Add(normalizedItem.Id))
            {
                normalized.Add(normalizedItem);
            }
        }

        return normalized;
    }

    private static StorySettingsBookItem NormalizeSettingsBookItem(StorySettingsBookItem item)
    {
        var now = DateTimeOffset.UtcNow;
        var createdAt = item.CreatedAt == default ? now : item.CreatedAt;
        return item with
        {
            Id = NormalizeText(item.Id, "note-0001"),
            Category = Enum.IsDefined(item.Category) ? item.Category : StorySettingsBookCategory.Other,
            Title = NormalizeText(item.Title, "Untitled"),
            Body = item.Body ?? "",
            Tags = NormalizeTextList(item.Tags ?? []),
            CreatedAt = createdAt,
            UpdatedAt = item.UpdatedAt == default ? createdAt : item.UpdatedAt
        };
    }

    private static void ValidateRelationshipEndpoints(
        IEnumerable<StoryEntity> entities,
        string sourceEntityId,
        string targetEntityId)
    {
        var entityIds = entities.Select(entity => entity.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!entityIds.Contains(sourceEntityId.Trim()) || !entityIds.Contains(targetEntityId.Trim()))
        {
            throw new InvalidOperationException("Relationship endpoints must exist in the story structure.");
        }
    }

    private static int FindEntityIndex(IReadOnlyList<StoryEntity> entities, string entityId)
    {
        for (var index = 0; index < entities.Count; index++)
        {
            if (string.Equals(entities[index].Id, entityId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindRelationshipIndex(IReadOnlyList<StoryRelationship> relationships, string relationshipId)
    {
        for (var index = 0; index < relationships.Count; index++)
        {
            if (string.Equals(relationships[index].Id, relationshipId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindSettingsBookItemIndex(IReadOnlyList<StorySettingsBookItem> items, string itemId)
    {
        for (var index = 0; index < items.Count; index++)
        {
            if (string.Equals(items[index].Id, itemId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
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

    private static string NormalizeColor(string value)
    {
        var color = value?.Trim() ?? "";
        return color.StartsWith('#') && color.Length is 7 or 9 ? color : "#2563EB";
    }

    private static IReadOnlyList<string> NormalizeTextList(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
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
