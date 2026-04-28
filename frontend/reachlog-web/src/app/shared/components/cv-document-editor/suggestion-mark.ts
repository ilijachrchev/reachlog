import { Mark, mergeAttributes } from '@tiptap/core';

export const SuggestionMark = Mark.create({
  name: 'suggestion',

  addAttributes() {
    return {
      id: { default: null },
      type: { default: null },
      status: { default: 'pending' },
    };
  },

  parseHTML() {
    return [{ tag: 'span[data-suggestion-id]' }];
  },

  renderHTML({ HTMLAttributes }) {
    const { id, type, status } = HTMLAttributes as { id: string; type: string; status: string };
    return [
      'span',
      mergeAttributes(HTMLAttributes, {
        'data-suggestion-id': id,
        class: `suggestion-mark suggestion-${type} suggestion-${status}`,
      }),
      0,
    ];
  },
});
