"use strict";

// Utility functions
const $ = (selector) => document.querySelector(selector);
const $$= (selector) => Array.from(document.querySelectorAll(selector));
const icon = (name) => `<svg class="icon" aria-hidden="true"><use href="#i-${name}"/></svg>`;
const esc = (value) =>
  String(value ?? "").replace(
    /[&<>"']/g,
    (c) =>
      ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]
  );

// Application state
const state = {
  page: "overview",
  records: [],
  categories: [],
  runs: [],
  me: null,
  agent: { mode: "local" },
  sync: { enabled: false, cloudOnline: false, pendingChanges: 0 },
  selected: null,
  run: null,
  busy: false,
  search: "",
  filter: "all",
  intake: { notes: "", narrative: "", location: "", date: "", category: "" },
};

let pendingRequest, toastTimer;

// Page metadata
const pageMeta = {
  overview: ["Overview", "Incident overview", "A clear view of your records and the work ahead."],
  incidents: ["Incident register", "Incident register", "Find the details. Keep the account clear."],
  intake: ["Intake assistant", "A clear account starts here", "Turn the initial account into a draft you can verify and file."],
  history: ["Agent history", "Your agent history", "Every draft, source and review decision in one place."],
  settings: ["Connections", "Workspace connections", "Know where your records and assistance come from."],
};

// Badge renderer
function badge(status) {
  const known = ["Pending", "Ongoing", "Resolved", "Dismissed", "needs_review", "accepted", "rejected", "failed", "cancelled", "running"];
  return `<span class="badge ${known.includes(status) ? status.toLowerCase() : ""}">${esc(
    status === "needs_review" ? "Needs review" : status.charAt(0).toUpperCase() + status.slice(1)
  )}</span>`;
}

// Date formatter
function when(value, opts = {}) {
  return value
    ? new Intl.DateTimeFormat("en-PH", {
        timeZone: "Asia/Manila",
        month: "short",
        day: "numeric",
        ...opts,
      }).format(new Date(value.endsWith("Z") ? value : value + "Z"))
    : "Not recorded";
}

// Get category name
function category(record) {
  return state.categories.find((c) => c.categoryId === record.categoryId)?.name || "Uncategorized incident";
}

// Get resident name
function getResidentName(residentId) {
  if (!residentId) return "Unknown";
  const residents = storage.getResidents();
  const resident = residents.find((r) => r.residentId === residentId);
  return resident ? `${resident.firstName} ${resident.lastName}` : "Unknown";
}

// Error handling
function showError(message) {
  $("#global-error").textContent = message;
  $("#global-error").hidden = false;
}

function clearError() {
  $("#global-error").hidden = true;
}

// Toast notifications
function toast(message) {
  clearTimeout(toastTimer);
  $("#toast").textContent = message;
  $("#toast").hidden = false;
  toastTimer = setTimeout(() => ($("#toast").hidden = true), 4500);
}

// Load all data from storage
async function loadData() {
  state.records = storage.getRecords();
  state.categories = storage.getCategories();
  state.runs = storage.getRuns();
  state.sync = storage.getSyncStatus();

  document.getElementById("nav-count").textContent = state.records.length;

  const el = $("#cloud-status");
  el.textContent = "Local storage (standalone mode)";
  el.classList.remove("offline");
}

// Boot application
async function boot() {
  if (!localStorage.getItem("jwtToken")) return showAuth();

  try {
    state.me = storage.getCurrentUser();
    if (!state.me) return showAuth();

    $("#auth").hidden = true;
    $("#shell").hidden = false;
    clearError();

    $("#profile-name").textContent = state.me.firstName + " " + state.me.lastName;
    $("#profile-role").textContent = state.me.role;
    $("#avatar").textContent = (state.me.firstName + state.me.lastName)
      .split(" ")
      .map((x) => x[0])
      .slice(0, 2)
      .join("");

    $("#demo-banner").hidden = !state.me.demo;

    $("#today").textContent = new Intl.DateTimeFormat("en-PH", {
      timeZone: "Asia/Manila",
      month: "short",
      day: "numeric",
      year: "numeric",
    }).format(new Date());

    await loadData();
    navigate("overview");
  } catch (error) {
    showError(error.message + " Reload the page to retry.");
  }
}

// Navigation
function navigate(page) {
  if (state.busy) {
    toast("Cancel or finish the current draft before changing views.");
    return;
  }

  state.page = page;
  state.search = "";
  state.filter = "all";
  state.selected = null;
  state.run = null;
  clearError();

  $$("[data-nav]").forEach((b) => {
    b.classList.toggle("active", b.dataset.nav === page);
    if (b.dataset.nav === page) b.setAttribute("aria-current", "page");
    else b.removeAttribute("aria-current");
  });

  const meta = pageMeta[page];
  $("#breadcrumb").textContent = meta[0];
  $("#page-title").textContent = meta[1];
  $("#page-description").textContent = meta[2];
  $("#new-incident").hidden = page === "intake";
  $("#sidebar").classList.remove("open");
  $("#menu-toggle").setAttribute("aria-expanded", "false");

  render();
}

// Show authentication page
function showAuth() {
  $("#auth").hidden = false;
  $("#shell").hidden = true;
}

// Login handler
async function login(demo = false) {
  const button = demo ? $("#demo-login") : $("#login-form button[type=submit]");
  button.disabled = true;
  $("#login-error").textContent = "";

  try {
    let user;

    if (demo) {
      // Demo login uses first officer
      const users = storage.getUsers();
      user = users[0];
      if (!user) throw new Error("Demo user not found");
    } else {
      const username = $("#username").value;
      const password = $("#password").value;
      user = storage.findUser(username, password);
      if (!user) throw new Error("Check your username and password and try again.");
    }

    storage.setCurrentUser(user);
    $("#password").value = "";
    await boot();
  } catch (error) {
    $("#login-error").textContent = error.message;
  } finally {
    button.disabled = false;
  }
}

// Logout handler
function logout() {
  if (confirm("Sign out of your workspace?")) {
    storage.logout();
    showAuth();
    $("#login-form").reset();
  }
}

// Metrics section
function metrics() {
  const pending = state.records.filter((r) => r.status === "Pending").length;
  const ongoing = state.records.filter((r) => r.status === "Ongoing").length;
  const resolved = state.records.filter((r) => r.status === "Resolved").length;

  return `<section class="metrics" aria-label="Incident totals">
    ${[
      ["folder", "Total incidents", state.records.length, "Within your record access"],
      ["clock", "Awaiting attention", pending, "Pending incidents"],
      ["sync", "In progress", ongoing, "Ongoing incidents"],
      ["check", "Resolved", resolved, "Recorded resolutions"],
    ]
      .map(
        ([i, label, value, sub]) =>
          `<div class="metric"><span class="metric-label">${icon(i)}${label}</span><div class="metric-value">${value}</div><small>${sub}</small></div>`
      )
      .join("")}
  </section>`;
}

// Incident register section
function register() {
  return `<section class="panel">
    <div class="panel-heading">
      <div>
        <h2>${state.page === "overview" ? "Recent incidents" : "All incidents"}</h2>
        <p>${state.page === "overview" ? "The latest records at your desk." : "Records available to your officer account."}</p>
      </div>
      ${state.page === "overview" ? '<button class="button quiet" data-action="view-all">View all</button>' : ""}
    </div>
    <div class="filters">
      <label class="search">${icon("search")}<input id="record-search" type="search" aria-label="Search incidents" placeholder="Search records…" value="${esc(
        state.search
      )}">
      </label>
      <select id="status-filter" aria-label="Filter by status">
        <option value="all">All statuses</option>
        ${["Pending", "Ongoing", "Resolved", "Dismissed"]
          .map((s) => `<option ${state.filter === s ? "selected" : ""}>${s}</option>`)
          .join("")}
      </select>
    </div>
    <div id="incident-rows"></div>
  </section>`;
}

// Render incident rows
function renderRows() {
  const records = state.records.filter(
    (r) =>
      (state.filter === "all" || r.status === state.filter) &&
      `${r.trackingNumber} ${r.location} ${r.narrative} ${category(r)}`
        .toLowerCase()
        .includes(state.search.toLowerCase())
  );

  const shown = state.page === "overview" ? records.slice(0, 6) : records;

  $("#incident-rows").innerHTML = shown.length
    ? `<div class="table-wrap"><table><thead><tr><th scope="col">Incident</th><th scope="col" class="location-cell">Location</th><th scope="col">Status</th></tr></thead><tbody>
        ${shown
          .map(
            (r) => `<tr><td><button class="incident-link" data-record="${r.recordId}">${esc(category(r))}</button>
            <span class="tracking">${esc(r.trackingNumber)} · ${when(r.incidentDate)}</span></td>
            <td class="location-cell">${esc(r.location)}</td><td>${badge(r.status)}</td></tr>`
          )
          .join("")}
      </tbody></table></div><div class="table-foot"><span>Showing ${shown.length} of ${records.length} incidents</span><span>Latest first</span></div>`
    : `<div class="empty"><h3>${state.records.length ? "No matching incidents" : "Your register is ready"}</h3>${
        state.records.length
          ? "Try another search or choose all statuses."
          : "Use New incident to record the first account."
      }</div>`;
}

// Assistant start section
function assistantStart() {
  return `<div class="assistant-top"><span class="assistant-symbol">${icon("spark")}</span><div><h2>Desk assistant</h2><small>Local assistance</small></div></div>
  <h3>Less paperwork.<br>More clarity.</h3>
  <p>Prepare a brief, find the missing details, and keep your next step in view.</p>
  <button class="workflow-option" data-action="pick-record">${icon("file")}<span>Summarize an incident<small>Start with a record from your register</small></span>${icon("chevron")}</button>
  <button class="workflow-option" data-action="intake">${icon("spark")}<span>Prepare an intake<small>Shape initial notes into a clear account</small></span>${icon("chevron")}</button>
  <div class="assistant-note">Local mode extracts supplied text. Connect AI in Connections for generated summaries.</div>`;
}

// Queue summary
function queueSummary() {
  return `<section class="panel queue-summary">
    <h2 class="section-title">Before you close the desk</h2>
    <div class="queue-line"><span>Pending incidents</span><strong>${state.records.filter((r) => r.status === "Pending").length} to review</strong></div>
    <div class="queue-line"><span>Assistant drafts</span><strong>${state.runs.filter((r) => r.status === "needs_review").length} awaiting your review</strong></div>
    <div class="queue-line"><span>Local storage</span><strong>${state.records.length} records stored</strong></div>
  </section>`;
}

// Intake form
function intakeForm() {
  return `<section class="panel intake-form">
    <h2>Start with what was reported</h2>
    <p>Use the original account. Keep uncertain details attributed to the person reporting them.</p>
    <div class="form-field">
      <label for="intake-notes">Initial account</label>
      <textarea id="intake-notes" rows="7" maxlength="6000" placeholder="What happened, where, when, and who reported it?">${esc(state.intake.notes)}</textarea>
      <small>Up to 6,000 characters. Include only information needed for this record.</small>
    </div>
    <div class="button-row">
      <button class="button primary" data-action="generate-intake">${icon("spark")}Prepare intake draft</button>
      <span class="tracking">Local extraction · no model connected</span>
    </div>
    <form id="file-incident" class="manual-fields">
      <h3>Review and file the incident</h3>
      <p>You can enter these fields directly or use an accepted draft. Filing creates a Pending record.</p>
      <div class="form-grid">
        <div class="form-field">
          <label for="intake-category">Category</label>
          <select id="intake-category">
            <option value="">Select a category</option>
            ${state.categories.map((c) => `<option value="${c.categoryId}">${esc(c.name)}</option>`).join("")}
          </select>
        </div>
        <div class="form-field">
          <label for="intake-location">Location</label>
          <input id="intake-location" type="text" maxlength="200" placeholder="Where did this occur?">
        </div>
        <div class="form-field">
          <label for="intake-date">Incident date</label>
          <input id="intake-date" type="date">
        </div>
        <div class="form-field">
          <label for="intake-complainant">Person reporting</label>
          <input id="intake-complainant" type="text" placeholder="Name or resident ID" list="resident-list">
        </div>
      </div>
      <div class="form-field">
        <label for="intake-narrative">Summary</label>
        <textarea id="intake-narrative" rows="6" maxlength="3000" placeholder="Clear account of the incident">${esc(state.intake.narrative)}</textarea>
      </div>
      <div class="button-row">
        <button type="submit" class="button primary">File incident</button>
        <button type="reset" class="button secondary">Clear</button>
      </div>
    </form>
  </section>`;
}

// History view
function historyView() {
  const runs = state.runs.filter((r) => r.officerId === state.me.officerId || r.officerId === "");
  return `<section class="panel">
    <div class="panel-heading">
      <div>
        <h2>Your assistant runs</h2>
        <p>Drafts and summaries you've created or reviewed.</p>
      </div>
    </div>
    ${runs.length
      ? `<div class="history-grid">
        ${runs
          .map(
            (r) => `<div class="history-card ${r.status}">
          <div class="history-status">${badge(r.status)}</div>
          <h3>${r.recordId ? "Record summary" : "Intake draft"}</h3>
          <p class="history-text">${esc(r.generatedDraft.substring(0, 150))}...</p>
          <small>${when(r.createdAt)}</small>
          <button class="button quiet" data-action="view-run-${r.runId}">View details</button>
        </div>`
          )
          .join("")}
      </div>`
      : `<div class="empty"><h3>No runs yet</h3>Get started by summarizing an incident or preparing an intake.</div>
    }`}
  </section>`;
}

// Settings/connections view
function settingsView() {
  return `<section class="panel">
    <div class="panel-heading">
      <h2>Database and connections</h2>
      <p>Manage your data storage and assistant configuration.</p>
    </div>
    <div class="settings-grid">
      <div class="setting-card">
        <h3>Local storage</h3>
        <p>All your data is stored in this browser's local storage. No cloud sync is configured.</p>
        <small>${state.records.length} incidents recorded</small>
        <button class="button secondary" id="export-btn">Export data</button>
      </div>
      <div class="setting-card">
        <h3>Demo mode</h3>
        <p>This workspace is running in demonstration mode with synthetic records.</p>
        <button class="button danger" id="reset-btn">Reset demo data</button>
      </div>
      <div class="setting-card">
        <h3>Privacy</h3>
        <p>Your incident data stays in your browser. No data is transmitted to external servers.</p>
        <small>Last boot: ${new Date().toLocaleString()}</small>
      </div>
    </div>
  </section>`;
}

// Main render function
function render() {
  let html = "";

  switch (state.page) {
    case "overview":
      clearError();
      html = metrics() + queueSummary() + register();
      break;
    case "incidents":
      clearError();
      html = register();
      break;
    case "intake":
      html = assistantStart() + intakeForm();
      break;
    case "history":
      html = historyView();
      break;
    case "settings":
      html = settingsView();
      break;
  }

  $("#page-content").innerHTML = html;
  if ($("#incident-rows")) {
    renderRows();
  }
  attachEventListeners();
}

// Event listeners
function attachEventListeners() {
  // Navigation
  $$("[data-nav]").forEach((btn) => {
    btn.addEventListener("click", () => navigate(btn.dataset.nav));
  });

  // Search and filter
  const searchInput = $("#record-search");
  const filterSelect = $("#status-filter");

  if (searchInput) {
    searchInput.addEventListener("input", (e) => {
      state.search = e.target.value;
      if ($("#incident-rows")) renderRows();
    });
  }

  if (filterSelect) {
    filterSelect.addEventListener("change", (e) => {
      state.filter = e.target.value;
      if ($("#incident-rows")) renderRows();
    });
  }

  // Action buttons
  $$("[data-action]").forEach((btn) => {
    btn.addEventListener("click", (e) => {
      e.preventDefault();
      handleAction(btn.dataset.action);
    });
  });

  // Incident links
  $$(".incident-link").forEach((link) => {
    link.addEventListener("click", (e) => {
      e.preventDefault();
      viewRecord(link.dataset.record);
    });
  });

  // New incident
  const newBtn = $("#new-incident");
  if (newBtn) {
    newBtn.addEventListener("click", () => navigate("intake"));
  }

  // File incident form
  const fileForm = $("#file-incident");
  if (fileForm) {
    fileForm.addEventListener("submit", (e) => {
      e.preventDefault();
      fileIncident();
    });
  }

  // Settings buttons
  const exportBtn = $("#export-btn");
  if (exportBtn) {
    exportBtn.addEventListener("click", exportData);
  }

  const resetBtn = $("#reset-btn");
  if (resetBtn) {
    resetBtn.addEventListener("click", () => {
      if (confirm("Reset all demo data to initial state? This cannot be undone.")) {
        resetDemoData();
        location.reload();
      }
    });
  }

  // Logout
  $("#logout").addEventListener("click", logout);

  // Mobile menu toggle
  $("#menu-toggle").addEventListener("click", () => {
    const sidebar = $("#sidebar");
    const isOpen = sidebar.classList.toggle("open");
    $("#menu-toggle").setAttribute("aria-expanded", isOpen);
  });
}

// Handle actions
function handleAction(action) {
  if (action === "view-all") {
    navigate("incidents");
  } else if (action === "generate-intake") {
    generateIntakeDraft();
  } else if (action === "pick-record") {
    navigate("incidents");
  } else if (action === "intake") {
    navigate("intake");
  } else if (action.startsWith("view-run-")) {
    const runId = action.replace("view-run-", "");
    viewRun(runId);
  }
}

// View incident record
function viewRecord(recordId) {
  const record = state.records.find((r) => r.recordId === recordId);
  if (!record) return;

  state.selected = record;
  const complainant = getResidentName(record.complainantId);
  const respondent = record.respondentId ? getResidentName(record.respondentId):  "Not recorded";

  const html = `<section class="panel record-detail">
    <div class="panel-heading">
      <h2>${esc(category(record))}</h2>
      <small>${esc(record.trackingNumber)}</small>
    </div>
    <div class="record-info">
      <div><strong>Status</strong> ${badge(record.status)}</div>
      <div><strong>Incident</strong> ${when(record.incidentDate)}</div>
      <div><strong>Reported</strong> ${when(record.reportedDate)}</div>
      <div><strong>Location</strong> ${esc(record.location)}</div>
      <div><strong>Complainant</strong> ${esc(complainant)}</div>
      <div><strong>Respondent</strong> ${esc(respondent)}</div>
    </div>
    <div class="record-narrative">
      <h3>Narrative</h3>
      <p>${esc(record.narrative)}</p>
    </div>
    <div class="button-row">
      <button class="button primary" data-action="edit-record-${record.recordId}">Edit record</button>
      <button class="button secondary" data-action="back">Back to list</button>
      <button class="button danger" data-action="delete-record-${record.recordId}">Delete</button>
    </div>
  </section>`;

  $("#page-content").innerHTML = html;

  $$("[data-action]").forEach((btn) => {
    btn.addEventListener("click", (e) => {
      e.preventDefault();
      const action = btn.dataset.action;
      if (action === "back") {
        navigate(state.page);
      } else if (action.startsWith("edit-record-")) {
        editRecord(record.recordId);
      } else if (action.startsWith("delete-record-")) {
        if (confirm("Delete this incident record? This cannot be undone.")) {
          storage.deleteRecord(record.recordId);
          state.records = storage.getRecords();
          navigate(state.page);
          toast("Incident deleted");
        }
      }
    });
  });
}

// Edit record
function editRecord(recordId) {
  const record = state.records.find((r) => r.recordId === recordId);
  if (!record) return;

  const html = `<section class="panel">
    <h2>Edit incident</h2>
    <form id="edit-form">
      <div class="form-field">
        <label>Status</label>
        <select id="edit-status">
          ${["Pending", "Ongoing", "Resolved", "Dismissed"]
            .map((s) => `<option ${record.status === s ? "selected" : ""}>${s}</option>`)
            .join("")}
        </select>
      </div>
      <div class="form-field">
        <label>Narrative</label>
        <textarea rows="6" id="edit-narrative" maxlength="3000">${esc(record.narrative)}</textarea>
      </div>
      <div class="button-row">
        <button type="submit" class="button primary">Save changes</button>
        <button type="button" class="button secondary" data-action="cancel-edit">Cancel</button>
      </div>
    </form>
  </section>`;

  $("#page-content").innerHTML = html;

  $("#edit-form").addEventListener("submit", (e) => {
    e.preventDefault();
    record.status = $("#edit-status").value;
    record.narrative = $("#edit-narrative").value;
    storage.saveRecord(record);
    state.records = storage.getRecords();
    navigate(state.page);
    toast("Incident updated");
  });

  $$("[data-action]").forEach((btn) => {
    if (btn.dataset.action === "cancel-edit") {
      btn.addEventListener("click", () => viewRecord(recordId));
    }
  });
}

// Generate intake draft
function generateIntakeDraft() {
  const notes = $("#intake-notes").value;
  if (!notes.trim()) {
    toast("Please enter initial account notes");
    return;
  }

  // Simple local extraction (not AI)
  const narrative = notes.split("\n").slice(0, 3).join(" ");

  state.intake.narrative = narrative;
  $("#intake-narrative").value = narrative;
  toast("Intake draft generated from your notes");
}

// File incident
function fileIncident() {
  const categoryId = $("#intake-category").value;
  const location = $("#intake-location").value;
  const date = $("#intake-date").value;
  const narrative = $("#intake-narrative").value;
  const complainantName = $("#intake-complainant").value;

  if (!categoryId || !location || !date || !narrative) {
    toast("Please fill in all required fields");
    return;
  }

  // Find or create complainant
  let complainantId = null;
  const residents = storage.getResidents();
  const existingResident = residents.find((r) => `${r.firstName} ${r.lastName}` === complainantName);

  if (existingResident) {
    complainantId = existingResident.residentId;
  } else if (complainantName) {
    const [firstName, ...lastNameParts] = complainantName.split(" ");
    const lastName = lastNameParts.join(" ") || "Unknown";
    const newResident = storage.saveResident({
      firstName: firstName,
      lastName: lastName,
      contactNumber: "",
      address: "",
      zone: ""
    });
    complainantId = newResident.residentId;
  }

  // Create record
  const record = {
    recordId: storage.generateId(),
    trackingNumber: `BL-${new Date().getFullYear()}-${String(state.records.length + 1).padStart(3, "0")}`,
    categoryId: categoryId,
    officerId: state.me.officerId,
    complainantId: complainantId,
    respondentId: null,
    incidentDate: new Date(date).toISOString(),
    reportedDate: new Date().toISOString(),
    location: location,
    narrative: narrative,
    status: "Pending",
    resolutionDate: null
  };

  storage.saveRecord(record);
  state.records = storage.getRecords();

  // Create agent run
  const run = storage.saveRun({
    officerId: state.me.officerId,
    recordId: record.recordId,
    status: "accepted",
    mode: "local",
    sourceText: $("#intake-notes").value,
    generatedDraft: narrative,
    createdAt: new Date().toISOString(),
    reviewedAt: new Date().toISOString(),
    reviewNotes: "Filed locally"
  });

  state.runs = storage.getRuns();

  // Reset form
  state.intake = { notes: "", narrative: "", location: "", date: "", category: "" };
  navigate("incidents");
  toast("Incident filed successfully");
}

// View run
function viewRun(runId) {
  const run = state.runs.find((r) => r.runId === runId);
  if (!run) return;

  const record = run.recordId ? state.records.find((r) => r.recordId === run.recordId) : null;

  const html = `<section class="panel">
    <h2>Assistant run details</h2>
    <div class="run-info">
      <div><strong>Status</strong> ${badge(run.status)}</div>
      <div><strong>Mode</strong> ${run.mode}</div>
      <div><strong>Created</strong> ${when(run.createdAt)}</div>
      ${run.recordId ? `<div><strong>Record</strong> ${esc(category(record))}</div>` : ""}
    </div>
    <div class="run-content">
      <h3>Generated draft</h3>
      <p>${esc(run.generatedDraft)}</p>
    </div>
    ${run.status === "needs_review"
      ? `<div class="button-row">
        <button class="button primary" id="accept-run">Accept draft</button>
        <button class="button danger" id="reject-run">Reject</button>
      </div>`
      : `<p class="tracking">Review decision: ${run.reviewNotes}</p>`}
    <button class="button secondary" data-action="back">Back to history</button>
  </section>`;

  $("#page-content").innerHTML = html;

  const acceptBtn = $("#accept-run");
  if (acceptBtn) {
    acceptBtn.addEventListener("click", () => {
      run.status = "accepted";
      run.reviewedAt = new Date().toISOString();
      storage.saveRun(run);
      state.runs = storage.getRuns();
      navigate("history");
      toast("Draft accepted");
    });
  }

  const rejectBtn = $("#reject-run");
  if (rejectBtn) {
    rejectBtn.addEventListener("click", () => {
      run.status = "rejected";
      run.reviewedAt = new Date().toISOString();
      storage.saveRun(run);
      state.runs = storage.getRuns();
      navigate("history");
      toast("Draft rejected");
    });
  }

  $$("[data-action]").forEach((btn) => {
    if (btn.dataset.action === "back") {
      btn.addEventListener("click", () => navigate("history"));
    }
  });
}

// Export data
function exportData() {
  const data = storage.exportData();
  const jsonStr = JSON.stringify(data, null, 2);
  const blob = new Blob([jsonStr], { type: "application/json" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `blottersync-export-${new Date().toISOString().split("T")[0]}.json`;
  a.click();
  URL.revokeObjectURL(url);
  toast("Data exported");
}

// Initialize on page load
document.addEventListener("DOMContentLoaded", async () => {
  await storage.init();
  initializeDemoData();

  // Setup login form
  const loginForm = $("#login-form");
  if (loginForm) {
    loginForm.addEventListener("submit", (e) => {
      e.preventDefault();
      login(false);
    });
  }

  // Setup demo button
  const demoBtn = $("#demo-login");
  if (demoBtn) {
    demoBtn.addEventListener("click", (e) => {
      e.preventDefault();
      login(true);
    });
  }

  // Boot application
  boot();
});
