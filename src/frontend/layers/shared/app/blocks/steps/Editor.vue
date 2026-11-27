<script setup lang="ts">
import type { BlockInputDto } from '../../types/layout'
import type { EditorProps } from '../types'

const props = defineProps<EditorProps>()
const emit = defineEmits<{ update: [block: BlockInputDto] }>()

function updateStepsHeading(val: string) {
  emit('update', { ...props.block, heading: val || undefined })
}
function updateStepItem(index: number, field: string, val: string) {
  const items = [...(props.block.items || [])]
  items[index] = { ...items[index], [field]: val || undefined }
  emit('update', { ...props.block, items })
}
function addStepItem() {
  const items = [...(props.block.items || [])]
  const nextNum = String(items.length + 1).padStart(2, '0')
  items.push({ number: nextNum, title: '', description: '' })
  emit('update', { ...props.block, items })
}
function removeStepItem(index: number) {
  const items = (props.block.items || []).filter((_, i) => i !== index)
  emit('update', { ...props.block, items })
}
</script>

<template>
  <div class="space-y-4">
    <EditorLinkableField
      :model-value="block.heading || ''"
      label="Section Heading"
      placeholder="e.g. Vores tilgang"
      @update:model-value="updateStepsHeading"
    />
    <div class="space-y-3">
      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Steps</label>
      <div
        v-for="(item, i) in (block.items || [])"
        :key="i"
        class="border border-gray-200 dark:border-gray-700 rounded-lg p-3 space-y-2 bg-gray-50 dark:bg-gray-700"
      >
        <div class="flex items-center justify-between">
          <span class="text-xs text-gray-500 dark:text-gray-400 font-medium">Step {{ i + 1 }}</span>
          <button type="button" class="text-gray-400 hover:text-red-500 transition-colors" @click="removeStepItem(i)">
            <UiIcon name="x-mark" class="w-4 h-4" />
          </button>
        </div>
        <UiInput
          :model-value="item.number || ''"
          label="Number"
          placeholder="01"
          @update:model-value="(val: string) => updateStepItem(i, 'number', val)"
        />
        <EditorLinkableField
          :model-value="item.title || ''"
          label="Title"
          placeholder="Step title"
          @update:model-value="(val: string) => updateStepItem(i, 'title', val)"
        />
        <EditorLinkableField
          :model-value="item.description || ''"
          label="Description"
          placeholder="Step description"
          multiline
          :rows="2"
          @update:model-value="(val: string) => updateStepItem(i, 'description', val)"
        />
      </div>
      <button
        type="button"
        class="w-full py-2 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-500 dark:text-gray-400 hover:border-primary-400 hover:text-primary-600 transition-colors"
        @click="addStepItem"
      >
        + Add Step
      </button>
    </div>
  </div>
</template>
