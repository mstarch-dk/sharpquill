<script setup lang="ts">
import type { BlockInputDto } from '../../types/layout'
import { isFeatureIcon } from '../../utils/featureIcons'
import type { EditorProps } from '../types'

const props = defineProps<EditorProps>()
const emit = defineEmits<{ update: [block: BlockInputDto] }>()

function updateCardsHeading(val: string) {
  emit('update', { ...props.block, heading: val || undefined })
}
function updateCardsContent(val: string) {
  emit('update', { ...props.block, content: val || undefined })
}
function updateCardItem(index: number, field: string, val: string) {
  const items = [...(props.block.items || [])]
  items[index] = { ...items[index], [field]: val || undefined }
  emit('update', { ...props.block, items })
}
function addCardItem() {
  const items = [...(props.block.items || []), { icon: '', title: '', description: '' }]
  emit('update', { ...props.block, items })
}
function removeCardItem(index: number) {
  const items = (props.block.items || []).filter((_, i) => i !== index)
  emit('update', { ...props.block, items })
}
</script>

<template>
  <div class="space-y-4">
    <EditorLinkableField
      :model-value="block.heading || ''"
      label="Section Heading"
      placeholder="e.g. Hvad vi gør"
      @update:model-value="updateCardsHeading"
    />
    <EditorLinkableField
      :model-value="block.content || ''"
      label="Intro Text (optional)"
      placeholder="Optional intro paragraph..."
      multiline
      :rows="2"
      @update:model-value="updateCardsContent"
    />
    <div class="space-y-3">
      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Cards</label>
      <div
        v-for="(item, i) in (block.items || [])"
        :key="i"
        class="border border-gray-200 dark:border-gray-700 rounded-lg p-3 space-y-2 bg-gray-50 dark:bg-gray-700"
      >
        <div class="flex items-center justify-between">
          <span class="text-xs text-gray-500 dark:text-gray-400 font-medium">Card {{ i + 1 }}</span>
          <button type="button" class="text-gray-400 hover:text-red-500 transition-colors" @click="removeCardItem(i)">
            <UiIcon name="x-mark" class="w-4 h-4" />
          </button>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Icon</label>
          <div class="flex flex-wrap items-center gap-2">
            <UiIconPicker
              :model-value="item.icon || ''"
              @update:model-value="(val: string) => updateCardItem(i, 'icon', val)"
            />
            <span class="text-xs text-gray-500 dark:text-gray-400">or</span>
            <UiEmojiPicker
              :model-value="isFeatureIcon(item.icon) ? '' : (item.icon || '')"
              @update:model-value="(val: string) => updateCardItem(i, 'icon', val)"
            />
          </div>
        </div>
        <EditorLinkableField
          :model-value="item.title || ''"
          label="Title"
          placeholder="Card title"
          @update:model-value="(val: string) => updateCardItem(i, 'title', val)"
        />
        <EditorLinkableField
          :model-value="item.description || ''"
          label="Description"
          placeholder="Card description"
          multiline
          :rows="2"
          @update:model-value="(val: string) => updateCardItem(i, 'description', val)"
        />
      </div>
      <button
        type="button"
        class="w-full py-2 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg text-sm text-gray-500 dark:text-gray-400 hover:border-primary-400 hover:text-primary-600 transition-colors"
        @click="addCardItem"
      >
        + Add Card
      </button>
    </div>
  </div>
</template>
