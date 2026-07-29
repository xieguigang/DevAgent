var importsConfigJSON = null;

(function () {
  "use strict";

  /* ============================================================
               Settings management.
               All settings are stored as a single JSON object and persisted
               to localStorage. The same JSON object can be imported from a
               file or exported to a file.
               ============================================================ */

  var STORAGE_KEY = "codeEditor.settings";

  /* ---- LLM function tool catalog ---- */
  var LLM_TOOLS = [
    {
      id: "code_completion",
      label: "Code Completion",
      desc: "Suggest code completions at the cursor position.",
    },
    {
      id: "code_explanation",
      label: "Explain Code",
      desc: "Generate natural-language explanations of selected code.",
    },
    {
      id: "code_refactor",
      label: "Refactor Code",
      desc: "Restructure code without changing its behavior.",
    },
    {
      id: "code_review",
      label: "Code Review",
      desc: "Review selected code for bugs and improvements.",
    },
    {
      id: "generate_tests",
      label: "Generate Tests",
      desc: "Produce unit tests for the selected function or module.",
    },
    {
      id: "generate_docs",
      label: "Generate Documentation",
      desc: "Generate doc comments and API documentation.",
    },
    {
      id: "fix_errors",
      label: "Fix Errors",
      desc: "Diagnose and propose fixes for compile/runtime errors.",
    },
    {
      id: "chat",
      label: "Chat Assistant",
      desc: "General-purpose conversational assistant.",
    },
    {
      id: "semantic_search",
      label: "Semantic Search",
      desc: "Search the codebase using natural-language queries.",
    },
    {
      id: "summarize",
      label: "Summarize File",
      desc: "Produce a concise summary of the current file.",
    },
    {
      id: "translate",
      label: "Translate Language",
      desc: "Convert code between VB.NET, R, and other languages.",
    },
    {
      id: "snippet_gen",
      label: "Generate Snippet",
      desc: "Create reusable code snippets from a description.",
    },
  ];

  /* ---- Default settings ---- */
  function defaultSettings() {
    var tools = {};
    LLM_TOOLS.forEach(function (t) {
      tools[t.id] = true;
    });
    return {
      appearance: {
        theme: "light",
        showSymbols: true,
        showLineNumbers: true,
        showMinimap: false,
        enableFolding: true,
        fontSize: 14,
      },
      llm: {
        endpoint: "https://api.openai.com/v1",
        apiKey: "",
        model: "gpt-4o",
        temperature: 0.2,
        maxTokens: 2048,
        enabledTools: tools,
      },
      devTools: {
        dotnetPath: "",
        gitPath: "",
        rscriptPath: "",
      },
    };
  }

  /* ---- State ---- */
  var settings = defaultSettings();

  /* ============================================================
               DOM helpers
               ============================================================ */

  function $(id) {
    return document.getElementById(id);
  }
  function $$(sel) {
    return document.querySelectorAll(sel);
  }

  function escapeHtml(text) {
    if (text == null) return "";
    return String(text)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  /* ============================================================
               Load / save to localStorage
               ============================================================ */

  function loadFromStorage() {
    try {
      var raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      var parsed = JSON.parse(raw);
      return parsed;
    } catch (e) {
      console.warn("Failed to load settings from localStorage:", e);
      return null;
    }
  }

  function saveToStorage() {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
      return true;
    } catch (e) {
      console.warn("Failed to save settings to localStorage:", e);
      return false;
    }
  }

  /* ============================================================
               Apply settings JSON to the form
               ============================================================ */

  function applySettingsToForm(s) {
    var a = s.appearance || {};
    var l = s.llm || {};
    var d = s.devTools || {};

    // Appearance
    setTheme(a.theme || "light");
    $("show-symbols").checked = a.showSymbols !== false;
    $("show-line-numbers").checked = a.showLineNumbers !== false;
    $("show-minimap").checked = a.showMinimap === true;
    $("enable-folding").checked = a.enableFolding !== false;
    $("font-size").value = a.fontSize || 14;

    // LLM
    $("llm-endpoint").value = l.endpoint || "";
    $("llm-api-key").value = l.apiKey || "";
    $("llm-model").value = l.model || "";
    $("llm-temperature").value = l.temperature != null ? l.temperature : 0.2;
    $("llm-max-tokens").value = l.maxTokens || 2048;
    renderToolCheckboxes(l.enabledTools || {});

    // Dev tools
    $("dotnet-path").value = d.dotnetPath || "";
    $("git-path").value = d.gitPath || "";
    $("rscript-path").value = d.rscriptPath || "";
    updatePathStatuses();
  }

  /* ============================================================
               Collect form values into a settings JSON object
               ============================================================ */

  function collectSettingsFromForm() {
    var enabledTools = {};
    $$("#llm-tools-grid .checkbox-item input[type=checkbox]").forEach(
      function (cb) {
        enabledTools[cb.value] = cb.checked;
      },
    );
    return {
      appearance: {
        theme: getSelectedTheme(),
        showSymbols: $("show-symbols").checked,
        showLineNumbers: $("show-line-numbers").checked,
        showMinimap: $("show-minimap").checked,
        enableFolding: $("enable-folding").checked,
        fontSize: parseInt($("font-size").value, 10) || 14,
      },
      llm: {
        endpoint: $("llm-endpoint").value.trim(),
        apiKey: $("llm-api-key").value,
        model: $("llm-model").value.trim(),
        temperature: parseFloat($("llm-temperature").value) || 0,
        maxTokens: parseInt($("llm-max-tokens").value, 10) || 2048,
        enabledTools: enabledTools,
      },
      devTools: {
        dotnetPath: $("dotnet-path").value.trim(),
        gitPath: $("git-path").value.trim(),
        rscriptPath: $("rscript-path").value.trim(),
      },
    };
  }

  /* ============================================================
               Theme picker
               ============================================================ */

  function getSelectedTheme() {
    var checked = document.querySelector('input[name="theme"]:checked');
    return checked ? checked.value : "light";
  }

  function setTheme(theme) {
    var radios = document.querySelectorAll('input[name="theme"]');
    radios.forEach(function (r) {
      r.checked = r.value === theme;
    });
    $$(".theme-option").forEach(function (opt) {
      opt.classList.toggle(
        "selected",
        opt.getAttribute("data-theme") === theme,
      );
    });
  }

  /* ============================================================
               LLM tool checkboxes
               ============================================================ */

  function renderToolCheckboxes(enabledMap) {
    var grid = $("llm-tools-grid");
    grid.innerHTML = "";
    LLM_TOOLS.forEach(function (tool) {
      var enabled = enabledMap[tool.id] !== false;
      var item = document.createElement("label");
      item.className = "checkbox-item" + (enabled ? " checked" : "");
      item.innerHTML =
        '<input type="checkbox" value="' +
        escapeHtml(tool.id) +
        '"' +
        (enabled ? " checked" : "") +
        ">" +
        '<div class="cb-content">' +
        '<span class="cb-label">' +
        escapeHtml(tool.label) +
        "</span>" +
        '<div class="cb-desc">' +
        escapeHtml(tool.desc) +
        "</div>" +
        "</div>";
      var cb = item.querySelector("input");
      cb.addEventListener("change", function () {
        item.classList.toggle("checked", cb.checked);
      });
      grid.appendChild(item);
    });
  }

  /* ============================================================
               Path validation (heuristic — checks for non-empty path)
               ============================================================ */

  function updatePathStatuses() {
    updatePathStatus("dotnet-path", "dotnet-status");
    updatePathStatus("git-path", "git-status");
    updatePathStatus("rscript-path", "rscript-status");
  }

  function updatePathStatus(inputId, statusId) {
    var input = $(inputId);
    var status = $(statusId);
    var value = input.value.trim();
    if (!value) {
      status.textContent = "Empty — will use system PATH";
      status.className = "path-status unknown";
    } else {
      status.textContent = "Configured: " + value;
      status.className = "path-status valid";
    }
  }

  /* ============================================================
               Schema preview
               ============================================================ */

  function renderSchemaPreview() {
    var sample = {
      appearance: {
        theme: "light | dark",
        showSymbols: true,
        showLineNumbers: true,
        showMinimap: false,
        enableFolding: true,
        fontSize: 14,
      },
      llm: {
        endpoint: "https://api.openai.com/v1",
        apiKey: "sk-...",
        model: "gpt-4o",
        temperature: 0.2,
        maxTokens: 2048,
        enabledTools: {
          code_completion: true,
          code_explanation: true,
          code_refactor: true,
          code_review: true,
          generate_tests: true,
          generate_docs: true,
          fix_errors: true,
          chat: true,
          semantic_search: true,
          summarize: true,
          translate: true,
          snippet_gen: true,
        },
      },
      devTools: {
        dotnetPath: "/usr/local/share/dotnet/dotnet",
        gitPath: "/usr/bin/git",
        rscriptPath: "/usr/local/bin/Rscript",
      },
    };
    $("schema-preview").textContent = JSON.stringify(sample, null, 2);
  }

  /* ============================================================
               Toast
               ============================================================ */

  var toastTimer = null;
  function showToast(message, type) {
    var toast = $("toast");
    toast.textContent = message;
    toast.className = "toast visible" + (type ? " " + type : "");
    if (toastTimer) clearTimeout(toastTimer);
    toastTimer = setTimeout(function () {
      toast.className = "toast";
    }, 2800);
  }

  /* ============================================================
               Status message
               ============================================================ */

  function setStatus(message, type) {
    var el = $("status-msg");
    el.textContent = message;
    el.className = "status-msg" + (type ? " " + type : "");
  }

  /* ============================================================
               Deep merge (source overrides target)
               ============================================================ */

  function deepMerge(target, source) {
    if (typeof source !== "object" || source === null) return target;
    if (typeof target !== "object" || target === null) return source;
    var result = Array.isArray(target)
      ? target.slice()
      : Object.assign({}, target);
    for (var key in source) {
      if (!source.hasOwnProperty(key)) continue;
      if (
        typeof source[key] === "object" &&
        source[key] !== null &&
        !Array.isArray(source[key])
      ) {
        result[key] = deepMerge(target[key] || {}, source[key]);
      } else {
        result[key] = source[key];
      }
    }
    return result;
  }

  /* ============================================================
               Import / Export
               ============================================================ */

  function exportToFile() {
    var current = collectSettingsFromForm();
    var json = JSON.stringify(current, null, 2);
    var blob = new Blob([json], { type: "application/json" });
    var url = URL.createObjectURL(blob);
    var a = document.createElement("a");
    a.href = url;
    a.download = "code-editor-settings.json";
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    showToast("Settings exported to code-editor-settings.json", "success");
    setStatus("Exported to code-editor-settings.json", "success");
  }

  function importFromFile(file) {
    var reader = new FileReader();

    reader.onload = () => importFromJSONString(reader.result, file.name);
    reader.onerror = () => showToast("Failed to read file.", "error");
    reader.readAsText(file);
  }

  function importFromJSONString(json, filename) {
    console.log("load config from a given json string:");
    console.log(json);

    try {
      var parsed = JSON.parse(json);
      settings = deepMerge(defaultSettings(), parsed);
      
      console.log(settings);

      applySettingsToForm(settings);
      saveToStorage();
      showToast("Settings imported successfully.", "success");

      if (filename) {
        setStatus("Imported settings from " + filename, "success");
      }
    } catch (e) {
      showToast("Invalid JSON file: " + e.message, "error");
      setStatus("Import failed: " + e.message, "error");
    }
  }

  importsConfigJSON = (json) => importFromJSONString(json, null);

  window.chrome.webview.addEventListener("message", function (event) {
    const message = event.data;

    if (message.type === "loadConfig") {
      // 直接使用传递过来的 text 和 filename
      importFromJSONString(message.text, null);
    }
  });

  /* ============================================================
               JSON modal
               ============================================================ */

  function openJsonModal(mode) {
    var modal = $("json-modal");
    var title = $("modal-title");
    var textarea = $("json-textarea");
    if (mode === "view") {
      title.textContent = "Current Configuration JSON";
      textarea.value = JSON.stringify(collectSettingsFromForm(), null, 2);
      textarea.readOnly = true;
    } else {
      title.textContent = "Apply Configuration JSON";
      textarea.value = JSON.stringify(collectSettingsFromForm(), null, 2);
      textarea.readOnly = false;
    }
    modal.classList.add("visible");
    setTimeout(function () {
      textarea.focus();
    }, 50);
  }

  function closeJsonModal() {
    $("json-modal").classList.remove("visible");
  }

  function applyJsonFromModal() {
    var textarea = $("json-textarea");
    try {
      var parsed = JSON.parse(textarea.value);
      settings = deepMerge(defaultSettings(), parsed);
      applySettingsToForm(settings);
      saveToStorage();
      closeJsonModal();
      showToast("Configuration applied from JSON.", "success");
      setStatus("Applied JSON configuration.", "success");
    } catch (e) {
      showToast("Invalid JSON: " + e.message, "error");
    }
  }

  /* ============================================================
               Save
               ============================================================ */

  function save() {
    settings = collectSettingsFromForm();
    devkit.Save(JSON.stringify(settings));

    if (saveToStorage()) {
      showToast("Settings saved.", "success");
      setStatus("Settings saved to local storage.", "success");
    } else {
      showToast("Failed to save settings.", "error");
      setStatus("Save failed.", "error");
    }
  }

  function saveAndClose() {
    settings = collectSettingsFromForm();
    // save();
    // setTimeout(function () {
    //   window.location.href = "index.html";
    // }, 600);
    devkit.SaveAndClose(JSON.stringify(settings));
  }

  function resetToDefaults() {
    if (
      !confirm(
        "Reset all settings to their default values? This cannot be undone.",
      )
    )
      return;
    settings = defaultSettings();
    applySettingsToForm(settings);
    saveToStorage();
    showToast("Settings reset to defaults.", "success");
    setStatus("Reset to defaults.", "success");
  }

  /* ============================================================
               Event wiring
               ============================================================ */

  function attachEvents() {
    // Theme picker
    $$(".theme-option").forEach(function (opt) {
      opt.addEventListener("click", function () {
        setTheme(opt.getAttribute("data-theme"));
      });
    });

    // API key show/hide
    $("toggle-api-key").addEventListener("click", function () {
      var input = $("llm-api-key");
      var icon = $("eye-icon");
      if (input.type === "password") {
        input.type = "text";
        icon.innerHTML =
          '<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path><line x1="1" y1="1" x2="23" y2="23"></line>';
      } else {
        input.type = "password";
        icon.innerHTML =
          '<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path><circle cx="12" cy="12" r="3"></circle>';
      }
    });

    // Path inputs
    ["dotnet-path", "git-path", "rscript-path"].forEach(function (id) {
      $(id).addEventListener("input", function () {
        updatePathStatuses();
      });
    });

    // Browse buttons (file input — works in browsers that support directory picking)
    $$(".browse-btn").forEach(function (btn) {
      btn.addEventListener("click", function () {
        var targetId = btn.getAttribute("data-target-input");
        var target = $(targetId);
        // Browsers don't allow picking an executable path directly.
        // We use a file input as a workaround; the selected file's
        // name is placed in the field.
        var fileInput = document.createElement("input");
        fileInput.type = "file";
        fileInput.addEventListener("change", function () {
          if (fileInput.files && fileInput.files[0]) {
            target.value = fileInput.files[0].name;
            updatePathStatuses();
          }
        });
        fileInput.click();
      });
    });

    // Import / Export
    $("btn-export").addEventListener("click", exportToFile);
    $("btn-import").addEventListener("click", function () {
      $("import-file-input").click();
    });
    $("import-file-input").addEventListener("change", function () {
      if (this.files && this.files[0]) {
        importFromFile(this.files[0]);
        this.value = "";
      }
    });
    $("btn-view-json").addEventListener("click", function () {
      openJsonModal("view");
    });
    $("btn-apply-json").addEventListener("click", function () {
      openJsonModal("apply");
    });

    // Modal
    $("modal-close").addEventListener("click", closeJsonModal);
    $("modal-cancel").addEventListener("click", closeJsonModal);
    $("modal-apply").addEventListener("click", applyJsonFromModal);
    $("json-modal").addEventListener("click", function (e) {
      if (e.target === this) closeJsonModal();
    });

    // Action bar
    $("btn-save").addEventListener("click", save);
    $("btn-save-and-close").addEventListener("click", saveAndClose);
    $("btn-reset").addEventListener("click", resetToDefaults);

    // Sidebar nav — smooth scroll + active state
    $$(".side-nav a").forEach(function (link) {
      link.addEventListener("click", function (e) {
        $$(".side-nav a").forEach(function (l) {
          l.classList.remove("active");
        });
        link.classList.add("active");
      });
    });

    // Auto-save on field change (debounced)
    var saveTimer = null;
    function scheduleAutoSave() {
      if (saveTimer) clearTimeout(saveTimer);
      saveTimer = setTimeout(function () {
        settings = collectSettingsFromForm();
        saveToStorage();
        setStatus("Auto-saved.", "success");
      }, 1000);
    }
    document.addEventListener("change", scheduleAutoSave);
    document.addEventListener("input", function (e) {
      if (
        e.target.tagName === "INPUT" ||
        e.target.tagName === "TEXTAREA" ||
        e.target.tagName === "SELECT"
      ) {
        scheduleAutoSave();
      }
    });

    // Keyboard shortcuts
    document.addEventListener("keydown", function (e) {
      if (e.key === "Escape") {
        if ($("json-modal").classList.contains("visible")) {
          closeJsonModal();
        }
      }
      if ((e.ctrlKey || e.metaKey) && e.key === "s") {
        e.preventDefault();
        save();
      }
    });

    // Scroll spy for sidebar
    var sections = ["appearance", "llms", "devtools", "json"];
    window.addEventListener("scroll", function () {
      var scrollY = window.scrollY + 120;
      var current = sections[0];
      for (var i = 0; i < sections.length; i++) {
        var el = $(sections[i]);
        if (el && el.offsetTop <= scrollY) {
          current = sections[i];
        }
      }
      $$(".side-nav a").forEach(function (link) {
        var target = link.getAttribute("data-target");
        link.classList.toggle("active", target === current);
      });
    });
  }

  /* ============================================================
               Init
               ============================================================ */

  function init() {
    renderSchemaPreview();
    var stored = loadFromStorage();
    if (stored) {
      settings = deepMerge(defaultSettings(), stored);
    }
    applySettingsToForm(settings);
    attachEvents();
    setStatus("Settings loaded. Changes auto-save.");
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
