const state = {
  payload: null,
  remoteIconOnly: false,
  railMode: "binder",
  pendingEditorUpdate: null,
  editorUpdateTimer: 0,
  isRendering: false,
  remoteDrag: null,
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
  const project = readPayloadValue(payload, "project", "Project", {});
  const active = readPayloadValue(payload, "activeScene", "ActiveScene", null);
  const binder = readPayloadValue(payload, "binder", "Binder", []);
  const toolbarCommands = readPayloadValue(payload, "commands", "Commands", []);
  const menuCommands = readPayloadValue(payload, "menuCommands", "MenuCommands", []);
  const remoteCommands = readPayloadValue(payload, "remoteCommands", "RemoteCommands", []);
  const statusText = readPayloadValue(payload, "statusText", "StatusText", "");
  const graphicPresetName = readPayloadValue(payload, "graphicPresetName", "GraphicPresetName", "기본");
  const autosaveEnabled = readPayloadValue(payload, "autosaveEnabled", "AutosaveEnabled", true);

  $("project-title").textContent = readPayloadValue(project, "title", "Title", "원고 작업대");
  $("project-path").textContent = readPayloadValue(project, "rootPath", "RootPath", "");
  $("scene-count-pill").textContent = `${formatNumber(readPayloadValue(project, "sceneCount", "SceneCount", 0))} 장면`;
  $("theme-pill").textContent = graphicPresetName;
  $("autosave-pill").textContent = autosaveEnabled ? "자동저장 켬" : "자동저장 끔";
  $("status-text").textContent = statusText;

  renderTopMenu(menuCommands.length ? menuCommands : toolbarCommands);
  renderBinder(binder);
  renderActiveScene(active);
  renderInspector(active);
  renderPipeline(binder);
  renderSettingsPanel(menuCommands);
  renderReferencePanel(project, active);
  renderRemote(remoteCommands.length ? remoteCommands : toolbarCommands.slice(0, 6));
  state.isRendering = false;
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
    row.innerHTML = `
      <div class="scene-line">
        <span class="scene-title"></span>
        <span class="state-pill"></span>
      </div>
      <div class="scene-meta">
        <span></span>
        <span></span>
      </div>`;
    row.querySelector(".scene-title").textContent = title;
    row.querySelector(".state-pill").textContent = status;
    const meta = row.querySelectorAll(".scene-meta span");
    meta[0].textContent = id;
    meta[1].textContent = `${sceneType} · ${formatNumber(length)}`;
    row.addEventListener("dblclick", () => sendCommand("view.main.open"));
    list.appendChild(row);
  }
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
    $("active-type").textContent = "Scene";
    $("active-segment-status").textContent = "";
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
  const isSegmentMode = readPayloadValue(active, "isSegmentMode", "IsSegmentMode", false);
  const visibleParagraphCount = readPayloadValue(active, "visibleParagraphCount", "VisibleParagraphCount", 0);

  $("active-title").textContent = title;
  if (document.activeElement !== $("active-title-editor")) {
    $("active-title-editor").value = title;
  }
  if (document.activeElement !== $("active-body-editor")) {
    $("active-body-editor").value = editorText;
  }
  $("active-body-editor").disabled = false;
  $("active-status").textContent = status;
  $("active-length").textContent = formatNumber(readPayloadValue(active, "contentLength", "ContentLength", 0));
  $("active-length-spaces").textContent = formatNumber(readPayloadValue(active, "contentLengthWithSpaces", "ContentLengthWithSpaces", 0));
  $("active-type").textContent = sceneType;
  $("active-segment-status").textContent = isSegmentMode
    ? `대형 장면 편집 구간 · ${formatNumber(visibleParagraphCount)}문단`
    : "";
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

function renderSettingsPanel(menuCommands) {
  const list = $("settings-list");
  const commands = (menuCommands || [])
    .map(normalizeCommand)
    .filter((command) => command.area === "top.story" || command.area === "top.tools");
  list.textContent = "";
  $("settings-count").textContent = formatNumber(commands.length);

  for (const command of commands) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "side-action";
    button.dataset.command = command.commandId;
    button.textContent = command.label || command.commandId;
    list.appendChild(button);
  }
}

function renderReferencePanel(project, active) {
  const list = $("reference-list");
  list.textContent = "";
  const references = [
    ["프로젝트", readPayloadValue(project, "rootPath", "RootPath", "")],
    ["현재 장면", active ? `${readPayloadValue(active, "id", "Id", "")} · ${readPayloadValue(active, "title", "Title", "")}` : "-"],
    ["요약", active ? readPayloadValue(active, "summary", "Summary", "-") || "-" : "-"],
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

  const button = event.target.closest("button[data-command]");
  if (button) {
    sendCommand(button.dataset.command);
  }
});

$("active-title-editor").addEventListener("input", scheduleActiveSceneUpdate);
$("active-body-editor").addEventListener("input", scheduleActiveSceneUpdate);

$("remote-drag-handle").addEventListener("pointerdown", startRemoteDrag);
document.addEventListener("pointermove", moveRemoteDrag);
document.addEventListener("pointerup", endRemoteDrag);
document.addEventListener("pointercancel", endRemoteDrag);

function setRailMode(mode) {
  state.railMode = mode || "binder";
  document.querySelectorAll("[data-rail-mode]").forEach((button) => {
    button.classList.toggle("active", button.dataset.railMode === state.railMode);
  });
  document.querySelectorAll(".rail-panel").forEach((panel) => {
    panel.classList.toggle("active", panel.id === `rail-panel-${state.railMode}`);
  });
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
}

function flushActiveSceneUpdate() {
  if (!state.pendingEditorUpdate) return;

  window.clearTimeout(state.editorUpdateTimer);
  const message = state.pendingEditorUpdate;
  state.pendingEditorUpdate = null;
  postWebMessage(message);
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
      editorText: "여기에 원고를 씁니다.\n\n메인에서도 현재 장면 본문을 바로 수정할 수 있습니다.",
      isSegmentMode: false,
      visibleParagraphCount: 2
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
    commands: [],
    statusText: "메인",
    graphicPresetName: "기본",
    autosaveEnabled: true
  });
}
