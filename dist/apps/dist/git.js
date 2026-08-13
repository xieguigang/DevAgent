"use strict";

/* ---------------------------------------------------------
       Enums (mirror diff.vb: DiffLineType & FileChangeKind)
       DiffLineType : 0=Added, 1=Deleted, 2=Context
       FileChangeKind: 0=Added, 1=Modified, 2=Deleted, 3=Renamed
    --------------------------------------------------------- */
const KIND = {
  0: { key: "added", label: "Added", letter: "A" },
  1: { key: "modified", label: "Modified", letter: "M" },
  2: { key: "deleted", label: "Deleted", letter: "D" },
  3: { key: "renamed", label: "Renamed", letter: "R" },
};
const LTYPE = { 0: "added", 1: "deleted", 2: "context" };

const state = {
  files: [], // normalized: {filePath, changeKind, hunks, added, removed}
  current: -1, // index into state.files (filtered order irrelevant; stored as original index)
  currentRef: null, // reference to the file object itself
  search: "",
  kindFilter: "all",
  hideContext: false,
  collapsed: new Set(), // keys "i:h"
  loaded: false,
};

/* ---------------- helpers ---------------- */
function esc(s) {
  return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) {
    return {
      "&": "&amp;",
      "<": "&lt;",
      ">": "&gt;",
      '"': "&quot;",
      "'": "&#39;",
    }[c];
  });
}

function basename(p) {
  const i = Math.max(p.lastIndexOf("/"), p.lastIndexOf("\\"));
  return i < 0 ? p : p.slice(i + 1);
}

function dirname(p) {
  const i = Math.max(p.lastIndexOf("/"), p.lastIndexOf("\\"));
  if (i < 0) return "";
  const d = p.slice(0, i);
  return d.length > 40 ? "…" + d.slice(-39) : d;
}

function extOf(p) {
  const b = basename(p);
  const i = b.lastIndexOf(".");
  if (i < 0) return "·";
  return b
    .slice(i + 1)
    .slice(0, 4)
    .toLowerCase();
}

function kindInfo(k) {
  return KIND[k] || { key: "modified", label: "Changed", letter: "?" };
}

/* ---------------- normalize ---------------- */
function normalize(data) {
  if (!data || !Array.isArray(data.Files)) {
    throw new Error("JSON 结构无效：缺少 Files 数组（应为 DiffResult 对象）。");
  }
  return data.Files.map(function (f) {
    const hunks = Array.isArray(f.Hunks) ? f.Hunks : [];
    let added = 0,
      removed = 0;
    hunks.forEach(function (h) {
      const lines = Array.isArray(h.Lines) ? h.Lines : [];
      lines.forEach(function (l) {
        if (l.Type === 0) added++;
        else if (l.Type === 1) removed++;
      });
    });
    return {
      filePath: f.FilePath || "",
      changeKind: typeof f.ChangeKind === "number" ? f.ChangeKind : 1,
      hunks: hunks,
      added: added,
      removed: removed,
    };
  });
}

/* ---------------- loading ---------------- */
function loadFile(file) {
  if (!file) return;
  const reader = new FileReader();
  reader.onload = function (e) {
    loadGitDiff(e.target.result || "");
  };
  reader.onerror = function () {
    toast("读取文件失败。", true);
  };
  reader.readAsText(file);
}

function loadGitDiff(text) {
  try {
    const data = JSON.parse((text || "").replace(/^\uFEFF/, ""));
    state.files = normalize(data);
    state.loaded = true;
    state.current = state.files.length ? 0 : -1;
    state.currentRef = state.files[state.current] || null;
    state.collapsed.clear();
    renderAll();
    toast("已加载 " + state.files.length + " 个文件");
  } catch (err) {
    toast("加载失败：" + err.message, true);
  }
}

/* ---------------- filtering ---------------- */
function filteredFiles() {
  const q = state.search.trim().toLowerCase();
  return state.files.filter(function (f) {
    if (state.kindFilter !== "all" && String(f.changeKind) !== state.kindFilter)
      return false;
    if (q && f.filePath.toLowerCase().indexOf(q) < 0) return false;
    return true;
  });
}

/* ---------------- render: file list ---------------- */
function renderFileList() {
  const scroll = document.getElementById("file-list-scroll");
  const list = filteredFiles();
  document.getElementById("file-count").textContent = list.length;

  if (!state.loaded) {
    scroll.innerHTML =
      '<div class="file-list-empty">尚未加载数据<br/>点击「打开 JSON」或拖入文件</div>';
    return;
  }
  if (!list.length) {
    scroll.innerHTML = '<div class="file-list-empty">没有匹配的文件</div>';
    return;
  }

  const html = list
    .map(function (f) {
      const ki = kindInfo(f.changeKind);
      const active = state.currentRef === f ? " active" : "";
      const add = f.added
        ? '<span class="stat-add">+' + f.added + "</span>"
        : "";
      const del = f.removed
        ? '<span class="stat-del">-' + f.removed + "</span>"
        : "";
      return (
        '<div class="file-item' +
        active +
        '" data-path="' +
        esc(f.filePath) +
        '">' +
        '<div class="ext-badge">' +
        esc(extOf(f.filePath)) +
        "</div>" +
        '<div class="file-meta">' +
        '<div class="file-name">' +
        esc(basename(f.filePath)) +
        "</div>" +
        '<div class="file-path">' +
        esc(dirname(f.filePath)) +
        "</div>" +
        '<div class="file-stats">' +
        '<span class="kind-badge ' +
        ki.key +
        '">' +
        ki.letter +
        "</span>" +
        add +
        del +
        "</div></div></div>"
      );
    })
    .join("");
  scroll.innerHTML = html;
}

/* ---------------- render: diff view ---------------- */
function renderDiff() {
  const scroll = document.getElementById("diff-scroll");
  const empty = document.getElementById("diff-empty");

  if (!state.loaded || !state.currentRef) {
    scroll.style.display = "none";
    empty.style.display = "flex";
    return;
  }
  empty.style.display = "none";
  scroll.style.display = "block";

  const f = state.currentRef;
  const ki = kindInfo(f.changeKind);

  // file header
  let out = "";
  out +=
    '<div class="diff-file-header">' +
    '<span class="kind-badge ' +
    ki.key +
    '">' +
    ki.letter +
    "</span>" +
    '<span class="df-path">' +
    esc(f.filePath) +
    "</span>" +
    '<div class="df-stats">' +
    (f.added ? '<span class="df-add">+' + f.added + "</span>" : "") +
    (f.removed ? '<span class="df-del">-' + f.removed + "</span>" : "") +
    "</div></div>";

  if (!f.hunks.length) {
    out +=
      '<div class="hunk"><div class="hunk-lines" style="padding:16px 22px;color:var(--ui-foreground-muted)">该文件没有差异内容（可能是二进制或空）。</div></div>';
    scroll.innerHTML = out;
    return;
  }

  const fileIdx = state.files.indexOf(f);
  f.hunks.forEach(function (h, hi) {
    const key = fileIdx + ":" + hi;
    const collapsed = state.collapsed.has(key);
    const lines = Array.isArray(h.Lines) ? h.Lines : [];

    const oldStart = h.OldStart || 0;
    const newStart = h.NewStart || 0;
    const oldCount = h.OldCount || 0;
    const newCount = h.NewCount || 0;
    const range =
      "@@ -" +
      oldStart +
      (oldCount ? "," + oldCount : "") +
      " +" +
      newStart +
      (newCount ? "," + newCount : "") +
      " @@";

    let lineHtml = "";
    let o = oldStart,
      n = newStart;
    lines.forEach(function (l) {
      if (state.hideContext && l.Type === 2) return;
      const cls = LTYPE[l.Type] || "context";
      const oldCell = l.Type === 0 ? "" : o++;
      const newCell = l.Type === 1 ? "" : n++;
      const sign = l.Type === 0 ? "+" : l.Type === 1 ? "-" : " ";
      lineHtml +=
        '<div class="diff-line ' +
        cls +
        '">' +
        '<span class="lno">' +
        oldCell +
        "</span>" +
        '<span class="lno new">' +
        newCell +
        "</span>" +
        '<span class="lcontent"><span class="sign">' +
        sign +
        "</span>" +
        esc(l.Content) +
        "</span>" +
        "</div>";
    });

    out +=
      '<div class="hunk' +
      (collapsed ? " collapsed" : "") +
      '" data-key="' +
      key +
      '">' +
      '<div class="hunk-header" data-toggle="' +
      key +
      '">' +
      '<span class="hunk-toggle">▾</span>' +
      '<span class="hunk-range">' +
      esc(range) +
      "</span>" +
      '<span class="hunk-meta">' +
      lines.length +
      " 行</span>" +
      "</div>" +
      '<div class="hunk-lines">' +
      lineHtml +
      "</div>" +
      "</div>";
  });

  scroll.innerHTML = out;
}

/* ---------------- status bar ---------------- */
function updateStatus() {
  if (!state.loaded) {
    document.getElementById("status-total").textContent = "未加载数据";
    document.getElementById("status-file").textContent = "";
    return;
  }
  let totalAdd = 0,
    totalDel = 0;
  state.files.forEach(function (f) {
    totalAdd += f.added;
    totalDel += f.removed;
  });
  const list = filteredFiles();
  document.getElementById("status-total").innerHTML =
    state.files.length +
    " 个文件 · <b>+" +
    totalAdd +
    "</b> / <b>-" +
    totalDel +
    "</b>" +
    (list.length !== state.files.length ? "（显示 " + list.length + "）" : "");
  document.getElementById("status-file").textContent = state.currentRef
    ? basename(state.currentRef.filePath)
    : "";
}

function renderAll() {
  renderFileList();
  renderDiff();
  updateStatus();
}

/* ---------------- diff text (copy) ---------------- */
function diffToText(f) {
  let t = "";
  t += "diff --git a/" + f.filePath + " b/" + f.filePath + "\n";
  t += "--- a/" + f.filePath + "\n";
  t += "+++ b/" + f.filePath + "\n";
  f.hunks.forEach(function (h) {
    const oldStart = h.OldStart || 0,
      newStart = h.NewStart || 0;
    const oldCount = h.OldCount || 0,
      newCount = h.NewCount || 0;
    t +=
      "@@ -" +
      oldStart +
      (oldCount ? "," + oldCount : "") +
      " +" +
      newStart +
      (newCount ? "," + newCount : "") +
      " @@\n";
    (Array.isArray(h.Lines) ? h.Lines : []).forEach(function (l) {
      const sign = l.Type === 0 ? "+" : l.Type === 1 ? "-" : " ";
      t += sign + (l.Content == null ? "" : l.Content) + "\n";
    });
  });
  return t;
}

function copyCurrent() {
  if (!state.currentRef) {
    toast("没有可复制的内容。", true);
    return;
  }
  const text = diffToText(state.currentRef);
  function done() {
    toast("已复制当前文件 diff 到剪贴板。");
  }
  if (navigator.clipboard && navigator.clipboard.writeText) {
    navigator.clipboard.writeText(text).then(done, fallback);
  } else {
    fallback();
  }
  function fallback() {
    const ta = document.createElement("textarea");
    ta.value = text;
    document.body.appendChild(ta);
    ta.select();
    try {
      document.execCommand("copy");
      done();
    } catch (e) {
      toast("复制失败，请手动选择。", true);
    }
    document.body.removeChild(ta);
  }
}

/* ---------------- toast ---------------- */
let toastTimer = null;
function toast(msg, isError) {
  const el = document.getElementById("toast");
  el.textContent = msg;
  el.className = "toast show" + (isError ? " error" : "");
  clearTimeout(toastTimer);
  toastTimer = setTimeout(function () {
    el.className = "toast";
  }, 2800);
}

/* ---------------- events ---------------- */
function selectFile(filePath) {
  const f = state.files.find(function (x) {
    return x.filePath === filePath;
  });
  if (f) {
    state.currentRef = f;
    renderAll();
  }
}

document.getElementById("btn-open").addEventListener("click", function () {
  document.getElementById("file-input").click();
});
document.getElementById("btn-open-2").addEventListener("click", function () {
  document.getElementById("file-input").click();
});
document.getElementById("file-input").addEventListener("change", function (e) {
  if (e.target.files && e.target.files[0]) loadFile(e.target.files[0]);
  e.target.value = "";
});

document.getElementById("search").addEventListener("input", function (e) {
  state.search = e.target.value;
  renderFileList();
  updateStatus();
});
document.getElementById("kind-filter").addEventListener("change", function (e) {
  state.kindFilter = e.target.value;
  renderFileList();
  updateStatus();
});
document
  .getElementById("hide-context")
  .addEventListener("change", function (e) {
    state.hideContext = e.target.checked;
    renderDiff();
  });
document.getElementById("btn-copy").addEventListener("click", copyCurrent);

document.getElementById("btn-collapse").addEventListener("click", function () {
  if (!state.currentRef) return;
  const fi = state.files.indexOf(state.currentRef);
  const allKeys = state.currentRef.hunks.map(function (_, hi) {
    return fi + ":" + hi;
  });
  const allCollapsed = allKeys.every(function (k) {
    return state.collapsed.has(k);
  });
  if (allCollapsed)
    allKeys.forEach(function (k) {
      state.collapsed.delete(k);
    });
  else
    allKeys.forEach(function (k) {
      state.collapsed.add(k);
    });
  renderDiff();
});

// file list click
document
  .getElementById("file-list-scroll")
  .addEventListener("click", function (e) {
    const item = e.target.closest(".file-item");
    if (item) selectFile(item.getAttribute("data-path"));
  });

// hunk collapse toggle
document.getElementById("diff-scroll").addEventListener("click", function (e) {
  const hdr = e.target.closest(".hunk-header");
  if (hdr) {
    const key = hdr.getAttribute("data-toggle");
    const hunk = hdr.parentElement;
    if (state.collapsed.has(key)) {
      state.collapsed.delete(key);
      hunk.classList.remove("collapsed");
    } else {
      state.collapsed.add(key);
      hunk.classList.add("collapsed");
    }
  }
});

// drag & drop
const app = document.getElementById("git-app");
["dragenter", "dragover"].forEach(function (ev) {
  app.addEventListener(ev, function (e) {
    e.preventDefault();
    app.classList.add("dragover");
  });
});
["dragleave", "drop"].forEach(function (ev) {
  app.addEventListener(ev, function (e) {
    e.preventDefault();
    if (ev === "dragleave" && app.contains(e.relatedTarget)) return;
    app.classList.remove("dragover");
  });
});
app.addEventListener("drop", function (e) {
  if (e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files[0]) {
    loadFile(e.dataTransfer.files[0]);
  }
});

// keyboard: Esc clears search
document.addEventListener("keydown", function (e) {
  if (e.key === "Escape") {
    const s = document.getElementById("search");
    if (s.value) {
      s.value = "";
      state.search = "";
      renderFileList();
      updateStatus();
    }
  }
});

window.addEventListener("DOMContentLoaded", () => {
  window.chrome.webview.addEventListener("message", function (event) {
    const message = event.data;

    if (message.type === "loadFile") {
      // 直接使用传递过来的 text 和 filename
      loadGitDiff(message.text);
    }
  });
});
