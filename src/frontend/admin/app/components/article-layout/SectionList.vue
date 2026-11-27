<script setup lang="ts">
import { VueDraggable } from 'vue-draggable-plus'
import type { SectionInputDto } from '~/types/article'

// Draggable list of nested sections inside a container block (used by the container block's editor in the shared
// layer, layers/shared/app/blocks/container/Editor.vue)
const props = defineProps<{
  sections: SectionInputDto[]
  depth: number
  addLabel: string
}>()

const emit = defineEmits<{
  update: [sections: SectionInputDto[]]
}>()

const sortableSections = computed({
  get: () => props.sections,
  set: (val: SectionInputDto[]) => emit('update', val)
})

function updateSection(index: number, section: SectionInputDto) {
  const sections = [...props.sections]
  sections[index] = section
  emit('update', sections)
}

function removeSection(index: number) {
  emit('update', props.sections.filter((_, i) => i !== index))
}

function addSection() {
  emit('update', [...props.sections, { type: 'full', blocks: [{ type: 'text', content: '' }] }])
}
</script>

<template>
  <div class="space-y-3">
    <VueDraggable
      v-model="sortableSections"
      handle=".section-drag-handle"
      :animation="150"
      ghost-class="sortable-ghost"
      drag-class="sortable-drag"
      chosen-class="sortable-chosen"
      class="space-y-3"
    >
      <ArticleLayoutSectionEditor
        v-for="(section, sIndex) in sections"
        :key="sIndex"
        :section="section"
        :index="sIndex"
        :depth="depth"
        @update="updateSection(sIndex, $event)"
        @remove="removeSection(sIndex)"
      />
    </VueDraggable>

    <button
      type="button"
      class="w-full py-2 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-500 dark:text-gray-400 hover:border-primary-400 hover:text-primary-600 transition-colors"
      @click="addSection"
    >
      {{ addLabel }}
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
