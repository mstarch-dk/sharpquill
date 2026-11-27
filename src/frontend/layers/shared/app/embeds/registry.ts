import { defineAsyncComponent, type Component } from 'vue'
import type { EmbedDefinition } from './types'

// Every embeds/<name>/index.ts, keyed by folder name. Adding a folder adds the embed.
const definitionModules = import.meta.glob<EmbedDefinition>('./*/index.ts', { eager: true, import: 'default' })

const definitions = new Map(Object.entries(definitionModules).map(([path, definition]) => [path.split('/')[1]!, definition]))
const components = new Map<string, Component>()

export const embedDefinitions: EmbedDefinition[] =
  [...definitions.values()].sort((a, b) => a.label.localeCompare(b.label))

export const embedFolders: { folder: string, name: string }[] =
  [...definitions.entries()].map(([folder, definition]) => ({ folder, name: definition.name }))

export function getEmbedDefinition(name: string | undefined): EmbedDefinition | undefined {
  return name ? definitions.get(name) : undefined
}

// Undefined for an unknown name: the layout skips the block
export function getEmbedComponent(name: string | undefined): Component | undefined {
  const definition = getEmbedDefinition(name)
  if (!definition) return undefined
  let component = components.get(definition.name)
  if (!component) {
    component = defineAsyncComponent(definition.component)
    components.set(definition.name, component)
  }
  return component
}
