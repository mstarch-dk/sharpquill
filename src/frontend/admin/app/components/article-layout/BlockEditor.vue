<script setup lang="ts">
import { blockDefinitions, getBlockDefinition } from '#layer/blocks/registry'
import { getBlockEditor } from '#layer/blocks/editors'
import { maxNestingDepth } from '#layer/blocks/types'
import type { BlockInputDto } from '~/types/article'

// One block in the layout editor: the type picker and the block's own editor from the shared block registry
// (layers/shared/app/blocks/<type>/Editor.vue). Adding a block type needs no change here.
interface Props {
  block: BlockInputDto
  index: number
  depth: number
}

const props = defineProps<Props>()

const emit = defineEmits<{
  update: [block: BlockInputDto]
  remove: []
}>()

const canNestContainer = computed(() => props.depth + 1 < maxNestingDepth)

const blockType = computed({
  get: () => props.block.type,
  set: (val: string) => {
    const definition = getBlockDefinition(val)
    if (definition) emit('update', definition.create())
  }
})

const blockTypeOptions = computed(() => blockDefinitions
  .filter(definition => !definition.nestsSections || canNestContainer.value)
  .map(definition => ({ value: definition.type, label: definition.label })))

const editor = computed(() => getBlockEditor(props.block.type))
</script>

<template>
  <div class="border border-gray-200 dark:border-gray-700 rounded-lg p-4 bg-white dark:bg-gray-800">
    <div class="flex items-center justify-between mb-3">
      <div class="flex items-center gap-3">
        <!-- Drag handle -->
        <div class="block-drag-handle cursor-grab active:cursor-grabbing p-1 -ml-1 text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-300" title="Drag to reorder">
          <UiIcon name="drag-handle" class="w-4 h-4" />
        </div>
        <span class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Block {{ index + 1 }}</span>
        <UiSelect
          v-model="blockType"
          :options="blockTypeOptions"
          class="w-32"
        />
      </div>
      <button
        type="button"
        class="text-gray-400 dark:text-gray-500 hover:text-red-500 dark:hover:text-red-400 transition-colors"
        title="Remove block"
        @click="$emit('remove')"
      >
        <UiIcon name="x-mark" class="w-4 h-4" />
      </button>
    </div>

    <component
      :is="editor"
      v-if="editor"
      :block="block"
      :depth="depth"
      @update="(updated: BlockInputDto) => emit('update', updated)"
    />
    <p v-else class="text-sm text-red-600 dark:text-red-400">
      Unknown block type "{{ block.type }}". Pick another type or remove the block.
    </p>
  </div>
</template>
