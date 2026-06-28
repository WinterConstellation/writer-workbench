const state = {
  payload: null,
  remoteIconOnly: false,
  railMode: "binder",
  pendingEditorUpdate: null,
  editorUpdateTimer: 0,
  metricUpdateTimer: 0,
  isRendering: false,
  remoteDrag: null,
  storyDrag: null,
  activeView: "editor",
  selectedDocumentId: "",
  binderContextDocumentId: "",
  remoteDraftCommandIds: [],
  remoteSettingsDirty: false,
  availableCommands: [],
  shortcutBindings: [],
  storyModel: { entities: [], relationships: [] },
  settingsBook: [],
  editingEntityId: "",
  editingRelationshipId: "",
  editingSettingsBookId: "",
  activeSceneMetrics: null,
};

const $ = (id) => document.getElementById(id);

const menuLabels = new Map([
  ["top.project", "작품"],
  ["top.manuscript", "원고"],
  ["top.story", "구조"],
  ["top.view", "보기"],
  ["top.tools", "도구"],
  ["top.help", "도움말"],
]);

const badgeColors = ["#285fbd", "#16806b", "#a56416", "#6e5bb8", "#b83f5f", "#44546a"];

function sendCommand(commandId) {
  if (!commandId) return;

  flushActiveSceneUpdate();
  postWebMessage({ type: "command", commandId });
}

function postWebMessage(message) {
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.postMessage(message);
  }
}

function formatNumber(value) {
  return new Intl.NumberFormat("ko-KR").format(value || 0);
}

function readPayloadValue(source, camel, pascal, fallback) {
  return source?.[camel] ?? source?.[pascal] ?? fallback;
}

function render(payload) {
  state.isRendering = true;
  state.payload = payload;
  normalizeCharacterCountLabels();
  const project = readPayloadValue(payload, "project", "Project", {});
  const active = readPayloadValue(payload, "activeScene", "ActiveScene", null);
  const binder = readPayloadValue(payload, "binder", "Binder", []);
  const toolbarCommands = readPayloadValue(payload, "commands", "Commands", []);
  const menuCommands = readPayloadValue(payload, "menuCommands", "MenuCommands", []);
  const remoteCommands = readPayloadValue(payload, "remoteCommands", "RemoteCommands", []);
  const availableCommands = readPayloadValue(payload, "availableCommands", "AvailableCommands", []);
  const shortcutBindings = readPayloadValue(payload, "shortcutBindings", "ShortcutBindings", []);
  const statusText = readPayloadValue(payload, "statusText", "StatusText", "");
  const graphicPresetName = readPayloadValue(payload, "graphicPresetName", "GraphicPresetName", "기본");
  const autosaveEnabled = readPayloadValue(payload, "autosaveEnabled", "AutosaveEnabled", true);
  const activeView = readPayloadValue(payload, "activeView", "ActiveView", "editor");
  const previewText = readPayloadValue(payload, "previewText", "PreviewText", "");
  const story = readPayloadValue(payload, "story", "Story", null);
  const trash = readPayloadValue(payload, "trash", "Trash", []) || [];
  const settingsBook = readPayloadValue(payload, "settingsBook", "SettingsBook", []) || [];
  state.availableCommands = availableCommands.map(normalizeCommand);
  state.shortcutBindings = shortcutBindings.map(normalizeShortcut);
  syncRemoteDraftFromPayload(remoteCommands, activeView);
  state.selectedDocumentId = readPayloadValue(active, "id", "Id", state.selectedDocumentId || "");

  $("project-title").textContent = readPayloadValue(project, "title", "Title", "원고 작업대");
  $("project-path").textContent = readPayloadValue(project, "rootPath", "RootPath", "");
  $("scene-count-pill").textContent = `${formatNumber(readPayloadValue(project, "sceneCount", "SceneCount", 0))} 장면`;
  $("theme-pill").textContent = graphicPresetName;
  $("autosave-pill").textContent = autosaveEnabled ? "자동저장 켬" : "자동저장 끔";
  $("status-text").textContent = statusText;

  renderTopMenu(menuCommands.length ? menuCommands : toolbarCommands);
  renderBinder(binder);
  renderActiveScene(active);
  renderPreview(previewText);
  renderInspector(active);
  renderPipeline(binder);
  renderSettingsPanel(menuCommands, settingsBook);
  renderReferencePanel(project, active, trash);
  renderRelationshipMap(story);
  renderRemoteSettings(remoteCommands, state.availableCommands);
  renderShortcutSettings(state.shortcutBindings);
  renderRemote(remoteCommands.length ? remoteCommands : toolbarCommands.slice(0, 6));
  setActiveView(activeView);
  state.isRendering = false;
}

function syncRemoteDraftFromPayload(remoteCommands, activeView) {
  const incomingCommandIds = remoteCommands.map(normalizeCommand).map((command) => command.commandId);
  if (activeView === "remote-settings" && state.remoteSettingsDirty) {
    if (sameCommandIds(state.remoteDraftCommandIds, incomingCommandIds)) {
      state.remoteSettingsDirty = false;
    }

    return;
  }

  state.remoteDraftCommandIds = incomingCommandIds;
  state.remoteSettingsDirty = false;
}

function sameCommandIds(left, right) {
  if (left.length !== right.length) return false;

  return left.every((commandId, index) =>
    commandId.toLowerCase() === right[index].toLowerCase());
}

function normalizeCommand(command) {
  return {
    commandId: readPayloadValue(command, "commandId", "CommandId", ""),
    label: readPayloadValue(command, "label", "Label", ""),
    category: readPayloadValue(command, "category", "Category", ""),
    surface: readPayloadValue(command, "surface", "Surface", ""),
    area: readPayloadValue(command, "area", "Area", "top.project"),
    slotKey: readPayloadValue(command, "slotKey", "SlotKey", ""),
    order: readPayloadValue(command, "order", "Order", 0),
  };
}

function normalizeShortcut(shortcut) {
  return {
    commandId: readPayloadValue(shortcut, "commandId", "CommandId", ""),
    commandName: readPayloadValue(shortcut, "commandName", "CommandName", ""),
    category: readPayloadValue(shortcut, "category", "Category", ""),
    scope: readPayloadValue(shortcut, "scope", "Scope", ""),
    gesture: readPayloadValue(shortcut, "gesture", "Gesture", ""),
  };
}

function renderTopMenu(commands) {
  const topMenu = $("top-menu");
  topMenu.textContent = "";
  const groups = new Map();
  commands.map(normalizeCommand)
    .sort((a, b) => a.order - b.order)
    .forEach((command) => {
      const area = command.area || "top.project";
      if (!groups.has(area)) groups.set(area, []);
      groups.get(area).push(command);
    });

  for (const [area, groupCommands] of groups) {
    const group = document.createElement("section");
    group.className = "menu-group";
    const title = document.createElement("span");
    title.className = "menu-title";
    title.textContent = menuLabels.get(area) || area.replace("top.", "");
    group.appendChild(title);

    for (const command of groupCommands) {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "menu-command";
      button.dataset.command = command.commandId;
      button.textContent = command.label || command.commandId;
      group.appendChild(button);
    }

    topMenu.appendChild(group);
  }
}

function renderBinder(items) {
  $("binder-count").textContent = formatNumber(items.length);
  const list = $("scene-list");
  list.textContent = "";
  for (const item of items) {
    const id = readPayloadValue(item, "id", "Id", "");
    const title = readPayloadValue(item, "title", "Title", id);
    const status = readPayloadValue(item, "status", "Status", "초고");
    const sceneType = readPayloadValue(item, "sceneType", "SceneType", "Scene");
    const length = readPayloadValue(item, "contentLength", "ContentLength", 0);
    const isActive = readPayloadValue(item, "isActive", "IsActive", false);
    const row = document.createElement("article");
    row.className = `scene-item${isActive ? " active" : ""}`;
    row.dataset.documentId = id;
    row.tabIndex = 0;
    row.innerHTML = `
      <div class="scene-line">
        <span class="scene-title"></span>
        <span class="state-pill"></span>
      </div>
      <div class="scene-meta">
        <span></span>
        <span></span>
      </div>
      <div class="scene-actions"></div>`;
    row.querySelector(".scene-title").textContent = title;
    row.querySelector(".state-pill").textContent = status;
    const meta = row.querySelectorAll(".scene-meta span");
    meta[0].textContent = id;
    meta[1].textContent = `${sceneType} · 공백 제외 ${formatNumber(length)}`;
    const actions = row.querySelector(".scene-actions");
    actions.append(
      createBinderActionButton("document.select", id, "열기"),
      createBinderActionButton("document.renameScene", id, "이름"),
      createBinderActionButton("document.duplicateScene", id, "복제"),
      createBinderActionButton("document.deleteScene", id, "삭제"));
    row.addEventListener("click", (event) => {
      if (event.target.closest("[data-binder-command]")) {
        return;
      }

      selectBinderDocument(id);
    });
    row.addEventListener("dblclick", () => selectBinderDocument(id));
    list.appendChild(row);
  }
}

function createBinderActionButton(commandId, documentId, label) {
  const button = document.createElement("button");
  button.type = "button";
  button.dataset.binderCommand = commandId;
  button.dataset.binderDocument = documentId;
  button.textContent = label;
  return button;
}

function selectBinderDocument(documentId) {
  if (!documentId) return;

  state.selectedDocumentId = documentId;
  hideBinderContextMenu();
  flushActiveSceneUpdate();
  postWebMessage({ type: "document.select", documentId });
}

function sendBinderCommand(commandId, documentId = state.selectedDocumentId) {
  if (!commandId) return;

  hideBinderContextMenu();
  flushActiveSceneUpdate();
  postWebMessage({
    type: "document.command",
    documentId: documentId || state.selectedDocumentId || "",
    commandId,
  });
}

function showBinderContextMenu(event, documentId) {
  event.preventDefault();
  if (!documentId) return;

  state.binderContextDocumentId = documentId;
  state.selectedDocumentId = documentId;
  const menu = $("binder-context-menu");
  if (!menu) return;

  menu.dataset.documentId = documentId;
  menu.hidden = false;
  positionBinderContextMenu(menu, event.clientX, event.clientY);
}

function positionBinderContextMenu(menu, clientX, clientY) {
  const padding = 8;
  const maxLeft = Math.max(padding, window.innerWidth - menu.offsetWidth - padding);
  const maxTop = Math.max(padding, window.innerHeight - menu.offsetHeight - padding);
  const left = Math.min(Math.max(padding, clientX), maxLeft);
  const top = Math.min(Math.max(padding, clientY), maxTop);
  menu.style.left = `${left}px`;
  menu.style.top = `${top}px`;
}

function hideBinderContextMenu() {
  const menu = $("binder-context-menu");
  if (!menu) return;

  menu.hidden = true;
  delete menu.dataset.documentId;
}

function renderActiveScene(active) {
  if (!active) {
    $("active-title").textContent = "장면 없음";
    $("active-title-editor").value = "";
    $("active-body-editor").value = "";
    $("active-body-editor").disabled = true;
    $("active-status").textContent = "-";
    $("active-length").textContent = "0";
    $("active-length-spaces").textContent = "0";
    state.activeSceneMetrics = null;
    $("active-type").textContent = "Scene";
    $("active-editor-metrics").textContent = "";
    $("active-summary").textContent = "";
    $("active-tags").textContent = "";
    $("status-active-scene").textContent = "-";
    return;
  }

  const id = readPayloadValue(active, "id", "Id", "");
  const title = readPayloadValue(active, "title", "Title", id);
  const status = readPayloadValue(active, "status", "Status", "초고");
  const sceneType = readPayloadValue(active, "sceneType", "SceneType", "Scene");
  const summary = readPayloadValue(active, "summary", "Summary", "");
  const tags = readPayloadValue(active, "tags", "Tags", []);
  const editorText = readPayloadValue(active, "editorText", "EditorText", "");
  const visibleMetrics = measureEditorText(editorText);
  state.activeSceneMetrics = visibleMetrics;

  $("active-title").textContent = title;
  if (document.activeElement !== $("active-title-editor")) {
    $("active-title-editor").value = title;
  }
  if (document.activeElement !== $("active-body-editor")) {
    $("active-body-editor").value = editorText;
  }
  $("active-body-editor").disabled = false;
  $("active-status").textContent = status;
  $("active-length").textContent = formatNumber(visibleMetrics.contentLength);
  $("active-length-spaces").textContent = formatNumber(visibleMetrics.contentLengthWithSpaces);
  $("active-type").textContent = sceneType;
  $("active-editor-metrics").textContent = formatEditorMetrics(visibleMetrics);
  $("active-summary").textContent = summary || " ";
  $("status-active-scene").textContent = `${id} · ${title}`;

  const tagRow = $("active-tags");
  tagRow.textContent = "";
  for (const tag of tags) {
    const span = document.createElement("span");
    span.className = "tag";
    span.textContent = tag;
    tagRow.appendChild(span);
  }
}

function normalizeCharacterCountLabels() {
  const withoutSpaces = $("active-length")?.previousElementSibling;
  const withSpaces = $("active-length-spaces")?.previousElementSibling;
  if (withoutSpaces) {
    withoutSpaces.textContent = "전체 공백 제외";
  }

  if (withSpaces) {
    withSpaces.textContent = "전체 공백 포함";
  }
}

function renderSettingsPanel(menuCommands, settingsBook) {
  state.settingsBook = (settingsBook || [])
    .map(normalizeSettingsBookItem)
    .filter((item) => item.id || item.title || item.body);
  if (state.editingSettingsBookId &&
      !state.settingsBook.some((item) => item.id === state.editingSettingsBookId)) {
    state.editingSettingsBookId = "";
  }

  $("settings-count").textContent = formatNumber(state.settingsBook.length);
  renderSettingsBookList(menuCommands);
  syncSettingsBookForm();
}

function normalizeSettingsBookItem(item) {
  return {
    id: readPayloadValue(item, "id", "Id", ""),
    category: readPayloadValue(item, "category", "Category", "Other") || "Other",
    title: readPayloadValue(item, "title", "Title", ""),
    body: readPayloadValue(item, "body", "Body", ""),
    tags: readPayloadValue(item, "tags", "Tags", []) || [],
    updatedAt: readPayloadValue(item, "updatedAt", "UpdatedAt", ""),
  };
}

function renderSettingsBookList(menuCommands) {
  const list = $("settings-list");
  list.textContent = "";

  if (state.settingsBook.length === 0) {
    const empty = document.createElement("div");
    empty.className = "reference-item";
    const title = document.createElement("strong");
    title.textContent = "설정집 항목 없음";
    const body = document.createElement("span");
    body.textContent = "인물, 세계관, 자료를 왼쪽 폼에서 추가하세요.";
    empty.append(title, body);
    list.appendChild(empty);
  }

  for (const item of state.settingsBook) {
    const row = document.createElement("article");
    row.className = "story-list-item settings-book-item";
    row.classList.toggle("editing", item.id === state.editingSettingsBookId);
    const title = document.createElement("strong");
    title.textContent = item.title || item.id;
    const meta = document.createElement("span");
    meta.textContent = `${categoryLabel(item.category)} · ${formatDate(item.updatedAt)} · ${item.id}`;
    const excerpt = document.createElement("span");
    excerpt.textContent = item.body ? item.body.slice(0, 96) : "내용 없음";
    const tags = document.createElement("div");
    tags.className = "settings-book-tags";
    for (const tag of item.tags) {
      const chip = document.createElement("span");
      chip.className = "tag";
      chip.textContent = tag;
      tags.appendChild(chip);
    }
    const actions = document.createElement("div");
    actions.className = "story-item-actions";
    actions.append(
      createSettingsBookActionButton("edit", item.id, "수정"),
      createSettingsBookActionButton("delete", item.id, "삭제"));
    row.append(title, meta, excerpt, tags, actions);
    list.appendChild(row);
  }

  const quick = createSettingsQuickCommands(menuCommands);
  if (quick.length > 0) {
    const quickWrap = document.createElement("div");
    quickWrap.className = "settings-book-quick";
    for (const command of quick) {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "side-action";
      button.dataset.command = command.commandId;
      button.textContent = command.label || command.commandId;
      quickWrap.appendChild(button);
    }
    list.appendChild(quickWrap);
  }
}

function createSettingsQuickCommands(menuCommands) {
  const commands = (menuCommands || [])
    .map(normalizeCommand)
    .filter((command) => command.area === "top.story" || command.area === "top.tools");
  const quickCommands = [
    findAvailableCommand("story.relationshipMap.open", "관계도", "구조"),
    findAvailableCommand("shortcuts.openSettings", "단축키", "작업공간"),
    findAvailableCommand("remote.openSettings", "리모컨 편집", "작업공간"),
    findAvailableCommand("help.open", "도움말", "도움말"),
  ];
  const seen = new Set(commands.map((command) => command.commandId.toLowerCase()));
  for (const command of quickCommands) {
    if (command.commandId && !seen.has(command.commandId.toLowerCase())) {
      commands.push(command);
      seen.add(command.commandId.toLowerCase());
    }
  }
  return commands;
}

function createSettingsBookActionButton(action, itemId, label) {
  const button = document.createElement("button");
  button.type = "button";
  button.dataset.settingsBookAction = action;
  button.dataset.settingsBookId = itemId;
  button.textContent = label;
  return button;
}

function categoryLabel(category) {
  const labels = {
    Character: "인물",
    World: "세계관",
    Place: "장소",
    Plot: "플롯",
    Reference: "자료",
    Other: "기타",
  };
  return labels[category] || category || "기타";
}

function syncSettingsBookForm() {
  const form = $("settings-book-form");
  if (form?.contains(document.activeElement)) {
    return;
  }

  const item = state.settingsBook.find((entry) => entry.id === state.editingSettingsBookId);
  if (item) {
    fillSettingsBookForm(item);
    return;
  }

  clearSettingsBookForm();
}

function fillSettingsBookForm(item) {
  state.editingSettingsBookId = item.id || "";
  $("settings-book-category").value = item.category || "Other";
  $("settings-book-title").value = item.title || "";
  $("settings-book-body").value = item.body || "";
  $("settings-book-tags").value = (item.tags || []).join(", ");
  $("settings-book-save").textContent = state.editingSettingsBookId ? "저장" : "추가";
  $("settings-book-cancel").hidden = !state.editingSettingsBookId;
}

function clearSettingsBookForm() {
  state.editingSettingsBookId = "";
  $("settings-book-category").value = "Character";
  $("settings-book-title").value = "";
  $("settings-book-body").value = "";
  $("settings-book-tags").value = "";
  $("settings-book-save").textContent = "추가";
  $("settings-book-cancel").hidden = true;
}

function parseSettingsBookTags(value) {
  const seen = new Set();
  return (value || "")
    .split(",")
    .map((tag) => tag.trim())
    .filter((tag) => tag.length > 0)
    .filter((tag) => {
      const key = tag.toLowerCase();
      if (seen.has(key)) return false;
      seen.add(key);
      return true;
    });
}

function saveSettingsBookItem() {
  const category = $("settings-book-category").value || "Other";
  const title = $("settings-book-title").value.trim();
  const body = $("settings-book-body").value;
  const tags = parseSettingsBookTags($("settings-book-tags").value);
  if (!title) {
    $("status-text").textContent = "설정집 제목을 입력하세요.";
    return;
  }

  if (state.editingSettingsBookId) {
    postWebMessage({
      type: "story.settingsBook.update",
      itemId: state.editingSettingsBookId,
      category,
      title,
      body,
      tags,
    });
    $("status-text").textContent = `${title} 저장 요청`;
  } else {
    postWebMessage({
      type: "story.settingsBook.add",
      category,
      title,
      body,
      tags,
    });
    $("status-text").textContent = `${title} 추가 요청`;
  }

  clearSettingsBookForm();
}

function handleSettingsBookAction(button) {
  const action = button.dataset.settingsBookAction;
  const itemId = button.dataset.settingsBookId;
  if (!action || !itemId) return;

  const item = state.settingsBook.find((entry) => entry.id === itemId);
  if (action === "edit") {
    if (!item) return;
    fillSettingsBookForm(item);
    $("settings-book-title").focus();
    return;
  }

  if (action === "delete") {
    if (!window.confirm(`${item?.title || itemId} 삭제?`)) return;
    postWebMessage({ type: "story.settingsBook.delete", itemId });
  }
}

function findAvailableCommand(commandId, label, category) {
  return state.availableCommands.find((command) => command.commandId === commandId) || {
    commandId,
    label,
    category,
    surface: "catalog",
    area: "catalog",
    slotKey: commandId,
    order: 0,
  };
}

function normalizeTrashItem(item) {
  return {
    trashId: readPayloadValue(item, "trashId", "TrashId", ""),
    documentId: readPayloadValue(item, "documentId", "DocumentId", ""),
    title: readPayloadValue(item, "title", "Title", ""),
    deletedAt: readPayloadValue(item, "deletedAt", "DeletedAt", ""),
  };
}

function renderReferencePanel(project, active, trashItems) {
  const list = $("reference-list");
  list.textContent = "";
  const trash = (trashItems || []).map(normalizeTrashItem);
  const references = [
    ["프로젝트", readPayloadValue(project, "rootPath", "RootPath", "")],
    ["현재 장면", active ? `${readPayloadValue(active, "id", "Id", "")} · ${readPayloadValue(active, "title", "Title", "")}` : "-"],
    ["요약", active ? readPayloadValue(active, "summary", "Summary", "-") || "-" : "-"],
    ["휴지통", `${formatNumber(trash.length)}개`],
  ];
  $("reference-count").textContent = formatNumber(references.length);

  for (const [title, value] of references) {
    const item = document.createElement("div");
    item.className = "reference-item";
    const strong = document.createElement("strong");
    strong.textContent = title;
    const span = document.createElement("span");
    span.textContent = value;
    item.append(strong, span);
    list.appendChild(item);
  }

  const trashList = $("trash-list");
  trashList.textContent = "";
  if (trash.length === 0) {
    const empty = document.createElement("div");
    empty.className = "reference-item";
    empty.innerHTML = "<strong>휴지통</strong><span>삭제 대기 장면 없음</span>";
    trashList.appendChild(empty);
    return;
  }

  for (const item of trash) {
    const row = document.createElement("article");
    row.className = "reference-item trash-item";
    const title = document.createElement("strong");
    title.textContent = item.title || item.documentId || item.trashId;
    const meta = document.createElement("span");
    meta.textContent = `${item.documentId} · ${formatDate(item.deletedAt)}`;
    const restore = document.createElement("button");
    restore.type = "button";
    restore.dataset.trashRestore = item.trashId;
    restore.textContent = "복원";
    row.append(title, meta, restore);
    trashList.appendChild(row);
  }
}

function renderPreview(text) {
  $("preview-reader").textContent = text || "";
}

function normalizeStory(story) {
  const entities = readPayloadValue(story, "entities", "Entities", []) || [];
  const relationships = readPayloadValue(story, "relationships", "Relationships", []) || [];
  return {
    entities: entities.map((entity) => ({
      id: readPayloadValue(entity, "id", "Id", ""),
      type: readPayloadValue(entity, "type", "Type", "Character"),
      name: readPayloadValue(entity, "name", "Name", ""),
      role: readPayloadValue(entity, "role", "Role", ""),
      summary: readPayloadValue(entity, "summary", "Summary", ""),
      color: readPayloadValue(entity, "color", "Color", "#2563EB"),
      tags: readPayloadValue(entity, "tags", "Tags", []) || [],
      x: Number(readPayloadValue(entity, "x", "X", 0)) || 0,
      y: Number(readPayloadValue(entity, "y", "Y", 0)) || 0,
    })),
    relationships: relationships.map((relationship) => ({
      id: readPayloadValue(relationship, "id", "Id", ""),
      sourceEntityId: readPayloadValue(relationship, "sourceEntityId", "SourceEntityId", ""),
      targetEntityId: readPayloadValue(relationship, "targetEntityId", "TargetEntityId", ""),
      label: readPayloadValue(relationship, "label", "Label", "관계"),
      notes: readPayloadValue(relationship, "notes", "Notes", ""),
      isDirectional: Boolean(readPayloadValue(relationship, "isDirectional", "IsDirectional", false)),
    })),
  };
}

function renderRelationshipMap(story) {
  const model = normalizeStory(story);
  state.storyModel = model;
  if (state.editingEntityId && !model.entities.some((entity) => entity.id === state.editingEntityId)) {
    state.editingEntityId = "";
  }
  if (state.editingRelationshipId && !model.relationships.some((relationship) => relationship.id === state.editingRelationshipId)) {
    state.editingRelationshipId = "";
  }
  $("relationship-counts").textContent = `${formatNumber(model.entities.length)}명 / 관계 ${formatNumber(model.relationships.length)}개`;
  renderStoryLists(model);
  renderStorySelectors(model.entities);
  renderStoryCanvas(model);
  syncStoryEditForms(model);
}

function renderStoryLists(model) {
  const entityList = $("story-entity-list");
  const relationshipList = $("story-relationship-list");
  entityList.textContent = "";
  relationshipList.textContent = "";

  for (const entity of model.entities) {
    const item = document.createElement("article");
    item.className = "story-list-item";
    item.classList.toggle("editing", entity.id === state.editingEntityId);
    const title = document.createElement("strong");
    title.textContent = entity.name || entity.id;
    const meta = document.createElement("span");
    meta.textContent = `${entity.role || entity.type} · ${entity.id}`;
    const actions = document.createElement("div");
    actions.className = "story-item-actions";
    actions.append(
      createStoryActionButton("entityEdit", entity.id, "수정"),
      createStoryActionButton("entityDelete", entity.id, "삭제"));
    item.append(title, meta, actions);
    entityList.appendChild(item);
  }

  const entityById = new Map(model.entities.map((entity) => [entity.id, entity]));
  for (const relationship of model.relationships) {
    const source = entityById.get(relationship.sourceEntityId);
    const target = entityById.get(relationship.targetEntityId);
    const item = document.createElement("article");
    item.className = "story-list-item";
    item.classList.toggle("editing", relationship.id === state.editingRelationshipId);
    const title = document.createElement("strong");
    title.textContent = relationship.label || "관계";
    const meta = document.createElement("span");
    meta.textContent = `${source?.name || relationship.sourceEntityId} → ${target?.name || relationship.targetEntityId}`;
    const actions = document.createElement("div");
    actions.className = "story-item-actions";
    actions.append(
      createStoryActionButton("relationshipEdit", relationship.id, "수정"),
      createStoryActionButton("relationshipDelete", relationship.id, "삭제"));
    item.append(title, meta, actions);
    relationshipList.appendChild(item);
  }
}

function createStoryActionButton(action, id, label) {
  const button = document.createElement("button");
  button.type = "button";
  button.dataset.storyAction = action;
  button.dataset.storyId = id;
  button.textContent = label;
  return button;
}

function renderStorySelectors(entities) {
  for (const select of [$("story-relationship-source"), $("story-relationship-target")]) {
    const previous = select.value;
    select.textContent = "";
    for (const entity of entities) {
      const option = document.createElement("option");
      option.value = entity.id;
      option.textContent = entity.name || entity.id;
      select.appendChild(option);
    }
    if (previous && entities.some((entity) => entity.id === previous)) {
      select.value = previous;
    }
  }
}

function renderStoryCanvas(model) {
  const canvas = $("relationship-map-canvas");
  canvas.textContent = "";
  const entityById = new Map(model.entities.map((entity) => [entity.id, entity]));
  const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
  svg.classList.add("relationship-lines");
  canvas.appendChild(svg);

  for (const relationship of model.relationships) {
    const source = entityById.get(relationship.sourceEntityId);
    const target = entityById.get(relationship.targetEntityId);
    if (!source || !target) continue;

    const x1 = source.x + 66;
    const y1 = source.y + 27;
    const x2 = target.x + 66;
    const y2 = target.y + 27;
    const line = document.createElementNS("http://www.w3.org/2000/svg", "line");
    line.setAttribute("x1", x1);
    line.setAttribute("y1", y1);
    line.setAttribute("x2", x2);
    line.setAttribute("y2", y2);
    line.setAttribute("stroke", "#64748B");
    line.setAttribute("stroke-width", relationship.isDirectional ? "2.5" : "1.5");
    svg.appendChild(line);

    const label = document.createElementNS("http://www.w3.org/2000/svg", "text");
    label.setAttribute("x", (x1 + x2) / 2);
    label.setAttribute("y", (y1 + y2) / 2 - 6);
    label.setAttribute("fill", "#111827");
    label.setAttribute("font-size", "12");
    label.setAttribute("text-anchor", "middle");
    label.textContent = relationship.label || "관계";
    svg.appendChild(label);
  }

  if (model.entities.length === 0) {
    const empty = document.createElement("div");
    empty.className = "story-list-item";
    empty.style.position = "absolute";
    empty.style.left = "24px";
    empty.style.top = "24px";
    empty.innerHTML = "<strong>캐릭터 없음</strong><span>왼쪽에서 캐릭터를 추가하세요.</span>";
    canvas.appendChild(empty);
    return;
  }

  for (const entity of model.entities) {
    const node = document.createElement("article");
    node.className = "story-map-node";
    node.dataset.entityId = entity.id;
    node.tabIndex = 0;
    node.style.left = `${entity.x}px`;
    node.style.top = `${entity.y}px`;
    node.style.background = entity.color || "#2563EB";
    const title = document.createElement("strong");
    title.textContent = entity.name || entity.id;
    const meta = document.createElement("span");
    meta.textContent = entity.role || entity.type;
    const actions = document.createElement("div");
    actions.className = "story-map-node-actions";
    actions.append(
      createStoryActionButton("entityEdit", entity.id, "수정"),
      createStoryActionButton("entityDelete", entity.id, "삭제"));
    node.append(title, meta, actions);
    node.addEventListener("pointerdown", startStoryNodeDrag);
    canvas.appendChild(node);
  }
}

function renderRemoteSettings(remoteCommands, availableCommands) {
  const currentList = $("remote-settings-current-list");
  const availableList = $("remote-settings-available-list");
  if (!currentList || !availableList) return;

  const commandById = new Map();
  for (const command of [...(availableCommands || []), ...(remoteCommands || []).map(normalizeCommand)]) {
    if (command.commandId) {
      commandById.set(command.commandId.toLowerCase(), command);
    }
  }

  const selected = new Set(state.remoteDraftCommandIds.map((id) => id.toLowerCase()));
  currentList.textContent = "";
  state.remoteDraftCommandIds.forEach((commandId, index) => {
    const command = commandById.get(commandId.toLowerCase()) || {
      commandId,
      label: commandId,
      category: "",
    };
    currentList.appendChild(createRemoteSettingsRow(command, index));
  });

  availableList.textContent = "";
  const candidates = (availableCommands || [])
    .filter((command) => command.commandId && !selected.has(command.commandId.toLowerCase()))
    .sort((a, b) => (a.category || "").localeCompare(b.category || "", "ko-KR") ||
      (a.label || a.commandId).localeCompare(b.label || b.commandId, "ko-KR"));
  for (const command of candidates) {
    availableList.appendChild(createRemoteSettingsCandidate(command));
  }

  $("remote-settings-current-count").textContent = formatNumber(state.remoteDraftCommandIds.length);
  $("remote-settings-available-count").textContent = formatNumber(candidates.length);
}

function createRemoteSettingsRow(command, index) {
  const row = document.createElement("article");
  row.className = "remote-settings-row";
  const body = document.createElement("div");
  body.className = "remote-settings-body";
  const title = document.createElement("strong");
  title.textContent = command.label || command.commandId;
  const meta = document.createElement("span");
  meta.textContent = `${command.category || "명령"} · ${command.commandId}`;
  body.append(title, meta);

  const actions = document.createElement("div");
  actions.className = "remote-settings-actions";
  actions.append(
    createRemoteSettingsButton("up", command.commandId, "위", index === 0),
    createRemoteSettingsButton("down", command.commandId, "아래", index === state.remoteDraftCommandIds.length - 1),
    createRemoteSettingsButton("remove", command.commandId, "삭제", false));
  row.append(body, actions);
  return row;
}

function createRemoteSettingsCandidate(command) {
  const row = document.createElement("article");
  row.className = "remote-settings-row remote-settings-row-candidate";
  const body = document.createElement("div");
  body.className = "remote-settings-body";
  const title = document.createElement("strong");
  title.textContent = command.label || command.commandId;
  const meta = document.createElement("span");
  meta.textContent = `${command.category || "명령"} · ${command.commandId}`;
  body.append(title, meta);
  row.append(body, createRemoteSettingsButton("add", command.commandId, "추가", false));
  return row;
}

function createRemoteSettingsButton(action, commandId, label, disabled) {
  const button = document.createElement("button");
  button.type = "button";
  button.dataset.remoteAction = action;
  button.dataset.remoteCommand = commandId;
  button.disabled = disabled;
  button.textContent = label;
  return button;
}

function handleRemoteSettingsAction(button) {
  const action = button.dataset.remoteAction;
  const commandId = button.dataset.remoteCommand;
  if (!action || !commandId) return;

  const index = state.remoteDraftCommandIds.findIndex((id) => id.toLowerCase() === commandId.toLowerCase());
  if (action === "add" && index < 0) {
    state.remoteDraftCommandIds.push(commandId);
  } else if (action === "remove" && index >= 0) {
    state.remoteDraftCommandIds.splice(index, 1);
  } else if (action === "up" && index > 0) {
    [state.remoteDraftCommandIds[index - 1], state.remoteDraftCommandIds[index]] =
      [state.remoteDraftCommandIds[index], state.remoteDraftCommandIds[index - 1]];
  } else if (action === "down" && index >= 0 && index < state.remoteDraftCommandIds.length - 1) {
    [state.remoteDraftCommandIds[index], state.remoteDraftCommandIds[index + 1]] =
      [state.remoteDraftCommandIds[index + 1], state.remoteDraftCommandIds[index]];
  }

  state.remoteSettingsDirty = true;
  renderRemoteSettings([], state.availableCommands);
}

function saveRemoteSettings() {
  postWebMessage({
    type: "remoteSettings.update",
    commandIds: state.remoteDraftCommandIds,
  });
  $("status-text").textContent = `리모컨 저장 요청 · ${formatNumber(state.remoteDraftCommandIds.length)}개`;
}

function renderShortcutSettings(shortcuts) {
  const list = $("shortcut-shell-list");
  if (!list) return;

  list.textContent = "";
  for (const shortcut of shortcuts || []) {
    const row = document.createElement("article");
    row.className = "shortcut-row";
    row.dataset.searchText = [
      shortcut.commandName,
      shortcut.commandId,
      shortcut.category,
      shortcut.scope,
      shortcut.gesture
    ].join(" ").toLowerCase();
    const left = document.createElement("div");
    left.className = "shortcut-command";
    const title = document.createElement("strong");
    title.textContent = shortcut.commandName || shortcut.commandId;
    const meta = document.createElement("span");
    meta.textContent = `${shortcut.category || "명령"} · ${shortcut.commandId}`;
    left.append(title, meta);
    const right = document.createElement("div");
    right.className = "shortcut-keys";
    const gesture = document.createElement("input");
    gesture.type = "text";
    gesture.className = "shortcut-editor";
    gesture.value = shortcut.gesture || "";
    gesture.dataset.shortcutCommand = shortcut.commandId;
    gesture.dataset.shortcutScope = shortcut.scope || "Workbench";
    gesture.setAttribute("aria-label", `${shortcut.commandName || shortcut.commandId} 단축키`);
    const scope = document.createElement("span");
    scope.textContent = shortcut.scope || "Workbench";
    const save = document.createElement("button");
    save.type = "button";
    save.dataset.shortcutSave = shortcut.commandId;
    save.dataset.shortcutScope = shortcut.scope || "Workbench";
    save.textContent = "저장";
    right.append(gesture, scope, save);
    row.append(left, right);
    list.appendChild(row);
  }

  filterShortcutSettings();
}

function filterShortcutSettings() {
  const input = $("shortcut-search");
  const query = (input?.value || "").trim().toLowerCase();
  document.querySelectorAll(".shortcut-row").forEach((row) => {
    row.hidden = query.length > 0 && !row.dataset.searchText.includes(query);
  });
}

function captureShortcutGesture(event) {
  const input = event.target.closest(".shortcut-editor");
  if (!input) return;

  const key = event.key;
  if (["Control", "Shift", "Alt", "Meta"].includes(key)) {
    event.preventDefault();
    return;
  }

  const parts = [];
  if (event.ctrlKey) parts.push("Ctrl");
  if (event.altKey) parts.push("Alt");
  if (event.shiftKey) parts.push("Shift");
  const normalizedKey = normalizeShortcutKey(key);
  if (!normalizedKey) return;
  parts.push(normalizedKey);
  input.value = parts.join("+");
  event.preventDefault();
}

function normalizeShortcutKey(key) {
  if (!key) return "";
  if (key.length === 1) return key.toUpperCase();
  const aliases = new Map([
    ["Escape", "Esc"],
    [" ", "Space"],
    ["ArrowUp", "Up"],
    ["ArrowDown", "Down"],
    ["ArrowLeft", "Left"],
    ["ArrowRight", "Right"],
  ]);
  return aliases.get(key) || key;
}

function saveShortcutBinding(button) {
  const commandId = button.dataset.shortcutSave;
  const scope = button.dataset.shortcutScope || "Workbench";
  const input = document.querySelector(`.shortcut-editor[data-shortcut-command="${cssEscape(commandId)}"][data-shortcut-scope="${cssEscape(scope)}"]`);
  const gesture = input?.value?.trim() || "";
  if (!commandId || !gesture) {
    $("status-text").textContent = "단축키를 입력하세요.";
    return;
  }

  postWebMessage({
    type: "shortcut.update",
    commandId,
    scope,
    gesture,
  });
  $("status-text").textContent = `${gesture} 저장 요청`;
}

function cssEscape(value) {
  if (window.CSS && typeof window.CSS.escape === "function") {
    return window.CSS.escape(value || "");
  }

  return String(value || "").replace(/["\\]/g, "\\$&");
}

function renderInspector(active) {
  if (!active) {
    $("inspector-status").textContent = "-";
    $("inspector-type").textContent = "-";
    $("inspector-tags").textContent = "-";
    $("active-updated").textContent = "-";
    return;
  }

  const tags = readPayloadValue(active, "tags", "Tags", []);
  const updatedAt = readPayloadValue(active, "updatedAt", "UpdatedAt", "");
  $("inspector-status").textContent = readPayloadValue(active, "status", "Status", "초고");
  $("inspector-type").textContent = readPayloadValue(active, "sceneType", "SceneType", "Scene");
  $("inspector-tags").textContent = tags.length ? tags.join(", ") : "-";
  $("active-updated").textContent = formatDate(updatedAt);
}

function renderPipeline(items) {
  const counts = { draft: 0, revising: 0, final: 0, excluded: 0 };
  for (const item of items) {
    const status = readPayloadValue(item, "status", "Status", "초고");
    if (status === "초고") counts.draft += 1;
    else if (status === "수정중") counts.revising += 1;
    else if (status === "완료") counts.final += 1;
    else if (status === "제외") counts.excluded += 1;
  }

  $("pipeline-draft").textContent = formatNumber(counts.draft);
  $("pipeline-revising").textContent = formatNumber(counts.revising);
  $("pipeline-final").textContent = formatNumber(counts.final);
  $("pipeline-excluded").textContent = formatNumber(counts.excluded);
}

function renderRemote(commands) {
  const list = $("remote-command-list");
  list.textContent = "";
  commands.map(normalizeCommand)
    .sort((a, b) => a.order - b.order)
    .forEach((command, index) => {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "remote-command";
      button.dataset.command = command.commandId;
      const badge = document.createElement("span");
      badge.className = "command-badge";
      badge.style.background = badgeColors[index % badgeColors.length];
      badge.textContent = (command.category || command.label || command.commandId).slice(0, 1).toUpperCase();
      const label = document.createElement("span");
      label.className = "remote-label";
      label.textContent = command.label || command.commandId;
      button.append(badge, label);
      list.appendChild(button);
    });
}

function formatDate(value) {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return new Intl.DateTimeFormat("ko-KR", {
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

document.addEventListener("click", (event) => {
  const contextCommand = event.target.closest("[data-binder-context-command]");
  if (contextCommand) {
    sendBinderCommand(
      contextCommand.dataset.binderContextCommand,
      state.binderContextDocumentId || $("binder-context-menu")?.dataset.documentId || state.selectedDocumentId);
    return;
  }

  const binderCommand = event.target.closest("[data-binder-command]");
  if (binderCommand) {
    sendBinderCommand(
      binderCommand.dataset.binderCommand,
      binderCommand.dataset.binderDocument || state.selectedDocumentId);
    return;
  }

  if (!event.target.closest("#binder-context-menu")) {
    hideBinderContextMenu();
  }

  const railTab = event.target.closest("[data-rail-mode]");
  if (railTab) {
    setRailMode(railTab.dataset.railMode);
    return;
  }

  const densityToggle = event.target.closest("#remote-density-toggle");
  if (densityToggle) {
    state.remoteIconOnly = !state.remoteIconOnly;
    $("floating-remote").classList.toggle("remote-icon-only", state.remoteIconOnly);
    densityToggle.textContent = state.remoteIconOnly ? "제목" : "아이콘";
    return;
  }

  const remoteSave = event.target.closest("#remote-settings-save");
  if (remoteSave) {
    saveRemoteSettings();
    return;
  }

  const remoteAction = event.target.closest("[data-remote-action]");
  if (remoteAction) {
    handleRemoteSettingsAction(remoteAction);
    return;
  }

  const settingsBookSave = event.target.closest("#settings-book-save");
  if (settingsBookSave) {
    saveSettingsBookItem();
    return;
  }

  const settingsBookCancel = event.target.closest("#settings-book-cancel");
  if (settingsBookCancel) {
    clearSettingsBookForm();
    return;
  }

  const settingsBookAction = event.target.closest("[data-settings-book-action]");
  if (settingsBookAction) {
    handleSettingsBookAction(settingsBookAction);
    return;
  }

  const storyAction = event.target.closest("[data-story-action]");
  if (storyAction) {
    handleStoryAction(storyAction);
    return;
  }

  const addEntity = event.target.closest("#story-add-entity");
  if (addEntity) {
    addStoryEntity();
    return;
  }

  const cancelEntityEdit = event.target.closest("#story-cancel-entity-edit");
  if (cancelEntityEdit) {
    clearStoryEntityForm();
    return;
  }

  const addRelationship = event.target.closest("#story-add-relationship");
  if (addRelationship) {
    addStoryRelationship();
    return;
  }

  const cancelRelationshipEdit = event.target.closest("#story-cancel-relationship-edit");
  if (cancelRelationshipEdit) {
    clearStoryRelationshipForm();
    return;
  }

  const trashRestore = event.target.closest("[data-trash-restore]");
  if (trashRestore) {
    if (window.confirm("삭제 대기 장면을 바인더로 복원할까요?")) {
      postWebMessage({ type: "trash.restore", trashId: trashRestore.dataset.trashRestore });
    }
    return;
  }

  const shortcutSave = event.target.closest("[data-shortcut-save]");
  if (shortcutSave) {
    saveShortcutBinding(shortcutSave);
    return;
  }

  const button = event.target.closest("button[data-command]");
  if (button) {
    sendCommand(button.dataset.command);
  }
});

document.addEventListener("contextmenu", (event) => {
  const row = event.target.closest(".scene-item[data-document-id]");
  if (!row) return;

  showBinderContextMenu(event, row.dataset.documentId);
});

$("active-title-editor").addEventListener("input", scheduleActiveSceneUpdate);
$("active-body-editor").addEventListener("input", scheduleActiveSceneUpdate);
$("shortcut-search")?.addEventListener("input", filterShortcutSettings);
document.addEventListener("keydown", captureShortcutGesture);

$("remote-drag-handle").addEventListener("pointerdown", startRemoteDrag);
document.addEventListener("pointermove", moveRemoteDrag);
document.addEventListener("pointerup", endRemoteDrag);
document.addEventListener("pointercancel", endRemoteDrag);
document.addEventListener("pointermove", moveStoryNodeDrag);
document.addEventListener("pointerup", endStoryNodeDrag);
document.addEventListener("pointercancel", endStoryNodeDrag);

function setRailMode(mode) {
  state.railMode = mode || "binder";
  document.querySelectorAll("[data-rail-mode]").forEach((button) => {
    button.classList.toggle("active", button.dataset.railMode === state.railMode);
  });
  document.querySelectorAll(".rail-panel").forEach((panel) => {
    panel.classList.toggle("active", panel.id === `rail-panel-${state.railMode}`);
  });
}

function setActiveView(view) {
  state.activeView = view || "editor";
  document.querySelectorAll("[data-view-panel]").forEach((panel) => {
    panel.classList.toggle("active", panel.dataset.viewPanel === state.activeView);
  });

  const viewNames = {
    editor: "현재 작업",
    preview: "미리보기",
    "relationship-map": "관계도",
    shortcuts: "단축키",
    "remote-settings": "리모컨 편집",
    help: "도움말",
  };
  document.querySelector(".surface-kicker").textContent = viewNames[state.activeView] || "현재 작업";
}

function scheduleActiveSceneUpdate() {
  if (state.isRendering) return;

  window.clearTimeout(state.editorUpdateTimer);
  state.pendingEditorUpdate = {
    type: "activeScene.update",
    title: $("active-title-editor").value,
    editorText: $("active-body-editor").value,
  };
  state.editorUpdateTimer = window.setTimeout(flushActiveSceneUpdate, 450);
  scheduleLocalMetricUpdate();
}

function flushActiveSceneUpdate() {
  if (!state.pendingEditorUpdate) return;

  window.clearTimeout(state.editorUpdateTimer);
  const message = state.pendingEditorUpdate;
  state.pendingEditorUpdate = null;
  postWebMessage(message);
}

function scheduleLocalMetricUpdate() {
  window.clearTimeout(state.metricUpdateTimer);
  state.metricUpdateTimer = window.setTimeout(updateActiveEditorMetrics, 1000);
}

function measureEditorText(editorText) {
  const normalized = (editorText || "").replace(/\r\n/g, "\n");
  return {
    contentLength: normalized.replace(/\s/g, "").length,
    contentLengthWithSpaces: normalized.length,
  };
}

function formatEditorMetrics(metrics) {
  return `현재 장면 · 공백 제외 ${formatNumber(metrics.contentLength)} · 공백 포함 ${formatNumber(metrics.contentLengthWithSpaces)}`;
}

function updateActiveEditorMetrics() {
  const current = measureEditorText($("active-body-editor").value || "");
  $("active-editor-metrics").textContent = formatEditorMetrics(current);
  $("active-length").textContent = formatNumber(current.contentLength);
  $("active-length-spaces").textContent = formatNumber(current.contentLengthWithSpaces);
}

function addStoryEntity() {
  const name = $("story-entity-name").value.trim();
  const role = $("story-entity-role").value.trim();
  if (!name) {
    $("status-text").textContent = "관계도 캐릭터 이름을 입력하세요.";
    return;
  }

  if (state.editingEntityId) {
    postWebMessage({ type: "story.entity.update", entityId: state.editingEntityId, name, role });
  } else {
    postWebMessage({ type: "story.entity.add", name, role });
  }

  clearStoryEntityForm();
}

function addStoryRelationship() {
  const sourceEntityId = $("story-relationship-source").value;
  const targetEntityId = $("story-relationship-target").value;
  const label = $("story-relationship-label").value.trim() || "관계";
  const notes = $("story-relationship-notes").value.trim();
  if (!sourceEntityId || !targetEntityId || sourceEntityId === targetEntityId) {
    $("status-text").textContent = "서로 다른 캐릭터 두 명을 선택하세요.";
    return;
  }

  if (state.editingRelationshipId) {
    postWebMessage({
      type: "story.relationship.update",
      relationshipId: state.editingRelationshipId,
      sourceEntityId,
      targetEntityId,
      label,
      notes,
    });
  } else {
    postWebMessage({
      type: "story.relationship.add",
      sourceEntityId,
      targetEntityId,
      label,
      notes,
    });
  }

  clearStoryRelationshipForm();
}

function syncStoryEditForms(model) {
  const entity = model.entities.find((item) => item.id === state.editingEntityId);
  const entityButton = $("story-add-entity");
  const entityCancel = $("story-cancel-entity-edit");
  if (entity) {
    $("story-entity-name").value = entity.name || "";
    $("story-entity-role").value = entity.role || "";
    entityButton.textContent = "저장";
    entityCancel.hidden = false;
  } else {
    entityButton.textContent = "추가";
    entityCancel.hidden = true;
  }

  const relationship = model.relationships.find((item) => item.id === state.editingRelationshipId);
  const relationshipButton = $("story-add-relationship");
  const relationshipCancel = $("story-cancel-relationship-edit");
  if (relationship) {
    $("story-relationship-source").value = relationship.sourceEntityId;
    $("story-relationship-target").value = relationship.targetEntityId;
    $("story-relationship-label").value = relationship.label || "";
    $("story-relationship-notes").value = relationship.notes || "";
    relationshipButton.textContent = "저장";
    relationshipCancel.hidden = false;
  } else {
    relationshipButton.textContent = "연결";
    relationshipCancel.hidden = true;
  }
}

function clearStoryEntityForm() {
  state.editingEntityId = "";
  $("story-entity-name").value = "";
  $("story-entity-role").value = "";
  $("story-add-entity").textContent = "추가";
  $("story-cancel-entity-edit").hidden = true;
}

function clearStoryRelationshipForm() {
  state.editingRelationshipId = "";
  $("story-relationship-label").value = "";
  $("story-relationship-notes").value = "";
  $("story-add-relationship").textContent = "연결";
  $("story-cancel-relationship-edit").hidden = true;
}

function handleStoryAction(button) {
  const action = button.dataset.storyAction;
  const id = button.dataset.storyId;
  if (!action || !id) return;

  if (action === "entityEdit") {
    const entity = state.storyModel.entities.find((item) => item.id === id);
    if (!entity) return;
    state.editingEntityId = id;
    $("story-entity-name").value = entity.name || "";
    $("story-entity-role").value = entity.role || "";
    syncStoryEditForms(state.storyModel);
    return;
  }

  if (action === "entityDelete") {
    const entity = state.storyModel.entities.find((item) => item.id === id);
    if (!window.confirm(`${entity?.name || id} 삭제? 연결된 관계도 같이 삭제됩니다.`)) return;
    postWebMessage({ type: "story.entity.delete", entityId: id });
    return;
  }

  if (action === "relationshipEdit") {
    const relationship = state.storyModel.relationships.find((item) => item.id === id);
    if (!relationship) return;
    state.editingRelationshipId = id;
    syncStoryEditForms(state.storyModel);
    return;
  }

  if (action === "relationshipDelete") {
    const relationship = state.storyModel.relationships.find((item) => item.id === id);
    if (!window.confirm(`${relationship?.label || id} 관계 삭제?`)) return;
    postWebMessage({ type: "story.relationship.delete", relationshipId: id });
  }
}

function startRemoteDrag(event) {
  const remote = $("floating-remote");
  const rect = remote.getBoundingClientRect();
  state.remoteDrag = {
    pointerId: event.pointerId,
    offsetX: event.clientX - rect.left,
    offsetY: event.clientY - rect.top,
  };
  remote.setPointerCapture?.(event.pointerId);
  event.preventDefault();
}

function moveRemoteDrag(event) {
  if (!state.remoteDrag || state.remoteDrag.pointerId !== event.pointerId) return;

  const remote = $("floating-remote");
  remote.style.right = "auto";
  remote.style.left = `${event.clientX - state.remoteDrag.offsetX}px`;
  remote.style.top = `${event.clientY - state.remoteDrag.offsetY}px`;
}

function endRemoteDrag(event) {
  if (!state.remoteDrag || state.remoteDrag.pointerId !== event.pointerId) return;

  $("floating-remote").releasePointerCapture?.(event.pointerId);
  state.remoteDrag = null;
}

function startStoryNodeDrag(event) {
  if (event.target.closest("[data-story-action]")) {
    return;
  }

  const node = event.currentTarget;
  const rect = node.getBoundingClientRect();
  state.storyDrag = {
    pointerId: event.pointerId,
    entityId: node.dataset.entityId,
    offsetX: event.clientX - rect.left,
    offsetY: event.clientY - rect.top,
  };
  node.setPointerCapture?.(event.pointerId);
  event.preventDefault();
}

function moveStoryNodeDrag(event) {
  if (!state.storyDrag || state.storyDrag.pointerId !== event.pointerId) return;

  const node = findStoryNode(state.storyDrag.entityId);
  const canvas = $("relationship-map-canvas");
  if (!node || !canvas) return;

  const rect = canvas.getBoundingClientRect();
  const x = Math.max(0, event.clientX - rect.left + canvas.scrollLeft - state.storyDrag.offsetX);
  const y = Math.max(0, event.clientY - rect.top + canvas.scrollTop - state.storyDrag.offsetY);
  node.style.left = `${x}px`;
  node.style.top = `${y}px`;
}

function endStoryNodeDrag(event) {
  if (!state.storyDrag || state.storyDrag.pointerId !== event.pointerId) return;

  const entityId = state.storyDrag.entityId;
  const node = findStoryNode(entityId);
  node?.releasePointerCapture?.(event.pointerId);
  state.storyDrag = null;
  if (!node) return;

  postWebMessage({
    type: "story.layout.update",
    entityId,
    x: parseFloat(node.style.left) || 0,
    y: parseFloat(node.style.top) || 0,
  });
}

function findStoryNode(entityId) {
  return Array.from(document.querySelectorAll(".story-map-node"))
    .find((node) => node.dataset.entityId === entityId);
}

if (window.chrome && window.chrome.webview) {
  window.chrome.webview.addEventListener("message", (event) => {
    const message = event.data;
    if (message && message.type === "state") {
      render(message.payload);
    }
  });
} else {
  render({
    project: { title: "원고 작업대", rootPath: "local preview", sceneCount: 3 },
    activeScene: {
      id: "scene-0001",
      title: "첫 장면",
      status: "초고",
      summary: "여기에 현재 장면의 요약과 작업 정보가 표시됩니다.",
      tags: ["주인공", "도입"],
      contentLength: 1200,
      contentLengthWithSpaces: 1360,
      sceneType: "Scene",
      updatedAt: new Date().toISOString(),
      editorText: "여기에 원고를 씁니다.\n\n메인에서도 현재 장면 본문을 바로 수정할 수 있습니다."
    },
    binder: [
      { id: "scene-0001", title: "첫 장면", status: "초고", sceneType: "Scene", contentLength: 1200, isActive: true },
      { id: "scene-0002", title: "추격", status: "수정중", sceneType: "Action", contentLength: 900, isActive: false },
      { id: "scene-0003", title: "결말", status: "완료", sceneType: "Scene", contentLength: 1500, isActive: false }
    ],
    menuCommands: [
      { commandId: "project.save", label: "저장", category: "프로젝트", surface: "menu", area: "top.project", slotKey: "save", order: 10 },
      { commandId: "document.createScene", label: "새 장면", category: "문서", surface: "menu", area: "top.manuscript", slotKey: "create", order: 20 },
      { commandId: "story.relationshipMap.open", label: "관계도", category: "구조", surface: "menu", area: "top.story", slotKey: "map", order: 30 },
      { commandId: "view.editor.open", label: "작품 수정", category: "보기", surface: "menu", area: "top.view", slotKey: "editor", order: 40 },
      { commandId: "view.preview.toggle", label: "미리보기", category: "보기", surface: "menu", area: "top.view", slotKey: "preview", order: 50 }
    ],
    remoteCommands: [
      { commandId: "snapshot.createCurrent", label: "현재 장면 스냅샷", category: "스냅샷", surface: "remote", area: "floating", slotKey: "snapshot", order: 10 },
      { commandId: "project.save", label: "저장", category: "프로젝트", surface: "remote", area: "floating", slotKey: "save", order: 20 },
      { commandId: "document.detachCurrent", label: "창 분리", category: "문서", surface: "remote", area: "floating", slotKey: "detach", order: 30 }
    ],
    availableCommands: [
      { commandId: "project.save", label: "저장", category: "프로젝트", surface: "catalog", area: "catalog", slotKey: "project.save", order: 1 },
      { commandId: "document.createScene", label: "새 장면", category: "문서", surface: "catalog", area: "catalog", slotKey: "document.createScene", order: 2 },
      { commandId: "story.relationshipMap.open", label: "관계도", category: "구조", surface: "catalog", area: "catalog", slotKey: "story.relationshipMap.open", order: 3 },
      { commandId: "view.preview.toggle", label: "미리보기", category: "보기", surface: "catalog", area: "catalog", slotKey: "view.preview.toggle", order: 4 },
      { commandId: "help.open", label: "도움말", category: "도움말", surface: "catalog", area: "catalog", slotKey: "help.open", order: 5 }
    ],
    shortcutBindings: [
      { commandId: "project.save", commandName: "저장", category: "프로젝트", scope: "Global", gesture: "Ctrl+S" },
      { commandId: "view.preview.toggle", commandName: "미리보기", category: "보기", scope: "Workbench", gesture: "Ctrl+Alt+P" },
      { commandId: "help.open", commandName: "도움말", category: "도움말", scope: "Global", gesture: "F1" }
    ],
    commands: [],
    statusText: "메인",
    graphicPresetName: "기본",
    autosaveEnabled: true,
    activeView: "editor",
    previewText: "여기에 원고를 씁니다.\n\n메인에서도 현재 장면 본문을 바로 수정할 수 있습니다."
  });
}
