namespace WriterWorkbench.Core.Commands;

public static class AppCommandIds
{
    public const string ProjectNew = "project.new";
    public const string ProjectOpen = "project.open";
    public const string ProjectSave = "project.save";
    public const string ExportCurrentScene = "export.currentScene";
    public const string ExportFullManuscript = "export.fullManuscript";
    public const string SnapshotCreateCurrent = "snapshot.createCurrent";
    public const string SnapshotRestoreSelected = "snapshot.restoreSelected";
    public const string SnapshotDeleteSelected = "snapshot.deleteSelected";
    public const string StoryRelationshipMapOpen = "story.relationshipMap.open";
    public const string StoryAddNode = "story.structure.addNode";
    public const string StoryUpdateNode = "story.structure.updateNode";
    public const string StoryDeleteNode = "story.structure.deleteNode";
    public const string StoryAddRelationship = "story.relationship.add";
    public const string StoryUpdateRelationship = "story.relationship.update";
    public const string StoryDeleteRelationship = "story.relationship.delete";
    public const string SceneEntityLinkAdd = "sceneEntity.link.add";
    public const string SceneEntityLinkDelete = "sceneEntity.link.delete";
    public const string DocumentCreateScene = "document.createScene";
    public const string DocumentCreateStressLarge = "document.createStressLarge";
    public const string DocumentDetachCurrent = "document.detachCurrent";
    public const string DocumentRenameScene = "document.renameScene";
    public const string DocumentDuplicateScene = "document.duplicateScene";
    public const string DocumentDeleteScene = "document.deleteScene";
    public const string DocumentMoveSceneUp = "document.moveSceneUp";
    public const string DocumentMoveSceneDown = "document.moveSceneDown";
    public const string WritingFocusToggle = "writing.focus.toggle";
    public const string WorkspacePresetOne = "workspace.preset.1";
    public const string WorkspacePresetTwo = "workspace.preset.2";
    public const string WorkspacePresetThree = "workspace.preset.3";
    public const string WorkspaceStartupPresetCycle = "workspace.startupPreset.cycle";
    public const string RemoteControlShow = "remote.show";
    public const string RemoteControlToggle = "remote.toggle";
    public const string RemoteControlOpenSettings = "remote.openSettings";
    public const string ShortcutsOpenSettings = "shortcuts.openSettings";
    public const string ViewHtmlWorkbenchOpen = "view.htmlWorkbench.open";
    public const string ViewMainOpen = "view.main.open";
    public const string ViewPreviewToggle = "view.preview.toggle";
    public const string SearchRun = "search.run";
    public const string AutosaveToggle = "autosave.toggle";
    public const string HelpOpen = "help.open";
}
