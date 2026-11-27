<script setup lang="ts">
import { VueDraggable } from 'vue-draggable-plus'
import type { SectionInputDto, BlockInputDto } from '~/types/article'

interface Props {
  section: SectionInputDto
  index: number
  depth: number
}

const props = defineProps<Props>()

const emit = defineEmits<{
  update: [section: SectionInputDto]
  remove: []
}>()

const sectionTypeOptions = [
  { value: 'full', label: 'Full Width' },
  { value: 'two-column', label: 'Two Columns' },
  { value: 'three-column', label: 'Three Columns' }
]

const sectionStyleOptions = [
  { value: '', label: 'No background' },
  { value: 'tinted', label: 'Tinted (theme color)' },
  { value: 'dark', label: 'Dark' },
  { value: 'card', label: 'Card (white/shadow)' },
  { value: 'gradient', label: 'Gradient' },
  { value: 'accent', label: 'Accent (bold color)' }
]

const sectionType = computed({
  get: () => props.section.type,
  set: (val: 'full' | 'two-column' | 'three-column') => {
    emit('update', { ...props.section, type: val })
  }
})

const sectionStyle = computed({
  get: () => props.section.style ?? '',
  set: (val: string) => {
    emit('update', { ...props.section, style: val || undefined })
  }
})

const blocks = computed({
  get: () => props.section.blocks,
  set: (val: BlockInputDto[]) => {
    emit('update', { ...props.section, blocks: val })
  }
})

function updateBlock(blockIndex: number, block: BlockInputDto) {
  const updated = [...props.section.blocks]
  updated[blockIndex] = block
  emit('update', { ...props.section, blocks: updated })
}

function removeBlock(blockIndex: number) {
  const updated = props.section.blocks.filter((_, i) => i !== blockIndex)
  emit('update', { ...props.section, blocks: updated })
}

function addBlock() {
  const updated = [...props.section.blocks, { type: 'text' as const, content: '' }]
  emit('update', { ...props.section, blocks: updated })
}
</script>

<template>
  <div class="border border-gray-300 dark:border-gray-600 rounded-lg bg-gray-50 dark:bg-gray-700">
    <!-- Section header -->
    <div class="flex items-center justify-between px-4 py-3 border-b border-gray-300 dark:border-gray-600 bg-gray-100 dark:bg-gray-700 rounded-t-lg">
      <div class="flex items-center gap-3">
        <!-- Drag handle -->
        <div class="section-drag-handle cursor-grab active:cursor-grabbing p-1 -ml-1 text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-300" title="Drag to reorder">
          <UiIcon name="drag-handle" class="w-4 h-4" />
        </div>
        <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Section {{ index + 1 }}</span>
        <UiSelect
          v-model="sectionType"
          :options="sectionTypeOptions"
          class="w-40"
        />
        <UiSelect
          v-model="sectionStyle"
          :options="sectionStyleOptions"
          class="w-48"
        />
      </div>
      <div class="flex items-center gap-1">
        <button
          type="button"
          class="p-1 text-gray-400 dark:text-gray-500 hover:text-red-500 dark:hover:text-red-400 transition-colors"
          title="Remove section"
          @click="$emit('remove')"
        >
          <UiIcon name="trash" class="w-4 h-4" />
        </button>
      </div>
    </div>

    <!-- Blocks -->
    <div class="p-4 space-y-3">
      <div
        v-if="section.blocks.length === 0"
        class="text-center text-sm text-gray-400 dark:text-gray-500 py-4"
      >
        No blocks yet. Add one below.
      </div>

      <VueDraggable
        v-model="blocks"
        group="blocks"
        handle=".block-drag-handle"
        :animation="150"
        ghost-class="sortable-ghost"
        drag-class="sortable-drag"
        chosen-class="sortable-chosen"
        :class="{
          'grid gap-3': true,
          'grid-cols-1': section.type === 'full',
          'grid-cols-1 md:grid-cols-2': section.type === 'two-column',
          'grid-cols-1 md:grid-cols-3': section.type === 'three-column'
        }"
      >
        <ArticleLayoutBlockEditor
          v-for="(block, blockIndex) in section.blocks"
          :key="blockIndex"
          :block="block"
          :index="blockIndex"
          :depth="depth"
          @update="updateBlock(blockIndex, $event)"
          @remove="removeBlock(blockIndex)"
        />
      </VueDraggable>

      <button
        type="button"
        class="w-full py-2 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-500 dark:text-gray-400 hover:border-primary-400 hover:text-primary-600 transition-colors"
        @click="addBlock"
      >
        + Add Block
      </button>
    </div>
  </div>
</template>

<style scoped>
:deep(.sortable-ghost) {
  opacity: 0.4;
  border: 2px dashed rgb(var(--color-primary-400));
  border-radius: 0.5rem;
}

:deep(.sortable-drag) {
  box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.2);
  border-radius: 0.5rem;
}

:deep(.sortable-chosen) {
  opacity: 0.9;
}
</style>
