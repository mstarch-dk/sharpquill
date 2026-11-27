import type { InjectionKey } from 'vue'
import type { ApiResponse } from '../types/api'
import type { BlockInputDto, RenderedBlockDto } from '../types/layout'

// One block type of the layout editor (docs/frontend-uis.md, "Adding a block"). Each lives in its own folder
// blocks/<type>/ with three files, found by file name (registry.ts, editors.ts):
//   index.ts   - this definition (default export, via defineBlock)
//   Render.vue - renders the API's RenderedBlockDto on the website and in the admin preview (props: RenderProps)
//   Editor.vue - edits the stored BlockInputDto in the admin (props: EditorProps, emits `update`)
export interface BlockDefinition {
  // Stored in the layout JSON. Must equal the folder name and an entry in the backend's LayoutBlockTypes.
  type: string
  // Shown in the admin type picker
  label: string
  // Position in the admin type picker (ascending)
  order: number
  // The block the editor inserts when an author switches a block to this type
  create: () => BlockInputDto
  // Holds nested sections: offered only while the nesting limit allows another level
  nestsSections?: boolean
}

export function defineBlock(definition: BlockDefinition): BlockDefinition {
  return definition
}

export interface RenderProps {
  block: RenderedBlockDto
  // Admin preview: embeds show a placeholder instead of the live component
  preview?: boolean
}

export interface EditorProps {
  block: BlockInputDto
  // Nesting level of the section the block is in (0 = top level)
  depth: number
}

// Admin services an editor may need. Provided by the admin layout editor, so the editors in this layer do not
// depend on admin composables (the website never loads editors, but type-checks the layer).
export interface BlockEditorServices {
  post: <T>(endpoint: string, body?: Record<string, unknown>) => Promise<ApiResponse<T>>
}

export const blockEditorServicesKey: InjectionKey<BlockEditorServices> = Symbol('blockEditorServices')

// Limit of nested section levels; the backend enforces the same (LayoutBlockTypes.MaxNestingDepth)
export const maxNestingDepth = 3
