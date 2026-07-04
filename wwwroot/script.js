// URL Shortener frontend - vanilla JS, Fetch API only.

const API_BASE = "/api/url";

const form = document.getElementById("shorten-form");
const urlInput = document.getElementById("original-url");
const shortenBtn = document.getElementById("shorten-btn");
const formMessage = document.getElementById("form-message");
const resultCard = document.getElementById("result-card");
const resultLink = document.getElementById("result-link");
const copyBtn = document.getElementById("copy-btn");
const tableBody = document.getElementById("links-table-body");

document.addEventListener("DOMContentLoaded", loadAllUrls);
form.addEventListener("submit", handleShorten);
copyBtn.addEventListener("click", handleCopy);

function isValidUrl(value) {
  try {
    const parsed = new URL(value);
    return parsed.protocol === "http:" || parsed.protocol === "https:";
  } catch {
    return false;
  }
}

async function handleShorten(event) {
  event.preventDefault();

  const originalUrl = urlInput.value.trim();

  if (!isValidUrl(originalUrl)) {
    showFormMessage("Please enter a valid URL.", "error");
    return;
  }

  setLoading(true);
  clearFormMessage();

  try {
    const response = await fetch(API_BASE, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ originalUrl }),
    });

    const data = await response.json().catch(() => null);

    if (!response.ok) {
      const message = data?.message || "Please enter a valid URL.";
      showFormMessage(message, "error");
      return;
    }

    showResult(data.shortUrl);
    showFormMessage("Short URL created successfully!", "success");
    urlInput.value = "";
    prependRow(data);
  } catch (err) {
    showFormMessage("Could not reach the server. Please try again.", "error");
  } finally {
    setLoading(false);
  }
}

function showResult(shortUrl) {
  resultLink.href = shortUrl;
  resultLink.textContent = shortUrl;
  resultCard.classList.remove("hidden");
}

async function handleCopy() {
  const text = resultLink.textContent;
  if (!text) return;

  try {
    await navigator.clipboard.writeText(text);
    const original = copyBtn.textContent;
    copyBtn.textContent = "Copied!";
    setTimeout(() => (copyBtn.textContent = original), 1500);
  } catch {
    showFormMessage("Could not copy to clipboard.", "error");
  }
}

async function loadAllUrls() {
  try {
    const response = await fetch(API_BASE);
    if (!response.ok) throw new Error("Failed to load links");

    const urls = await response.json();
    renderTable(urls);
  } catch (err) {
    tableBody.innerHTML = `<tr><td colspan="5" class="empty-state">Could not load links. Please refresh the page.</td></tr>`;
  }
}

function renderTable(urls) {
  if (!urls || urls.length === 0) {
    tableBody.innerHTML = `<tr><td colspan="5" class="empty-state">No links yet. Shorten your first URL above!</td></tr>`;
    return;
  }

  tableBody.innerHTML = "";
  urls.forEach((url) => tableBody.appendChild(buildRow(url)));
}

function prependRow(url) {
  const emptyRow = tableBody.querySelector(".empty-state");
  if (emptyRow) {
    tableBody.innerHTML = "";
  }
  tableBody.prepend(buildRow(url));
}

function buildRow(url) {
  const row = document.createElement("tr");
  row.dataset.shortCode = url.shortCode;

  const createdDate = new Date(url.createdAt).toLocaleString();

  row.innerHTML = `
    <td class="cell-original" title="${escapeHtml(url.originalUrl)}">${escapeHtml(url.originalUrl)}</td>
    <td class="cell-short"><a href="${escapeHtml(url.shortUrl)}" target="_blank" rel="noopener noreferrer">${escapeHtml(url.shortUrl)}</a></td>
    <td>${createdDate}</td>
    <td>${url.clickCount}</td>
    <td><button class="btn btn-danger" data-action="delete">Delete</button></td>
  `;

  row.querySelector('[data-action="delete"]').addEventListener("click", () => handleDelete(url.shortCode, row));

  return row;
}

async function handleDelete(shortCode, row) {
  const confirmed = confirm("Delete this link? This cannot be undone.");
  if (!confirmed) return;

  try {
    const response = await fetch(`${API_BASE}/${encodeURIComponent(shortCode)}`, {
      method: "DELETE",
    });

    if (!response.ok && response.status !== 204) {
      throw new Error("Delete failed");
    }

    row.remove();

    if (!tableBody.querySelector("tr")) {
      tableBody.innerHTML = `<tr><td colspan="5" class="empty-state">No links yet. Shorten your first URL above!</td></tr>`;
    }
  } catch (err) {
    alert("Could not delete the link. Please try again.");
  }
}

function setLoading(isLoading) {
  shortenBtn.disabled = isLoading;
  shortenBtn.querySelector(".btn-text").textContent = isLoading ? "Shortening..." : "Shorten URL";
}

function showFormMessage(message, type) {
  formMessage.textContent = message;
  formMessage.className = `form-message ${type}`;
}

function clearFormMessage() {
  formMessage.textContent = "";
  formMessage.className = "form-message";
}

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str;
  return div.innerHTML;
}
