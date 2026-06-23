using System.IO;

namespace WriterWorkbench.Core.Storage;

public sealed record ProjectPaths(string RootPath)
{
    public string DocumentsPath => Path.Combine(RootPath, "documents");
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
}
