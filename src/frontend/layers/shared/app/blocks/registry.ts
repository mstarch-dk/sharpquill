import type { Component } from 'vue'
import type { BlockDefinition } from './types'

// Every blocks/<type>/index.ts and Render.vue, keyed by folder name. Adding a folder adds the block; nothing to
// register by hand. Editors are in editors.ts, so the website never bundles them.
const definitionModules = import.meta.glob<BlockDefinition>('./*/index.ts', { eager: true, import: 'default' })
const rendererModules = import.meta.glob<Component>('./*/Render.vue', { eager: true, import: 'default' })

function folderName(path: string): string {
  return path.split('/')[1]!
}

function byFolder<T>(modules: Record<string, T>): Map<string, T> {
  return new Map(Object.entries(modules).map(([path, value]) => [folderName(path), value]))
}

const definitions = byFolder(definitionModules)
const renderers = byFolder(rendererModules)

// Sorted for the admin type picker
export const blockDefinitions: BlockDefinition[] = [...definitions.values()].sort((a, b) => a.order - b.order)

// Folder names next to the type each definition declares; a unit test checks they match
export const blockFolders: { folder: string, type: string }[] =
  [...definitions.entries()].map(([folder, definition]) => ({ folder, type: definition.type }))

export function getBlockDefinition(type: string): BlockDefinition | undefined {
  return definitions.get(type)
}

// Undefined for an unknown type (e.g. a layout written by a newer version): the layout skips that block
export function getBlockRenderer(type: string): Component | undefined {
  return renderers.get(type)
}
