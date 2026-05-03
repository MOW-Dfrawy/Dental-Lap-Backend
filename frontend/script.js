/* =====================================================================
 * Dental Lab Workflow — Frontend
 * Connects to: http://localhost:5000  (ASP.NET REST API)
 * Pure HTML / CSS / JavaScript — no frameworks.
 * =================================================================== */

const API_BASE = "http://localhost:5000";

/* ---------- Status enum (matches backend Domain.Enums.CaseStatus) ---------- */
const CASE_STATUSES = [
  "New",
  "InProgress",
  "OnHold",
  "QualityCheck",
  "ReadyForDelivery",
  "Delivered",
  "Cancelled",
];

const STATUS_LABELS = {
  New: "New / Received",
  InProgress: "In Progress",
  OnHold: "On Hold",
  QualityCheck: "Quality Check",
  ReadyForDelivery: "Ready for Delivery",
  Delivered: "Delivered",
  Cancelled: "Cancelled",
};

/* ---------- Shared state ---------- */
const state = {
  role: localStorage.getItem("dl_role") || "Admin",
  cases: [],
  clinics: [],
  patients: [],
  technicians: [],
  stages: [],
  selectedCaseId: null,
  selectedCaseHistory: [],
};

/* =====================================================================
 * API helper
 * =================================================================== */
async function api(path, options = {}) {
  const headers = {
    "Content-Type": "application/json",
    "X-Role": state.role,
    ...(options.headers || {}),
  };
  const res = await fetch(`${API_BASE}${path}`, { ...options, headers });

  if (res.status === 204) return null;

  let body = null;
  const text = await res.text();
  if (text) {
    try { body = JSON.parse(text); } catch { body = text; }
  }

  if (!res.ok) {
    let msg = `${res.status} ${res.statusText}`;
    if (body && typeof body === "object") {
      if (body.message)  msg = body.message;
      else if (body.title) msg = body.title;
      else if (body.errors) {
        msg = Object.values(body.errors).flat().join(", ");
      }
    } else if (typeof body === "string" && body) msg = body;
    const err = new Error(msg);
    err.status = res.status;
    err.body = body;
    throw err;
  }
  return body;
}

/* =====================================================================
 * Toasts
 * =================================================================== */
function toast(message, kind = "info", duration = 3500) {
  const container = document.getElementById("toasts");
  const el = document.createElement("div");
  el.className = `toast ${kind}`;
  el.textContent = message;
  container.appendChild(el);
  setTimeout(() => {
    el.style.opacity = "0";
    el.style.transition = "opacity 0.25s";
    setTimeout(() => el.remove(), 250);
  }, duration);
}

/* =====================================================================
 * Utilities
 * =================================================================== */
function fmtDate(value) {
  if (!value) return "—";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return "—";
  return d.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "2-digit" });
}
function fmtDateTime(value) {
  if (!value) return "—";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return "—";
  return d.toLocaleString(undefined, {
    year: "numeric", month: "short", day: "2-digit",
    hour: "2-digit", minute: "2-digit",
  });
}
function fmtMoney(value) {
  if (value == null) return "—";
  const n = Number(value);
  if (Number.isNaN(n)) return "—";
  return n.toLocaleString(undefined, { style: "currency", currency: "USD" });
}
function escapeHtml(str) {
  if (str == null) return "";
  return String(str)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}
function statusBadge(status) {
  const cls = `badge badge-${String(status).toLowerCase()}`;
  return `<span class="${cls}">${escapeHtml(STATUS_LABELS[status] || status || "—")}</span>`;
}

/* Tolerantly read paged or list responses */
function listFrom(payload) {
  if (!payload) return [];
  if (Array.isArray(payload)) return payload;
  if (Array.isArray(payload.items)) return payload.items;
  if (Array.isArray(payload.data))  return payload.data;
  return [];
}

/* =====================================================================
 * Navigation
 * =================================================================== */
const PAGE_LOADERS = {
  dashboard: loadDashboard,
  cases: loadCasesPage,
  workflow: loadWorkflowPage,
  clinics: loadClinicsPage,
  activity: loadActivityPage,
};

function navigate(page) {
  document.querySelectorAll(".sidebar a").forEach((a) => {
    a.classList.toggle("active", a.dataset.page === page);
  });
  document.querySelectorAll(".page").forEach((p) => {
    p.classList.toggle("active", p.id === `page-${page}`);
  });
  if (PAGE_LOADERS[page]) PAGE_LOADERS[page]();
}

/* =====================================================================
 * Health check
 * =================================================================== */
async function checkApi() {
  const el = document.getElementById("apiStatus");
  el.className = "api-status pending";
  el.textContent = "Checking API…";
  try {
    await api("/api/health");
    el.className = "api-status ok";
    el.textContent = "API online";
  } catch (e) {
    try {
      await api("/api/clinics?page=1&pageSize=1");
      el.className = "api-status ok";
      el.textContent = "API online";
    } catch (e2) {
      el.className = "api-status bad";
      el.textContent = "API offline";
    }
  }
}

/* =====================================================================
 * Reference data (clinics, patients, technicians, stages)
 * =================================================================== */
async function loadReferenceData() {
  const tasks = [
    api("/api/clinics?page=1&pageSize=200").then((d) => (state.clinics = listFrom(d))).catch(() => {}),
    api("/api/patients?page=1&pageSize=500").then((d) => (state.patients = listFrom(d))).catch(() => {}),
    api("/api/technicians").then((d) => (state.technicians = listFrom(d))).catch(() => {}),
    api("/api/workflow-stages").then((d) => (state.stages = listFrom(d))).catch(() => {}),
  ];
  await Promise.all(tasks);
}

/* =====================================================================
 * Dashboard
 * =================================================================== */
async function loadDashboard() {
  try {
    const data = await api("/api/cases?page=1&pageSize=500");
    const cases = listFrom(data);
    state.cases = cases;

    document.getElementById("statTotal").textContent = data?.totalCount ?? cases.length;

    const counts = {};
    CASE_STATUSES.forEach((s) => (counts[s] = 0));
    cases.forEach((c) => { if (counts[c.statusName] != null) counts[c.statusName]++; });

    document.getElementById("statNew").textContent           = counts.New || 0;
    document.getElementById("statInProgress").textContent    = counts.InProgress || 0;
    document.getElementById("statQualityCheck").textContent  = counts.QualityCheck || 0;
    document.getElementById("statReady").textContent         = counts.ReadyForDelivery || 0;
    document.getElementById("statDelivered").textContent     = counts.Delivered || 0;

    // Status breakdown bars
    const totalForBar = Math.max(1, cases.length);
    const breakdown = document.getElementById("statusBreakdown");
    breakdown.innerHTML = CASE_STATUSES.map((s) => {
      const n = counts[s] || 0;
      const pct = Math.round((n / totalForBar) * 100);
      return `
        <div class="status-row">
          <span class="label">${escapeHtml(STATUS_LABELS[s])}</span>
          <span class="bar"><span style="width:${pct}%"></span></span>
          <span class="count">${n}</span>
        </div>
      `;
    }).join("");
  } catch (e) {
    toast("Could not load dashboard: " + e.message, "error");
    document.getElementById("statTotal").textContent = "—";
  }

  // Recent activity (best-effort: try logs endpoint, fall back to derived activity)
  const list = document.getElementById("recentActivity");
  list.innerHTML = '<li class="empty">Loading…</li>';
  const logs = await fetchActivityLogs(8);
  if (logs.length === 0) {
    list.innerHTML = '<li class="empty">No recent activity.</li>';
  } else {
    list.innerHTML = logs
      .map(
        (l) => `
        <li>
          <div>${escapeHtml(l.text)}</div>
          <span class="meta">${escapeHtml(l.when)}${l.who ? " · " + escapeHtml(l.who) : ""}</span>
        </li>
      `
      )
      .join("");
  }
}

/* =====================================================================
 * Activity logs
 *  - Try /api/activitylogs (and a couple of common variants)
 *  - Fall back to deriving recent activity from cases & their stage history
 * =================================================================== */
async function fetchActivityLogs(limit) {
  const candidates = [
    "/api/activitylogs",
    "/api/activity-logs",
    "/api/activity",
    "/api/logs",
  ];
  for (const path of candidates) {
    try {
      const data = await api(path);
      const items = listFrom(data);
      if (items.length === 0) continue;
      return items.slice(0, limit || items.length).map((row) => ({
        text:
          row.message ||
          row.action ||
          row.description ||
          row.entityName ||
          JSON.stringify(row),
        when: fmtDateTime(row.timestamp || row.createdAt || row.date),
        who: row.userName || row.user || row.performedBy || "",
        raw: row,
      }));
    } catch {
      /* try next */
    }
  }
  // Fallback: derive activity from current cases
  return deriveActivityFromCases(limit);
}

function deriveActivityFromCases(limit = 10) {
  const events = [];
  state.cases.forEach((c) => {
    events.push({
      text: `Case "${c.title}" (${c.caseNumber}) — status ${STATUS_LABELS[c.statusName] || c.statusName}`,
      whenRaw: c.completedAt || c.startedAt || c.receivedDate,
      when: fmtDateTime(c.completedAt || c.startedAt || c.receivedDate),
      who: c.assignedTechnicianName || c.clinicName || "",
    });
  });
  events.sort((a, b) => new Date(b.whenRaw || 0) - new Date(a.whenRaw || 0));
  return events.slice(0, limit);
}

async function loadActivityPage() {
  const list = document.getElementById("activityList");
  list.innerHTML = '<li class="empty">Loading…</li>';
  // Make sure cases exist for the fallback
  if (state.cases.length === 0) {
    try {
      const data = await api("/api/cases?page=1&pageSize=200");
      state.cases = listFrom(data);
    } catch { /* ignore */ }
  }
  const logs = await fetchActivityLogs(100);
  if (!logs.length) {
    list.innerHTML = '<li class="empty">No activity found.</li>';
    return;
  }
  list.innerHTML = logs
    .map(
      (l) => `
      <li>
        <div>${escapeHtml(l.text)}</div>
        <span class="meta">${escapeHtml(l.when)}${l.who ? " · " + escapeHtml(l.who) : ""}</span>
      </li>
    `
    )
    .join("");
}

/* =====================================================================
 * Cases page
 * =================================================================== */
async function loadCasesPage() {
  // Populate status filter once
  const filterSel = document.getElementById("caseStatusFilter");
  if (filterSel.options.length <= 1) {
    CASE_STATUSES.forEach((s) => {
      const o = document.createElement("option");
      o.value = s;
      o.textContent = STATUS_LABELS[s];
      filterSel.appendChild(o);
    });
  }

  await loadReferenceData();
  await fetchCasesIntoTable();
}

async function fetchCasesIntoTable() {
  const tbody = document.querySelector("#casesTable tbody");
  tbody.innerHTML = '<tr><td class="empty" colspan="10">Loading…</td></tr>';

  const search = document.getElementById("caseSearch").value.trim();
  const status = document.getElementById("caseStatusFilter").value;
  const params = new URLSearchParams({ page: "1", pageSize: "200" });
  if (search) params.set("search", search);
  if (status) params.set("status", status);

  try {
    const data = await api(`/api/cases?${params.toString()}`);
    const cases = listFrom(data);
    state.cases = cases;
    if (!cases.length) {
      tbody.innerHTML = '<tr><td class="empty" colspan="10">No cases found.</td></tr>';
      return;
    }
    tbody.innerHTML = cases
      .map(
        (c, i) => `
        <tr>
          <td>${i + 1}</td>
          <td><strong>${escapeHtml(c.caseNumber)}</strong></td>
          <td>${escapeHtml(c.title)}</td>
          <td>${escapeHtml(c.clinicName || "—")}</td>
          <td>${escapeHtml(c.patientName || "—")}</td>
          <td>${statusBadge(c.statusName)}</td>
          <td>${escapeHtml(c.currentStageName || "—")}</td>
          <td>${fmtDate(c.dueDate)}</td>
          <td>${fmtMoney(c.price)}</td>
          <td>
            <button class="btn btn-secondary btn-sm" data-action="view"   data-id="${c.id}">View</button>
            <button class="btn btn-secondary btn-sm" data-action="edit"   data-id="${c.id}">Edit</button>
            <button class="btn btn-danger btn-sm"    data-action="delete" data-id="${c.id}">Delete</button>
          </td>
        </tr>
      `
      )
      .join("");
  } catch (e) {
    tbody.innerHTML = `<tr><td class="empty" colspan="10">Error loading cases: ${escapeHtml(e.message)}</td></tr>`;
  }
}

/* ---------- Case modal ---------- */
function openCaseModal(caseObj = null) {
  const modal = document.getElementById("caseModal");
  document.getElementById("caseModalTitle").textContent = caseObj ? "Edit Case" : "New Case";
  document.getElementById("caseId").value = caseObj?.id ?? "";
  document.getElementById("caseTitle").value = caseObj?.title ?? "";
  document.getElementById("caseDescription").value = caseObj?.description ?? "";
  document.getElementById("caseDueDate").value = caseObj?.dueDate ? caseObj.dueDate.substring(0, 10) : "";
  document.getElementById("casePrice").value = caseObj?.price ?? 0;
  document.getElementById("casePriority").value = caseObj?.priority ?? "";
  document.getElementById("caseShade").value = caseObj?.toothShade ?? "";
  document.getElementById("caseNotes").value = caseObj?.notes ?? "";

  // Clinic
  const clinicSel = document.getElementById("caseClinic");
  clinicSel.innerHTML = state.clinics
    .map((c) => `<option value="${c.id}">${escapeHtml(c.name)}</option>`)
    .join("");
  if (caseObj?.clinicId) clinicSel.value = caseObj.clinicId;
  clinicSel.disabled = !!caseObj; // backend update DTO does not include clinic

  // Patient
  const patientSel = document.getElementById("casePatient");
  const refreshPatients = () => {
    const cId = Number(clinicSel.value);
    const list = state.patients.filter((p) => !cId || p.clinicId === cId);
    patientSel.innerHTML = list.length
      ? list.map((p) => `<option value="${p.id}">${escapeHtml(p.fullName)}</option>`).join("")
      : '<option value="">No patients for this clinic</option>';
    if (caseObj?.patientId) patientSel.value = caseObj.patientId;
  };
  refreshPatients();
  clinicSel.onchange = refreshPatients;
  patientSel.disabled = !!caseObj;

  // Technician
  const techSel = document.getElementById("caseTechnician");
  techSel.innerHTML =
    '<option value="">Unassigned</option>' +
    state.technicians.map((t) => `<option value="${t.id}">${escapeHtml(t.fullName || t.name || "Tech #" + t.id)}</option>`).join("");
  if (caseObj?.assignedTechnicianId) techSel.value = caseObj.assignedTechnicianId;

  // Status (only used on edit; create uses default)
  const statusSel = document.getElementById("caseStatus");
  statusSel.innerHTML = CASE_STATUSES.map((s) => `<option value="${s}">${STATUS_LABELS[s]}</option>`).join("");
  statusSel.value = caseObj?.statusName || "New";
  statusSel.disabled = !caseObj;

  modal.classList.add("open");
  modal.setAttribute("aria-hidden", "false");
}
function closeModal(modal) {
  modal.classList.remove("open");
  modal.setAttribute("aria-hidden", "true");
}

async function submitCaseForm(e) {
  e.preventDefault();
  const id = document.getElementById("caseId").value;
  const title = document.getElementById("caseTitle").value.trim();
  const clinicId = Number(document.getElementById("caseClinic").value);
  const patientId = Number(document.getElementById("casePatient").value);
  if (!title) return toast("Title is required", "error");
  if (!clinicId) return toast("Clinic is required", "error");
  if (!patientId) return toast("Patient is required", "error");

  const technicianRaw = document.getElementById("caseTechnician").value;
  const dueRaw = document.getElementById("caseDueDate").value;

  const payload = {
    title,
    description: document.getElementById("caseDescription").value || null,
    assignedTechnicianId: technicianRaw ? Number(technicianRaw) : null,
    dueDate: dueRaw ? new Date(dueRaw).toISOString() : null,
    price: Number(document.getElementById("casePrice").value || 0),
    priority: document.getElementById("casePriority").value || null,
    toothShade: document.getElementById("caseShade").value || null,
    notes: document.getElementById("caseNotes").value || null,
  };

  try {
    if (id) {
      payload.status = document.getElementById("caseStatus").value;
      await api(`/api/cases/${id}`, { method: "PUT", body: JSON.stringify(payload) });
      toast("Case updated", "success");
    } else {
      payload.clinicId = clinicId;
      payload.patientId = patientId;
      await api("/api/cases", { method: "POST", body: JSON.stringify(payload) });
      toast("Case created", "success");
    }
    closeModal(document.getElementById("caseModal"));
    fetchCasesIntoTable();
  } catch (err) {
    toast("Save failed: " + err.message, "error");
  }
}

async function deleteCase(id) {
  if (!confirm("Delete this case? This cannot be undone.")) return;
  try {
    await api(`/api/cases/${id}`, { method: "DELETE" });
    toast("Case deleted", "success");
    fetchCasesIntoTable();
  } catch (err) {
    toast("Delete failed: " + err.message, "error");
  }
}

async function viewCase(id) {
  try {
    const c = await api(`/api/cases/${id}`);
    const body = document.getElementById("detailsBody");
    document.getElementById("detailsTitle").textContent = `${c.caseNumber} — ${c.title}`;
    body.innerHTML = `
      <div class="detail-grid">
        <div class="detail-item"><span class="label">Clinic</span><span class="value">${escapeHtml(c.clinicName || "—")}</span></div>
        <div class="detail-item"><span class="label">Patient</span><span class="value">${escapeHtml(c.patientName || "—")}</span></div>
        <div class="detail-item"><span class="label">Status</span><span class="value">${statusBadge(c.statusName)}</span></div>
        <div class="detail-item"><span class="label">Current Stage</span><span class="value">${escapeHtml(c.currentStageName || "—")}</span></div>
        <div class="detail-item"><span class="label">Technician</span><span class="value">${escapeHtml(c.assignedTechnicianName || "Unassigned")}</span></div>
        <div class="detail-item"><span class="label">Priority</span><span class="value">${escapeHtml(c.priority || "—")}</span></div>
        <div class="detail-item"><span class="label">Tooth Shade</span><span class="value">${escapeHtml(c.toothShade || "—")}</span></div>
        <div class="detail-item"><span class="label">Price</span><span class="value">${fmtMoney(c.price)}</span></div>
        <div class="detail-item"><span class="label">Received</span><span class="value">${fmtDate(c.receivedDate)}</span></div>
        <div class="detail-item"><span class="label">Due</span><span class="value">${fmtDate(c.dueDate)}</span></div>
        <div class="detail-item full"><span class="label">Description</span><span class="value">${escapeHtml(c.description || "—")}</span></div>
        <div class="detail-item full"><span class="label">Notes</span><span class="value">${escapeHtml(c.notes || "—")}</span></div>
      </div>
    `;
    const modal = document.getElementById("detailsModal");
    modal.classList.add("open");
    modal.setAttribute("aria-hidden", "false");
  } catch (err) {
    toast("Could not load case: " + err.message, "error");
  }
}

/* =====================================================================
 * Workflow page
 * =================================================================== */
async function loadWorkflowPage() {
  await loadReferenceData();
  if (state.cases.length === 0) {
    try {
      const data = await api("/api/cases?page=1&pageSize=200");
      state.cases = listFrom(data);
    } catch { /* ignore */ }
  }
  const sel = document.getElementById("workflowCaseSelect");
  sel.innerHTML =
    '<option value="">Select a case…</option>' +
    state.cases
      .map((c) => `<option value="${c.id}">${escapeHtml(c.caseNumber)} — ${escapeHtml(c.title)}</option>`)
      .join("");
  if (state.selectedCaseId) {
    sel.value = String(state.selectedCaseId);
    await renderWorkflowFor(state.selectedCaseId);
  } else {
    document.getElementById("workflowCurrent").textContent = "No case selected.";
    document.getElementById("stageButtons").innerHTML = "";
    document.getElementById("historyList").innerHTML = '<li class="empty">No history loaded.</li>';
  }
}

async function renderWorkflowFor(caseId) {
  state.selectedCaseId = caseId;
  const current = document.getElementById("workflowCurrent");
  const buttonsEl = document.getElementById("stageButtons");
  const historyEl = document.getElementById("historyList");

  current.textContent = "Loading…";
  buttonsEl.innerHTML = "";
  historyEl.innerHTML = '<li class="empty">Loading…</li>';

  try {
    const c = await api(`/api/cases/${caseId}`);
    current.textContent = `Current stage: ${c.currentStageName || "Not started"}  ·  Status: ${STATUS_LABELS[c.statusName] || c.statusName}`;

    if (state.stages.length === 0) {
      buttonsEl.innerHTML = '<span class="empty">No workflow stages defined on the server.</span>';
    } else {
      const sortedStages = [...state.stages].sort((a, b) => (a.order ?? 0) - (b.order ?? 0));
      buttonsEl.innerHTML = sortedStages
        .map((s) => {
          const isCurrent = c.currentStageId === s.id;
          return `<button data-stage-id="${s.id}" class="${isCurrent ? "current" : ""}" ${isCurrent ? "disabled" : ""}>
            ${escapeHtml(s.name)}
          </button>`;
        })
        .join("");
    }
  } catch (e) {
    current.textContent = "Could not load case.";
    toast("Failed to load case: " + e.message, "error");
  }

  try {
    const history = await api(`/api/cases/${caseId}/history`);
    state.selectedCaseHistory = listFrom(history);
    if (state.selectedCaseHistory.length === 0) {
      historyEl.innerHTML = '<li class="empty">No history yet.</li>';
    } else {
      historyEl.innerHTML = state.selectedCaseHistory
        .map(
          (h) => `
          <li>
            <strong>${escapeHtml(h.stageName || "Stage #" + h.stageId)}</strong>
            <span class="meta">
              Entered ${fmtDateTime(h.enteredAt)}
              ${h.exitedAt ? " · Exited " + fmtDateTime(h.exitedAt) : " · in progress"}
              ${h.technicianName ? " · " + escapeHtml(h.technicianName) : ""}
              ${h.durationMinutes != null ? " · " + h.durationMinutes + " min" : ""}
            </span>
            ${h.notes ? `<div>${escapeHtml(h.notes)}</div>` : ""}
          </li>
        `
        )
        .join("");
    }
  } catch (e) {
    historyEl.innerHTML = `<li class="empty">Could not load history: ${escapeHtml(e.message)}</li>`;
  }
}

async function moveCaseToStage(stageId) {
  if (!state.selectedCaseId) return;
  const notes = document.getElementById("stageNotes").value || null;
  try {
    await api(`/api/cases/${state.selectedCaseId}/move-stage`, {
      method: "POST",
      body: JSON.stringify({ stageId: Number(stageId), notes }),
    });
    document.getElementById("stageNotes").value = "";
    toast("Case moved to new stage", "success");
    await renderWorkflowFor(state.selectedCaseId);
  } catch (e) {
    toast("Move failed: " + e.message, "error");
  }
}

/* =====================================================================
 * Clinics page
 * =================================================================== */
async function loadClinicsPage() {
  resetClinicForm();
  await refreshClinicsTable();
}

async function refreshClinicsTable() {
  const tbody = document.querySelector("#clinicsTable tbody");
  tbody.innerHTML = '<tr><td class="empty" colspan="6">Loading…</td></tr>';
  try {
    const data = await api("/api/clinics?page=1&pageSize=200");
    const clinics = listFrom(data);
    state.clinics = clinics;
    if (!clinics.length) {
      tbody.innerHTML = '<tr><td class="empty" colspan="6">No clinics yet.</td></tr>';
      return;
    }
    tbody.innerHTML = clinics
      .map(
        (c) => `
        <tr>
          <td><strong>${escapeHtml(c.name)}</strong></td>
          <td>${escapeHtml(c.contactPerson || "—")}</td>
          <td>${escapeHtml(c.phone || "—")}</td>
          <td>${escapeHtml(c.city || "—")}</td>
          <td><span class="badge ${c.isActive ? "badge-active" : "badge-inactive"}">${c.isActive ? "Active" : "Inactive"}</span></td>
          <td>
            <button class="btn btn-secondary btn-sm" data-action="clinic-edit"   data-id="${c.id}">Edit</button>
            <button class="btn btn-danger btn-sm"    data-action="clinic-delete" data-id="${c.id}">Delete</button>
          </td>
        </tr>
      `
      )
      .join("");
  } catch (e) {
    tbody.innerHTML = `<tr><td class="empty" colspan="6">Error loading clinics: ${escapeHtml(e.message)}</td></tr>`;
  }
}

function resetClinicForm() {
  document.getElementById("clinicId").value = "";
  document.getElementById("clinicName").value = "";
  document.getElementById("clinicContact").value = "";
  document.getElementById("clinicPhone").value = "";
  document.getElementById("clinicEmail").value = "";
  document.getElementById("clinicCity").value = "";
  document.getElementById("clinicAddress").value = "";
  document.getElementById("clinicNotes").value = "";
  document.getElementById("clinicActive").checked = true;
  document.getElementById("clinicActiveWrap").style.display = "none";
  document.getElementById("clinicFormTitle").textContent = "Create Clinic";
}

function fillClinicForm(c) {
  document.getElementById("clinicId").value = c.id;
  document.getElementById("clinicName").value = c.name || "";
  document.getElementById("clinicContact").value = c.contactPerson || "";
  document.getElementById("clinicPhone").value = c.phone || "";
  document.getElementById("clinicEmail").value = c.email || "";
  document.getElementById("clinicCity").value = c.city || "";
  document.getElementById("clinicAddress").value = c.address || "";
  document.getElementById("clinicNotes").value = c.notes || "";
  document.getElementById("clinicActive").checked = c.isActive !== false;
  document.getElementById("clinicActiveWrap").style.display = "flex";
  document.getElementById("clinicFormTitle").textContent = "Edit Clinic";
}

async function submitClinicForm(e) {
  e.preventDefault();
  const id = document.getElementById("clinicId").value;
  const name = document.getElementById("clinicName").value.trim();
  if (!name) return toast("Clinic name is required", "error");
  const email = document.getElementById("clinicEmail").value.trim();
  if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    return toast("Email looks invalid", "error");
  }

  const payload = {
    name,
    contactPerson: document.getElementById("clinicContact").value || null,
    phone: document.getElementById("clinicPhone").value || null,
    email: email || null,
    city: document.getElementById("clinicCity").value || null,
    address: document.getElementById("clinicAddress").value || null,
    notes: document.getElementById("clinicNotes").value || null,
  };

  try {
    if (id) {
      payload.isActive = document.getElementById("clinicActive").checked;
      await api(`/api/clinics/${id}`, { method: "PUT", body: JSON.stringify(payload) });
      toast("Clinic updated", "success");
    } else {
      await api("/api/clinics", { method: "POST", body: JSON.stringify(payload) });
      toast("Clinic created", "success");
    }
    resetClinicForm();
    refreshClinicsTable();
  } catch (err) {
    toast("Save failed: " + err.message, "error");
  }
}

async function deleteClinic(id) {
  if (!confirm("Delete this clinic? This cannot be undone.")) return;
  try {
    await api(`/api/clinics/${id}`, { method: "DELETE" });
    toast("Clinic deleted", "success");
    refreshClinicsTable();
  } catch (err) {
    toast("Delete failed: " + err.message, "error");
  }
}

/* =====================================================================
 * Event wiring
 * =================================================================== */
function wireEvents() {
  // Sidebar nav
  document.querySelectorAll(".sidebar a").forEach((a) => {
    a.addEventListener("click", (e) => {
      e.preventDefault();
      navigate(a.dataset.page);
    });
  });

  // Role picker
  const roleSel = document.getElementById("roleSelect");
  roleSel.value = state.role;
  roleSel.addEventListener("change", () => {
    state.role = roleSel.value;
    localStorage.setItem("dl_role", state.role);
    toast(`Role set to ${state.role}`, "info", 1800);
    checkApi();
    const active = document.querySelector(".sidebar a.active")?.dataset.page || "dashboard";
    navigate(active);
  });

  // Dashboard refresh
  document.getElementById("refreshDashboard").addEventListener("click", loadDashboard);

  // Cases
  document.getElementById("refreshCases").addEventListener("click", fetchCasesIntoTable);
  document.getElementById("newCaseBtn").addEventListener("click", async () => {
    await loadReferenceData();
    if (state.clinics.length === 0) return toast("Create a clinic before adding a case.", "error");
    if (state.patients.length === 0) return toast("Create a patient before adding a case.", "error");
    openCaseModal(null);
  });
  document.getElementById("caseSearch").addEventListener("input", debounce(fetchCasesIntoTable, 350));
  document.getElementById("caseStatusFilter").addEventListener("change", fetchCasesIntoTable);

  document.querySelector("#casesTable tbody").addEventListener("click", async (e) => {
    const btn = e.target.closest("button[data-action]");
    if (!btn) return;
    const id = Number(btn.dataset.id);
    const action = btn.dataset.action;
    if (action === "view") return viewCase(id);
    if (action === "edit") {
      try {
        const c = await api(`/api/cases/${id}`);
        await loadReferenceData();
        openCaseModal(c);
      } catch (err) {
        toast("Could not load case: " + err.message, "error");
      }
    }
    if (action === "delete") return deleteCase(id);
  });

  // Case form
  document.getElementById("caseForm").addEventListener("submit", submitCaseForm);

  // Modal close buttons
  document.querySelectorAll(".modal").forEach((m) => {
    m.addEventListener("click", (e) => {
      if (e.target === m || e.target.matches("[data-close]")) closeModal(m);
    });
  });
  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") {
      document.querySelectorAll(".modal.open").forEach(closeModal);
    }
  });

  // Workflow
  document.getElementById("refreshWorkflow").addEventListener("click", loadWorkflowPage);
  document.getElementById("workflowCaseSelect").addEventListener("change", (e) => {
    const v = e.target.value;
    if (v) renderWorkflowFor(Number(v));
  });
  document.getElementById("stageButtons").addEventListener("click", (e) => {
    const btn = e.target.closest("button[data-stage-id]");
    if (btn && !btn.disabled) moveCaseToStage(btn.dataset.stageId);
  });

  // Clinics
  document.getElementById("refreshClinics").addEventListener("click", refreshClinicsTable);
  document.getElementById("newClinicBtn").addEventListener("click", () => {
    resetClinicForm();
    document.getElementById("clinicName").focus();
  });
  document.getElementById("clinicForm").addEventListener("submit", submitClinicForm);
  document.getElementById("clinicCancelBtn").addEventListener("click", resetClinicForm);
  document.querySelector("#clinicsTable tbody").addEventListener("click", async (e) => {
    const btn = e.target.closest("button[data-action]");
    if (!btn) return;
    const id = Number(btn.dataset.id);
    if (btn.dataset.action === "clinic-edit") {
      try {
        const c = await api(`/api/clinics/${id}`);
        fillClinicForm(c);
      } catch (err) {
        toast("Could not load clinic: " + err.message, "error");
      }
    } else if (btn.dataset.action === "clinic-delete") {
      deleteClinic(id);
    }
  });

  // Activity
  document.getElementById("refreshActivity").addEventListener("click", loadActivityPage);
}

function debounce(fn, ms) {
  let t;
  return (...args) => {
    clearTimeout(t);
    t = setTimeout(() => fn(...args), ms);
  };
}

/* =====================================================================
 * Boot
 * =================================================================== */
document.addEventListener("DOMContentLoaded", async () => {
  wireEvents();
  await checkApi();
  navigate("dashboard");
});
