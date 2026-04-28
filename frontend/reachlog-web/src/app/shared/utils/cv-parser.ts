export function parseCvToTiptapDoc(text: string): object {
  const rawLines = text.split('\n');
  const content: object[] = [];
  let firstNonEmptyDone = false;
  let secondNonEmptyDone = false;

  for (const rawLine of rawLines) {
    const line = rawLine.trim();

    if (!line) continue;

    if (!firstNonEmptyDone) {
      content.push({
        type: 'heading',
        attrs: { level: 1 },
        content: [{ type: 'text', text: line }]
      });
      firstNonEmptyDone = true;
      continue;
    }

    if (!secondNonEmptyDone) {
      content.push({
        type: 'paragraph',
        content: [{ type: 'text', text: line }]
      });
      secondNonEmptyDone = true;
      continue;
    }

    if (isAllCapsHeading(line)) {
      content.push({
        type: 'heading',
        attrs: { level: 2 },
        content: [{ type: 'text', text: line }]
      });
    } else {
      content.push({
        type: 'paragraph',
        content: [{ type: 'text', text: line }]
      });
    }
  }

  if (content.length === 0) {
    content.push({ type: 'paragraph', content: [] });
  }

  return { type: 'doc', content };
}

function isAllCapsHeading(line: string): boolean {
  const stripped = line.replace(/[\s\-\/&|,]/g, '');
  if (stripped.length < 3) return false;
  return stripped === stripped.toUpperCase() && /[A-Z]/.test(stripped);
}
