<script setup lang="ts">
import type { BlockInputDto } from '../../types/layout'
import type { EditorProps } from '../types'

const props = defineProps<EditorProps>()
const emit = defineEmits<{ update: [block: BlockInputDto] }>()

// The fields of the admin's MediaDto this editor uses (the layer cannot import admin types)
interface PickedMedia {
  id: number
  url: string
  altText?: string
}

const showImagePicker = ref(false)
const imageUrl = ref<string | undefined>()

function handleImageSelect(media: PickedMedia) {
  showImagePicker.value = false
  imageUrl.value = media.url
  emit('update', {
    type: 'image',
    mediaId: media.id,
    alt: props.block.alt || media.altText || '',
    caption: props.block.caption
  })
}

function updateAlt(val: string) {
  emit('update', { ...props.block, alt: val || undefined })
}

function updateCaption(val: string) {
  emit('update', { ...props.block, caption: val || undefined })
}
</script>

<template>
  <div class="space-y-3">
    <div v-if="block.mediaId && imageUrl" class="relative">
      <img :src="imageUrl" :alt="block.alt" class="max-h-48 rounded border border-gray-200 dark:border-gray-700" />
      <button
        type="button"
        class="mt-1 text-sm text-primary-600 hover:text-primary-800"
        @click="showImagePicker = true"
      >
        Change Image
      </button>
    </div>
    <div v-else>
      <button
        type="button"
        class="inline-flex items-center gap-2 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700"
        @click="showImagePicker = true"
      >
        <UiIcon name="photo" class="w-5 h-5 text-gray-400" />
        Choose Image
      </button>
    </div>

    <UiInput
      :model-value="block.alt || ''"
      label="Alt Text"
      placeholder="Image description"
      @update:model-value="updateAlt"
    />

    <EditorLinkableField
      :model-value="block.caption || ''"
      label="Caption"
      placeholder="Image caption (optional)"
      @update:model-value="updateCaption"
    />

    <MediaPicker
      :open="showImagePicker"
      :selected-media-id="block.mediaId"
      @select="handleImageSelect"
      @close="showImagePicker = false"
    />
  </div>
</template>
