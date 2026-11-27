import type { Component } from 'vue'

// Admin only: every blocks/<type>/Editor.vue, keyed by folder name (= block type). Imported by the admin's
// BlockEditor; the website never imports this file, so editors and their admin components stay out of its bundle.
const editorModules = import.meta.glob<Component>('./*/Editor.vue', { eager: true, import: 'default' })

const editors = new Map(Object.entries(editorModules).map(([path, editor]) => [path.split('/')[1]!, editor]))

export function getBlockEditor(type: string): Component | undefined {
  return editors.get(type)
}
