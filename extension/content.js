const BUTTON_CLASS = 'rl-capture-btn';

function extractMessageData() {
  const nameEl =
    document.querySelector('.msg-s-message-list__participant-names') ||
    document.querySelector('.msg-thread__participant-names') ||
    document.querySelector('.msg-entity-lockup__entity-title') ||
    document.querySelector('.presence-entity__name') ||
    document.querySelector('h2.msg-entity-lockup__entity-title');
  const name = nameEl?.innerText?.trim() ?? '';

  const headlineEl =
    document.querySelector('.msg-entity-lockup__subtitle span') ||
    document.querySelector('.msg-entity-lockup__subtitle');
  const headline = headlineEl?.innerText?.trim() ?? '';

  const messages = document.querySelectorAll(
    '.msg-s-event-listitem__body, .msg-s-message-list__event .body'
  );
  const lastMessage = messages.length
    ? messages[messages.length - 1].innerText?.trim()
    : '';

  const rawText = [
    name ? `From: ${name}` : '',
    headline ? `Role/Title: ${headline}` : '',
    lastMessage ? `Message:\n${lastMessage}` : ''
  ].filter(Boolean).join('\n\n');

  return { name, headline, rawText };
}

function createButton() {
  const btn = document.createElement('button');
  btn.className = BUTTON_CLASS;
  btn.textContent = '+ ReachLog';
  btn.style.cssText = `
    position: fixed;
    bottom: 80px;
    right: 20px;
    background: #5b6cf9;
    color: white;
    border: none;
    border-radius: 8px;
    padding: 8px 16px;
    font-size: 13px;
    font-weight: 600;
    cursor: pointer;
    z-index: 99999;
    font-family: sans-serif;
    letter-spacing: 0.03em;
    box-shadow: 0 4px 12px rgba(91,108,249,0.4);
  `;

  btn.addEventListener('mouseenter', () => btn.style.background = '#6b7dff');
  btn.addEventListener('mouseleave', () => btn.style.background = '#5b6cf9');

  btn.addEventListener('click', () => {
    const data = extractMessageData();

    chrome.storage.local.set({ pendingCapture: data }, () => {
      btn.textContent = '✓ Captured — open extension to send';
      btn.style.background = '#3ecf8e';
      btn.style.padding = '8px 14px';
    });
  });

  return btn;
}

function injectButton() {
  if (document.querySelector(`.${BUTTON_CLASS}`)) return;
  const btn = createButton();
  document.body.appendChild(btn);
}

function removeButton() {
  document.querySelector(`.${BUTTON_CLASS}`)?.remove();
}

function handleUrlChange() {
  if (window.location.href.includes('/messaging/thread/')) {
    removeButton();
    setTimeout(injectButton, 1000);
  } else {
    removeButton();
  }
}

handleUrlChange();

let lastUrl = location.href;
const observer = new MutationObserver(() => {
  if (location.href !== lastUrl) {
    lastUrl = location.href;
    handleUrlChange();
  }
});
observer.observe(document.body, { childList: true, subtree: true });