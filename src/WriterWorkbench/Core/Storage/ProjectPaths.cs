using System.IO;

namespace WriterWorkbench.Core.Storage;

public sealed record ProjectPaths(string RootPath)
{
    public string DocumentsPath => Path.Combine(RootPath, "documents");
    public string ExportsPath => Path.Combine(RootPath, "exports");
    public string SnapshotsPath => Path.Combine(RootPath, "snapshots");
    public string StoryPath => Path.Combine(RootPath, "story");
    public string StoryStructurePath => Path.Combine(StoryPath, "story.structure.json");
    public string StoryEntitiesPath => Path.Combine(StoryPath, "entities.json");
    public string StoryRelationshipsPath => Path.Combine(StoryPath, "relationships.json");
    public string StoryRelationLayoutPath => Path.Combine(StoryPath, "relation-layout.json");
    public string SceneEntityLinksPath => Path.Combine(StoryPath, "scene-entity-links.json");
    public string ProjectDatabasePath => Path.Combine(RootPath, "project.sqlite");
    public string ManifestPath => Path.Combine(RootPath, "project.manifest.json");
    public string WorkspacePresetsPath => Path.Combine(RootPath, "workspace.presets.json");
    public string ShortcutsPath => Path.Combine(RootPath, "shortcuts.json");

    public static ProjectPaths ForRoot(string rootPath)
    {
        return new ProjectPaths(rootPath);
    }

    public string DocumentJsonPath(string documentId)
    {
        return Path.Combine(DocumentsPath, $"{documentId}.wwdoc.json");
    }

    public string DocumentTextPath(string documentId)
    {
        return Path.Combine(DocumentsPath, $"{documentId}.txt");
    }

    public string SceneMetadataPath(string documentId)
    {
        return Path.Combine(DocumentsPath, $"{documentId}.meta.json");
    }

    public string SceneSnapshotsPath(string documentId)
    {
        return Path.Combine(SnapshotsPath, documentId);
    }

    public string SceneSnapshotPath(string documentId, string snapshotId)
    {
        return Path.Combine(SceneSnapshotsPath(documentId), snapshotId);
    }

    public string SceneSnapshotInfoPath(string documentId, string snapshotId)
    {
        return Path.Combine(SceneSnapshotPath(documentId, snapshotId), "snapshot.info.json");
    }

    public string SceneSnapshotDocumentJsonPath(string documentId, string snapshotId)
    {
        return Path.Combine(SceneSnapshotPath(documentId, snapshotId), "document.wwdoc.json");
    }

    public string SceneSnapshotDocumentTextPath(string documentId, string snapshotId)
    {
        return Path.Combine(SceneSnapshotPath(documentId, snapshotId), "document.txt");
    }

    public string SceneSnapshotMetadataPath(string documentId, string snapshotId)
    {
        return Path.Combine(SceneSnapshotPath(documentId, snapshotId), "scene.meta.json");
    }
}
