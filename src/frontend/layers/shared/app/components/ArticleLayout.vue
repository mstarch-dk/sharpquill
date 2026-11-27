<script setup lang="ts">
import type { RenderedLayoutDto } from '../types/layout'
import { getBlockRenderer } from '../blocks/registry'

// Renders a layout composed by the API: sections in order, each block by its registered renderer
// (blocks/<type>/Render.vue). Blocks of an unknown type render nothing, so a layout from a newer version or a
// hand-edited one never breaks the page. Used by the website and by the admin preview (`preview`).
const props = withDefaults(defineProps<{
  layout: RenderedLayoutDto
  preview?: boolean
}>(), { preview: false })

if (import.meta.dev) {
  for (const section of props.layout.sections) {
    for (const block of section.blocks) {
      if (!getBlockRenderer(block.type)) console.warn(`[ArticleLayout] Skipping block of unknown type "${block.type}"`)
    }
  }
}

function sectionGridClass(type: string): string {
  switch (type) {
    case 'two-column':
      return 'grid grid-cols-1 md:grid-cols-2 gap-8'
    case 'three-column':
      return 'grid grid-cols-1 md:grid-cols-3 gap-8'
    default:
      return ''
  }
}

function sectionStyleClass(style?: string): string {
  switch (style) {
    case 'tinted':
      return 'bg-primary-50 dark:bg-primary-950/30 rounded-2xl px-8 py-10'
    case 'dark':
      return 'bg-gray-900 dark:bg-gray-950 text-white rounded-2xl px-8 py-10'
    case 'card':
      return 'bg-white dark:bg-gray-800 shadow-lg dark:shadow-gray-900/40 rounded-2xl px-8 py-10 border border-gray-100 dark:border-gray-700'
    case 'gradient':
      return 'bg-gradient-to-br from-primary-50 to-primary-100 dark:from-primary-950/40 dark:to-primary-900/20 rounded-2xl px-8 py-10'
    case 'accent':
      return 'bg-primary-600 dark:bg-primary-700 text-white rounded-2xl px-8 py-10'
    default:
      return ''
  }
}
</script>

<template>
  <div class="space-y-12">
    <section
      v-for="(section, sIndex) in layout.sections"
      :key="sIndex"
      :class="[sectionGridClass(section.type), sectionStyleClass(section.style)]"
    >
      <div
        v-for="(block, bIndex) in section.blocks"
        :key="bIndex"
      >
        <component
          :is="getBlockRenderer(block.type)"
          v-if="getBlockRenderer(block.type)"
          :block="block"
          :preview="preview"
        />
      </div>
    </section>
  </div>
</template>
