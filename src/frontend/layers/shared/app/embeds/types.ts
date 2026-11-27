import type { Component } from 'vue'

// A component authors can place with a "component" block (docs/frontend-uis.md, "Adding a block"). Each lives in
// embeds/<name>/index.ts (default export, via defineEmbed) next to its component.
export interface EmbedDefinition {
  // Stored as the block's componentName. Must equal the folder name and an entry in the backend's
  // LayoutBlockTypes.ComponentNames.
  name: string
  // Shown in the admin component picker and in the admin preview placeholder
  label: string
  // Loaded only where it is rendered (the website), so the admin never bundles it
  component: () => Promise<Component | { default: Component }>
}

export function defineEmbed(definition: EmbedDefinition): EmbedDefinition {
  return definition
}
