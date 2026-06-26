const state = {
  payload: null,
};

const $ = (id) => document.getElementById(id);

function sendCommand(commandId) {
  if (!commandId) {
    return;
  }

  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.postMessage({ type: "command", commandId });
  }
}

function formatNumber(value) {
  return new Intl.NumberFormat("ko-KR").format(value || 0);
}

function render(payload) {
  state.payload = payload;
  const project = payload.project || payload.Project;
  const active = payload.activeScene || payload.ActiveScene;
  const binder = payload.binder || payload.Binder || [];
  const commands = payload.commands || payload.Commands || [];
  const statusText = payload.statusText || payload.StatusText || "";
  const graphicPresetName = payload.graphicPresetName || payload.GraphicPresetName || "기본";
  const autosaveEnabled = payload.autosaveEnabled ?? payload.AutosaveEnabled ?? true;

  $("project-title").textContent = project.title || project.Title || "원고 작업대";
  $("project-path").textContent = project.rootPath || project.RootPath || "";
  $("scene-count-pill").textContent = `${formatNumber(project.sceneCount || project.SceneCount)} 장면`;
  $("theme-pill").textContent = graphicPresetName;
  $("autosave-pill").textContent = autosaveEnabled ? "자동저장 켬" : "자동저장 끔";
  $("status-text").textContent = statusText;

  renderCommands(commands.slice(0, 14));
  renderBinder(binder);
  renderActiveScene(active);
  renderPipeline(binder);
}

function renderCommands(commands) {
  const ribbon = $("command-ribbon");
  ribbon.textContent = "";
  for (const command of commands) {
    const commandId = command.commandId || command.CommandId;
    const category = command.category || command.Category || "";
    const label = command.label || command.Label || commandId;
    const button = document.createElement("button");
    button.className = "command-button";
    button.dataset.command = commandId;
    button.innerHTML = `<span class="badge">${category.slice(0, 1) || "C"}</span><span></span>`;
    button.lastElementChild.textContent = label;
    button.addEventListener("click", (event) => {
      event.stopPropagation();
      sendCommand(commandId);
    });
    ribbon.appendChild(button);
  }
}

function renderBinder(items) {
  $("binder-count").textContent = formatNumber(items.length);
  const list = $("scene-list");
  list.textContent = "";
  for (const item of items) {
    const id = item.id || item.Id;
    const title = item.title || item.Title;
    const status = item.status || item.Status;
    const sceneType = item.sceneType || item.SceneType;
    const length = item.contentLength || item.ContentLength || 0;
    const isActive = item.isActive || item.IsActive;
    const row = document.createElement("article");
    row.className = `scene-item${isActive ? " active" : ""}`;
    row.innerHTML = `
      <div class="scene-line">
        <span class="scene-title"></span>
        <span class="pill"></span>
      </div>
      <div class="scene-meta">
        <span></span>
        <span></span>
      </div>`;
    row.querySelector(".scene-title").textContent = title;
    row.querySelector(".pill").textContent = status;
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
    $("active-status").textContent = "-";
    $("active-length").textContent = "0";
    $("active-length-spaces").textContent = "0";
    $("active-type").textContent = "Scene";
    $("active-summary").textContent = "";
    $("active-tags").textContent = "";
    return;
  }

  $("active-title").textContent = active.title || active.Title;
  $("active-status").textContent = active.status || active.Status;
  $("active-length").textContent = formatNumber(active.contentLength || active.ContentLength);
  $("active-length-spaces").textContent = formatNumber(active.contentLengthWithSpaces || active.ContentLengthWithSpaces);
  $("active-type").textContent = active.sceneType || active.SceneType;
  $("active-summary").textContent = active.summary || active.Summary || "";

  const tags = active.tags || active.Tags || [];
  const tagRow = $("active-tags");
  tagRow.textContent = "";
  for (const tag of tags) {
    const span = document.createElement("span");
    span.className = "tag";
    span.textContent = tag;
    tagRow.appendChild(span);
  }
}

function renderPipeline(items) {
  const counts = { draft: 0, revising: 0, final: 0, excluded: 0 };
  for (const item of items) {
    const status = item.status || item.Status;
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

document.addEventListener("click", (event) => {
  const button = event.target.closest("button[data-command]");
  if (button) {
    sendCommand(button.dataset.command);
  }
});

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
      summary: "HTML 작업대 미리보기",
      tags: ["주인공", "도입"],
      contentLength: 1200,
      contentLengthWithSpaces: 1360,
      sceneType: "Scene"
    },
    binder: [
      { id: "scene-0001", title: "첫 장면", status: "초고", sceneType: "Scene", contentLength: 1200, isActive: true },
      { id: "scene-0002", title: "추격", status: "수정중", sceneType: "Action", contentLength: 900, isActive: false },
      { id: "scene-0003", title: "결말", status: "완료", sceneType: "Scene", contentLength: 1500, isActive: false }
    ],
    commands: [
      { commandId: "project.save", label: "저장", category: "프로젝트" },
      { commandId: "document.createScene", label: "새 장면", category: "문서" },
      { commandId: "story.relationshipMap.open", label: "관계도", category: "구조" },
      { commandId: "writing.focus.toggle", label: "집중", category: "집필" }
    ],
    statusText: "HTML 작업대",
    graphicPresetName: "기본",
    autosaveEnabled: true
  });
}
