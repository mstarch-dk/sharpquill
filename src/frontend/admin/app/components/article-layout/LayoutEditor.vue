<script setup lang="ts">
import { VueDraggable } from 'vue-draggable-plus'
import { blockEditorServicesKey } from '#layer/blocks/types'
import type { LayoutInputDto, SectionInputDto } from '~/types/article'

interface Props {
  modelValue: LayoutInputDto
}

const props = defineProps<Props>()

const emit = defineEmits<{
  'update:modelValue': [value: LayoutInputDto]
}>()

// Services the block editors of the shared layer use (layers/shared/app/blocks/types.ts)
const { post } = useApi()
provide(blockEditorServicesKey, { post })

const sections = computed({
  get: () => props.modelValue.sections,
  set: (val: SectionInputDto[]) => {
    emit('update:modelValue', { sections: val })
  }
})

function updateSection(index: number, section: SectionInputDto) {
  const updated = [...props.modelValue.sections]
  updated[index] = section
  emit('update:modelValue', { sections: updated })
}

function removeSection(index: number) {
  const updated = props.modelValue.sections.filter((_, i) => i !== index)
  emit('update:modelValue', { sections: updated })
}

function addSection() {
  const updated = [
    ...props.modelValue.sections,
    { type: 'full' as const, blocks: [{ type: 'text' as const, content: '' }] }
  ]
  emit('update:modelValue', { sections: updated })
}
</script>

<template>
  <div class="space-y-4">
    <div class="flex items-center justify-between">
      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Layout Sections</label>
      <span class="text-xs text-gray-500 dark:text-gray-400">{{ modelValue.sections.length }} section(s)</span>
    </div>

    <div v-if="modelValue.sections.length === 0" class="text-center text-sm text-gray-400 dark:text-gray-500 py-8 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg">
      No sections yet. Add one to start building the layout.
    </div>

    <VueDraggable
      v-model="sections"
      handle=".section-drag-handle"
      :animation="150"
      ghost-class="sortable-ghost"
      drag-class="sortable-drag"
      chosen-class="sortable-chosen"
      class="space-y-4"
    >
      <ArticleLayoutSectionEditor
        v-for="(section, index) in modelValue.sections"
        :key="index"
        :section="section"
        :index="index"
        :depth="0"
        @update="updateSection(index, $event)"
        @remove="removeSection(index)"
      />
    </VueDraggable>

    <button
      type="button"
      class="w-full py-3 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg text-sm font-medium text-gray-500 dark:text-gray-400 hover:border-primary-400 hover:text-primary-600 transition-colors"
      @click="addSection"
    >
      + Add Section
    </button>
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
