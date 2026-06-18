const $ = (selector) => document.querySelector(selector);
const $$ = (selector) => document.querySelectorAll(selector);

const loginView = $("#loginView");
const appView = $("#appView");
const loginForm = $("#loginForm");
const registerForm = $("#registerForm");
const loginTab = $("#loginTab");
const registerTab = $("#registerTab");
const authMessage = $("#authMessage");
const regDepartment = $("#regDepartment");
const studentIdLabel = $("#studentIdLabel");
const roleLabel = $("#roleLabel");
const borrowCountLabel = $("#borrowCountLabel");
const signOutButton = $("#signOutButton");
const mainNav = $("#mainNav");
const adminNav = $("#adminNav");
const navLinks = $$(".nav-link");
const panels = {
    issue: $("#issuePanel"),
    return: $("#returnPanel"),
    books: $("#booksPanel"),
    history: $("#historyPanel"),
    fines: $("#finesPanel"),
    recommendations: $("#recommendationsPanel"),
    admin: $("#adminPanel")
};

const issueForm = $("#issueForm");
const returnForm = $("#returnForm");
const issueQrCodeId = $("#issueQrCodeId");
const returnQrCodeId = $("#returnQrCodeId");
const receiptTitle = $("#receiptTitle");
const receiptMessage = $("#receiptMessage");
const receiptTransaction = $("#receiptTransaction");
const receiptIssued = $("#receiptIssued");
const receiptDue = $("#receiptDue");
const returnTitle = $("#returnTitle");
const returnMessage = $("#returnMessage");
const returnTransaction = $("#returnTransaction");
const returnTime = $("#returnTime");
const returnFine = $("#returnFine");

let session = JSON.parse(window.localStorage.getItem("digitalLibrary.session") || "null");
const scanCanvas = document.createElement("canvas");
const scanContext = scanCanvas.getContext("2d", { willReadFrequently: true });

const scanners = {
    issue: {
        video: $("#scannerVideo"),
        placeholder: $("#scannerPlaceholder"),
        state: $("#scannerState"),
        startBtn: $("#startScanButton"),
        stopBtn: $("#stopScanButton"),
        qrInput: issueQrCodeId,
        stream: null,
        timer: null
    },
    return: {
        video: $("#returnScannerVideo"),
        placeholder: $("#returnScannerPlaceholder"),
        state: $("#returnScannerState"),
        startBtn: $("#returnStartScanButton"),
        stopBtn: $("#returnStopScanButton"),
        qrInput: returnQrCodeId,
        stream: null,
        timer: null
    }
};
let activeScanner = "issue";

function formatDate(value) {
    if (!value) return "-";
    return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

function formatMoney(value) {
    return `Rs. ${Number(value || 0).toFixed(2)}`;
}

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

async function apiFetch(url, options = {}) {
    const headers = {
        "Accept": "application/json",
        ...(options.body ? { "Content-Type": "application/json" } : {}),
        ...(session?.accessToken ? { "Authorization": `Bearer ${session.accessToken}` } : {}),
        ...(options.headers || {})
    };

    const response = await fetch(url, { ...options, headers });
    const text = await response.text();
    const data = text ? JSON.parse(text) : null;
    if (!response.ok) {
        throw new Error(data?.message || data?.title || `Request failed (${response.status})`);
    }
    return data;
}

function showAuthMessage(text, type) {
    authMessage.textContent = text;
    authMessage.className = `form-message ${type}`;
    authMessage.classList.remove("hidden");
}

function hideAuthMessage() {
    authMessage.classList.add("hidden");
}

function switchAuthTab(tab) {
    hideAuthMessage();
    loginTab.classList.toggle("active", tab === "login");
    registerTab.classList.toggle("active", tab === "register");
    loginForm.classList.toggle("hidden", tab !== "login");
    registerForm.classList.toggle("hidden", tab !== "register");
}

function setSession(nextSession) {
    session = nextSession;
    if (session) {
        window.localStorage.setItem("digitalLibrary.session", JSON.stringify(session));
    } else {
        window.localStorage.removeItem("digitalLibrary.session");
    }
}

function showApp() {
    studentIdLabel.textContent = `${session.fullName || session.universityId} (${session.universityId})`;
    roleLabel.textContent = session.role || "Student";
    loginView.classList.add("hidden");
    appView.classList.remove("hidden");
    mainNav.classList.remove("hidden");
    signOutButton.classList.remove("hidden");
    adminNav.classList.toggle("hidden", !(session.role === "Admin" || session.role === "Librarian"));
    loadBorrowedBooks();
    loadNotificationsAndFines();
    loadRecommendations();
}

function showPanel(name) {
    Object.entries(panels).forEach(([key, panel]) => panel.classList.toggle("active", key === name));
    navLinks.forEach((link) => link.classList.toggle("active", link.dataset.view === name));
    activeScanner = name === "return" ? "return" : "issue";

    if (name === "books") loadBorrowedBooks();
    if (name === "history") loadHistory();
    if (name === "fines") loadNotificationsAndFines();
    if (name === "recommendations") loadRecommendations();
    if (name === "admin") { loadAdminDashboard(); loadActivityLogs(); }
}

function renderEmpty(container, message) {
    container.innerHTML = `<div class="empty-state"><p class="muted">${escapeHtml(message)}</p></div>`;
}

function setReceipt(title, message, transactionId = "-", issuedAt = null, dueAt = null, type = "") {
    receiptTitle.textContent = title;
    receiptMessage.textContent = message;
    receiptMessage.className = type || "muted";
    receiptTransaction.textContent = transactionId || "-";
    receiptIssued.textContent = formatDate(issuedAt);
    receiptDue.textContent = formatDate(dueAt);
}

function setReturnReceipt(title, message, transactionId = "-", returnedAt = null, fineAmount = 0, type = "") {
    returnTitle.textContent = title;
    returnMessage.textContent = message;
    returnMessage.className = type || "muted";
    returnTransaction.textContent = transactionId || "-";
    returnTime.textContent = formatDate(returnedAt);
    returnFine.textContent = formatMoney(fineAmount);
}

async function loadBorrowedBooks() {
    if (!session) return;
    const booksList = $("#booksList");
    booksList.innerHTML = `<div class="empty-state"><p class="muted">Loading borrowed books...</p></div>`;

    try {
        const data = await apiFetch("/api/students/me/borrowed-books");
        borrowCountLabel.textContent = `${data.activeBorrowCount} / ${data.borrowLimit}`;
        if (!data.books.length) {
            renderEmpty(booksList, "No books are currently borrowed.");
            return;
        }

        booksList.innerHTML = data.books.map((book) => `
            <article class="item-row">
                <div>
                    <h3>${escapeHtml(book.title)}</h3>
                    <div class="meta-grid">
                        <span>${escapeHtml(book.author)}</span>
                        <span>Copy ${escapeHtml(book.accessionNo)}</span>
                        <span>Due ${formatDate(book.dueAt)}</span>
                        <span>${book.isOverdue ? `Estimated fine ${formatMoney(book.estimatedFine)}` : "On schedule"}</span>
                    </div>
                </div>
                <span class="status-badge ${book.isOverdue ? "danger" : ""}">${escapeHtml(book.status)}</span>
            </article>
        `).join("");
    } catch (error) {
        borrowCountLabel.textContent = "- / 3";
        renderEmpty(booksList, error.message);
    }
}

async function loadHistory() {
    const historyList = $("#historyList");
    historyList.innerHTML = `<div class="empty-state"><p class="muted">Loading transaction records...</p></div>`;

    try {
        const data = await apiFetch("/api/students/me/transactions");
        if (!data.transactions.length) {
            renderEmpty(historyList, "No transaction records found.");
            return;
        }

        historyList.innerHTML = data.transactions.map((item) => `
            <article class="item-row">
                <div>
                    <h3>${escapeHtml(item.title)}</h3>
                    <div class="meta-grid">
                        <span>${escapeHtml(item.author)}</span>
                        <span>Copy ${escapeHtml(item.accessionNo)}</span>
                        <span>Issued ${formatDate(item.issuedAt)}</span>
                        <span>Returned ${formatDate(item.returnedAt)}</span>
                        <span>Fine ${formatMoney(item.fineAmount)}</span>
                    </div>
                </div>
                <span class="status-badge ${item.returnedAt ? "closed" : ""}">${escapeHtml(item.status)}</span>
            </article>
        `).join("");
    } catch (error) {
        renderEmpty(historyList, error.message);
    }
}

async function loadNotificationsAndFines() {
    const fineSummary = $("#fineSummary");
    const finesList = $("#finesList");
    const notificationsList = $("#notificationsList");
    fineSummary.innerHTML = "";
    renderEmpty(finesList, "Loading fines...");
    renderEmpty(notificationsList, "Loading notifications...");

    try {
        const [fines, notifications] = await Promise.all([
            apiFetch("/api/students/me/fines"),
            apiFetch("/api/students/me/notifications")
        ]);

        fineSummary.innerHTML = `<div class="metric"><span>Outstanding Balance</span><strong>${formatMoney(fines.outstandingTotal)}</strong></div>`;
        finesList.innerHTML = fines.fines.length ? fines.fines.map((fine) => {
            const isUnpaid = fine.status === "UNPAID";
            return `
            <article class="item-row">
                <div><h3>${escapeHtml(fine.title)}</h3><div class="meta-grid"><span>Copy ${escapeHtml(fine.accessionNo)}</span><span>${formatDate(fine.assessedAt)}</span></div></div>
                <div style="text-align: right;">
                    <span class="status-badge ${isUnpaid ? "danger" : "closed"}">${formatMoney(fine.amount)} ${escapeHtml(fine.status)}</span>
                    ${isUnpaid ? `<br><button class="pay-fine-button" onclick="payFine('${fine.fineId}')">Pay Fine</button>` : ""}
                </div>
            </article>`;
        }).join("") : `<div class="empty-state"><p class="muted">No fines found.</p></div>`;

        notificationsList.innerHTML = notifications.notifications.length ? notifications.notifications.map((note) => `
            <article class="item-row">
                <div><h3>${escapeHtml(note.title)}</h3><p class="muted">${escapeHtml(note.message)}</p><div class="meta-grid"><span>${escapeHtml(note.notificationType)}</span><span>${formatDate(note.createdAt)}</span></div></div>
                <span class="status-badge ${note.isRead ? "closed" : ""}">${note.isRead ? "Read" : "New"}</span>
            </article>
        `).join("") : `<div class="empty-state"><p class="muted">No notifications found.</p></div>`;
    } catch (error) {
        renderEmpty(finesList, error.message);
        renderEmpty(notificationsList, error.message);
    }
}

async function payFine(fineId) {
    try {
        const response = await apiFetch(`/api/students/me/fines/${fineId}/pay`, { method: "POST" });
        alert(response.message || "Fine paid successfully");
        loadNotificationsAndFines();
    } catch (error) {
        alert(error.message || "Failed to pay fine");
    }
}

async function loadRecommendations() {
    const recommendationsList = $("#recommendationsList");
    renderEmpty(recommendationsList, "Loading recommendations...");

    try {
        const data = await apiFetch("/api/students/me/recommendations");
        if (!data.recommendations.length) {
            renderEmpty(recommendationsList, "No recommendations are available yet.");
            return;
        }

        recommendationsList.innerHTML = data.recommendations.map((item) => `
            <article class="item-row">
                <div>
                    <h3>#${item.rank} ${escapeHtml(item.title)}</h3>
                    <p class="muted">${escapeHtml(item.reason)}</p>
                    <div class="meta-grid">
                        <span>${escapeHtml(item.author)}</span>
                        <span>${escapeHtml(item.category || "General")}</span>
                        <span>${item.availabilityCount} available</span>
                    </div>
                </div>
                <span class="status-badge">Score ${item.score}</span>
            </article>
        `).join("");
    } catch (error) {
        renderEmpty(recommendationsList, error.message);
    }
}

async function loadAdminDashboard() {
    const adminSummary = $("#adminSummary");
    const adminAnalytics = $("#adminAnalytics");
    adminSummary.innerHTML = `<div class="empty-state"><p class="muted">Loading summary...</p></div>`;
    adminAnalytics.innerHTML = "";

    try {
        const [summary, analytics] = await Promise.all([
            apiFetch("/api/admin/dashboard/summary"),
            apiFetch("/api/admin/dashboard/analytics")
        ]);

        const metrics = [
            ["Issued", summary.totalIssuedBooks],
            ["Overdue", summary.overdueBooks],
            ["Borrowers", summary.activeBorrowers],
            ["Inventory", summary.activeInventory],
            ["Outstanding", formatMoney(summary.outstandingFines)],
            ["Collected", formatMoney(summary.fineCollectionTotal)]
        ];
        adminSummary.innerHTML = metrics.map(([label, value]) => `<div class="metric"><span>${label}</span><strong>${value}</strong></div>`).join("");

        const groups = [
            ["Active Borrowers", analytics.activeBorrowers],
            ["Fine Reports", analytics.fineReports],
            ["Most Active Students", analytics.mostActiveStudents],
            ["Popular Books (Most Borrowed)", analytics.popularBooks],
            ["Defaulters List", analytics.defaultersList],
            ["Daily Transactions", analytics.dailyTransactions],
            ["Peak Issuing Timings", analytics.peakIssuingTimings],
            ["Fine Trends", analytics.fineTrends],
            ["Department Statistics", analytics.departmentWiseStatistics]
        ];
        adminAnalytics.innerHTML = groups.map(([title, items]) => `
            <section class="analytics-card">
                <h3>${title}</h3>
                ${items.length ? items.map((item) => `<div class="mini-row"><span>${escapeHtml(item.label)}</span><strong>${escapeHtml(item.value)}</strong><small>${escapeHtml(item.detail)}</small></div>`).join("") : `<p class="muted">No data yet.</p>`}
            </section>
        `).join("");
    } catch (error) {
        renderEmpty(adminSummary, error.message);
    }
}

async function loadActivityLogs() {
    const logsSummary = $("#logsSummary");
    const logsList = $("#logsList");
    logsSummary.innerHTML = "";
    renderEmpty(logsList, "Loading activity logs...");

    try {
        const data = await apiFetch("/api/admin/dashboard/logs?limit=50");

        logsSummary.innerHTML = `
            <div class="metric"><span>Total Log Entries</span><strong>${data.totalCount}</strong></div>
            <div class="metric"><span>Showing</span><strong>${data.logs.length}</strong></div>
        `;

        if (!data.logs.length) {
            renderEmpty(logsList, "No activity logs recorded yet.");
            return;
        }

        logsList.innerHTML = data.logs.map((log) => {
            const isFail = log.actionStatus === "FAILURE" || log.actionStatus === "DENIED";
            return `
            <article class="item-row">
                <div>
                    <h3>${escapeHtml(log.actionType)}</h3>
                    <div class="meta-grid">
                        <span>${escapeHtml(log.actorUniversityId || "System")}</span>
                        <span>${escapeHtml(log.actorRole || "-")}</span>
                        <span>${escapeHtml(log.endpoint || "-")}</span>
                        <span>${formatDate(log.createdAt)}</span>
                        ${log.failureReason ? `<span class="danger-text">${escapeHtml(log.failureReason)}</span>` : ""}
                    </div>
                </div>
                <span class="status-badge ${isFail ? "danger" : ""}">${escapeHtml(log.actionStatus)}</span>
            </article>`;
        }).join("");
    } catch (error) {
        renderEmpty(logsList, error.message);
    }
}

async function startScanner(panelName) {
    const sc = scanners[panelName || activeScanner];
    if (!sc || !("mediaDevices" in navigator) || !navigator.mediaDevices.getUserMedia || typeof jsQR === "undefined") {
        if (sc) sc.state.textContent = "Manual entry";
        return;
    }

    try {
        sc.stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "environment" }, audio: false });
        sc.video.srcObject = sc.stream;
        await sc.video.play();
        sc.placeholder.classList.add("hidden");
        sc.startBtn.classList.add("hidden");
        sc.stopBtn.classList.remove("hidden");
        sc.state.textContent = "Scanning";
        sc.timer = window.setInterval(() => detectQrCode(panelName || activeScanner), 450);
    } catch {
        stopScanner(panelName || activeScanner);
        sc.state.textContent = "Manual entry";
    }
}

function detectQrCode(panelName) {
    const sc = scanners[panelName];
    if (!sc || sc.video.readyState !== sc.video.HAVE_ENOUGH_DATA || !sc.video.videoWidth) return;
    scanCanvas.width = sc.video.videoWidth;
    scanCanvas.height = sc.video.videoHeight;
    scanContext.drawImage(sc.video, 0, 0, scanCanvas.width, scanCanvas.height);
    const imageData = scanContext.getImageData(0, 0, scanCanvas.width, scanCanvas.height);
    const code = jsQR(imageData.data, imageData.width, imageData.height, { inversionAttempts: "dontInvert" });
    if (!code) return;
    sc.qrInput.value = code.data.trim();
    sc.state.textContent = "QR detected";
    stopScanner(panelName);
}

function stopScanner(panelName) {
    const sc = scanners[panelName || activeScanner];
    if (!sc) return;
    if (sc.timer) window.clearInterval(sc.timer);
    sc.timer = null;
    if (sc.stream) sc.stream.getTracks().forEach((track) => track.stop());
    sc.stream = null;
    sc.video.srcObject = null;
    sc.placeholder.classList.remove("hidden");
    sc.startBtn.classList.remove("hidden");
    sc.stopBtn.classList.add("hidden");
}

function stopAllScanners() {
    stopScanner("issue");
    stopScanner("return");
}

async function issueBook(event) {
    event.preventDefault();
    const qr = issueQrCodeId.value.trim();
    if (!qr) return;
    const submitButton = issueForm.querySelector("button[type='submit']");
    submitButton.disabled = true;
    submitButton.textContent = "Issuing...";
    setReceipt("Issuing book", "Please wait while the transaction is created.", "-", null, null, "muted");

    try {
        const data = await apiFetch("/api/transactions/issue", {
            method: "POST",
            body: JSON.stringify({ universityId: session.universityId, qrCodeId: qr })
        });
        setReceipt("Book issued", "The book has been issued to your account.", data.transactionId, data.issuedAt, data.dueAt, "success");
        issueQrCodeId.value = "";
        await Promise.all([loadBorrowedBooks(), loadNotificationsAndFines(), loadRecommendations()]);
    } catch (error) {
        setReceipt("Issue failed", error.message, "-", null, null, "failure");
    } finally {
        submitButton.disabled = false;
        submitButton.textContent = "Issue Book";
    }
}

async function returnBook(event) {
    event.preventDefault();
    const qr = returnQrCodeId.value.trim();
    if (!qr) return;
    const submitButton = returnForm.querySelector("button[type='submit']");
    submitButton.disabled = true;
    submitButton.textContent = "Returning...";
    setReturnReceipt("Returning book", "Please wait while the return is posted.", "-", null, 0, "muted");

    try {
        const data = await apiFetch("/api/transactions/return", {
            method: "POST",
            body: JSON.stringify({ qrCodeId: qr })
        });
        setReturnReceipt("Book returned", data.message, data.transactionId, data.returnedAt, data.fineAmount, data.fineAmount > 0 ? "failure" : "success");
        returnQrCodeId.value = "";
        await Promise.all([loadBorrowedBooks(), loadHistory(), loadNotificationsAndFines(), loadRecommendations()]);
    } catch (error) {
        setReturnReceipt("Return failed", error.message, "-", null, 0, "failure");
    } finally {
        submitButton.disabled = false;
        submitButton.textContent = "Return Book";
    }
}

async function fetchDepartments() {
    try {
        const departments = await apiFetch("/api/departments");
        regDepartment.innerHTML = departments.length
            ? '<option value="">Select department</option>' + departments.map((d) => `<option value="${escapeHtml(d.departmentCode)}">${escapeHtml(d.departmentName)}</option>`).join("")
            : '<option value="">No departments found</option>';
    } catch {
        regDepartment.innerHTML = '<option value="">Could not load departments</option>';
    }
}

loginTab.addEventListener("click", () => switchAuthTab("login"));
registerTab.addEventListener("click", () => switchAuthTab("register"));
navLinks.forEach((link) => link.addEventListener("click", () => showPanel(link.dataset.view)));
scanners.issue.startBtn.addEventListener("click", () => startScanner("issue"));
scanners.issue.stopBtn.addEventListener("click", () => { stopScanner("issue"); scanners.issue.state.textContent = "Ready"; });
scanners.return.startBtn.addEventListener("click", () => startScanner("return"));
scanners.return.stopBtn.addEventListener("click", () => { stopScanner("return"); scanners.return.state.textContent = "Ready"; });
issueForm.addEventListener("submit", issueBook);
returnForm.addEventListener("submit", returnBook);
$("#refreshBooksButton").addEventListener("click", loadBorrowedBooks);
$("#refreshHistoryButton").addEventListener("click", loadHistory);
$("#refreshFinesButton").addEventListener("click", loadNotificationsAndFines);
$("#refreshRecommendationsButton").addEventListener("click", loadRecommendations);
$("#refreshAdminButton").addEventListener("click", () => { loadAdminDashboard(); loadActivityLogs(); });
$("#refreshLogsButton").addEventListener("click", loadActivityLogs);

$("#roleManagementForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const targetId = $("#roleTargetId").value.trim();
    const newRole = $("#roleTargetSelect").value;
    const msg = $("#roleUpdateMessage");
    const btn = event.target.querySelector("button");

    if (!targetId) return;

    btn.disabled = true;
    msg.className = "muted";
    msg.textContent = "Updating...";
    msg.classList.remove("hidden");

    try {
        const response = await apiFetch("/api/admin/dashboard/roles", {
            method: "PUT",
            body: JSON.stringify({ universityId: targetId, newRole })
        });
        msg.className = "status-badge closed";
        msg.textContent = response.message || "Role updated successfully.";
        loadActivityLogs(); // refresh logs to show the new event
    } catch (error) {
        msg.className = "danger-text";
        msg.textContent = error.message || "Failed to update role.";
    } finally {
        btn.disabled = false;
    }
});

loginForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    hideAuthMessage();
    const universityId = $("#loginUniversityId").value.trim();
    const password = $("#loginPassword").value;
    const button = loginForm.querySelector("button[type='submit']");
    button.disabled = true;
    button.textContent = "Logging in...";

    try {
        const data = await apiFetch("/api/auth/login", {
            method: "POST",
            body: JSON.stringify({ universityId, password })
        });
        setSession(data);
        $("#loginPassword").value = "";
        showApp();
    } catch (error) {
        showAuthMessage(error.message, "failure");
    } finally {
        button.disabled = false;
        button.textContent = "Login";
    }
});

registerForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    hideAuthMessage();
    const body = {
        universityId: $("#regUniversityId").value.trim(),
        fullName: $("#regFullName").value.trim(),
        email: $("#regEmail").value.trim(),
        password: $("#regPassword").value,
        departmentCode: regDepartment.value,
        semester: parseInt($("#regSemester").value, 10)
    };
    const button = registerForm.querySelector("button[type='submit']");
    button.disabled = true;
    button.textContent = "Registering...";

    try {
        const data = await apiFetch("/api/auth/register", {
            method: "POST",
            body: JSON.stringify(body)
        });
        if (!data.success) throw new Error(data.message || "Registration failed.");
        registerForm.reset();
        switchAuthTab("login");
        showAuthMessage("Registration successful. You can now login.", "success");
    } catch (error) {
        showAuthMessage(error.message, "failure");
    } finally {
        button.disabled = false;
        button.textContent = "Register";
    }
});

signOutButton.addEventListener("click", () => {
    stopAllScanners();
    setSession(null);
    appView.classList.add("hidden");
    loginView.classList.remove("hidden");
    mainNav.classList.add("hidden");
    signOutButton.classList.add("hidden");
    hideAuthMessage();
});

fetchDepartments();
if (session?.accessToken) {
    showApp();
} else {
    loginView.classList.remove("hidden");
}
