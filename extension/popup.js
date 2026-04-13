let pendingCapture = null;

chrome.storage.local.get(['apiUrl', 'token', 'pendingCapture'], (data) => {
  if (data.apiUrl) document.getElementById('apiUrl').value = data.apiUrl;
  if (data.token) document.getElementById('token').value = data.token;

  if (data.pendingCapture) {
    pendingCapture = data.pendingCapture;
    showCaptureView(pendingCapture);
  }
});
// ── Listen for captures from content.js ─────────────
chrome.runtime.onMessage.addListener((msg) => {
  if (msg.type === 'CAPTURE') {
    pendingCapture = msg.payload;
    chrome.storage.local.set({ pendingCapture: msg.payload });
    showCaptureView(msg.payload);
  }
});

document.getElementById('saveBtn').addEventListener('click', () => {
  const apiUrl = document.getElementById('apiUrl').value.trim();
  const token = document.getElementById('token').value.trim();

  if (!apiUrl || !token) {
    setMsg('saveMsg', 'Both fields are required.', 'error');
    return;
  }

  chrome.storage.local.set({ apiUrl, token }, () => {
    setMsg('saveMsg', 'Saved.', 'success');
  });
});

document.getElementById('sendBtn').addEventListener('click', async () => {
  const btn = document.getElementById('sendBtn');
  btn.disabled = true;
  btn.textContent = 'Sending...';
  setMsg('statusMsg', '', '');

  chrome.storage.local.get(['apiUrl', 'token'], async (data) => {
    if (!data.apiUrl || !data.token) {
      setMsg('statusMsg', 'Set API URL and token in settings first.', 'error');
      btn.disabled = false;
      btn.textContent = 'Send to ReachLog';
      return;
    }

    try {
      const parseRes = await fetch(`${data.apiUrl}/api/parse`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${data.token}`
        },
        body: JSON.stringify({ rawMessage: pendingCapture.rawText })
      });

      if (!parseRes.ok) throw new Error('Parse failed');
      const parsed = await parseRes.json();

      const createRes = await fetch(`${data.apiUrl}/api/outreach`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${data.token}`
        },
        body: JSON.stringify({
          companyName: parsed.companyName ?? pendingCapture.name ?? 'Unknown',
          contactName: parsed.contactName ?? pendingCapture.name ?? '',
          contactEmail: parsed.contactEmail ?? '',
          role: parsed.role ?? '',
          channel: 'LinkedIn',
          rawMessage: pendingCapture.rawText ?? '',
          sentAt: new Date().toISOString(),
          notes: ''
        })
      });

      if (!createRes.ok) {
        const errBody = await createRes.text();
        throw new Error(`Save failed (${createRes.status}): ${errBody}`);
      }

      chrome.storage.local.remove('pendingCapture');
      pendingCapture = null;

      document.getElementById('sendBtn').textContent = '✓ Saved to ReachLog!';
      document.getElementById('sendBtn').style.background = '#3ecf8e';
      document.getElementById('discardBtn').textContent = 'Close';
      setMsg('statusMsg', 'Outreach created successfully.', 'success');

    } catch (err) {
      setMsg('statusMsg', `Error: ${err.message}`, 'error');
      btn.disabled = false;
      btn.textContent = 'Send to ReachLog';
    }
  });
});

document.getElementById('discardBtn').addEventListener('click', () => {
  chrome.storage.local.remove('pendingCapture');
  pendingCapture = null;
  showSettingsView();
});

function showCaptureView(data) {
  document.getElementById('view-settings').classList.add('hidden');
  document.getElementById('view-capture').classList.remove('hidden');
  document.getElementById('captureName').textContent = data.name || 'Unknown contact';
  document.getElementById('capturePreview').textContent = data.rawText;
}

function showSettingsView() {
  document.getElementById('view-capture').classList.add('hidden');
  document.getElementById('view-settings').classList.remove('hidden');
  setMsg('statusMsg', '', '');
  document.getElementById('sendBtn').disabled = false;
  document.getElementById('sendBtn').textContent = 'Send to ReachLog';
}

function setMsg(id, text, type) {
  const el = document.getElementById(id);
  el.textContent = text;
  el.className = `msg ${type}`;
}