<script setup lang="ts">
import type { MediaDto } from '~/types/media'

interface Props {
  modelValue?: number
  imageUrl?: string
  label?: string
}

const props = defineProps<Props>()

const emit = defineEmits<{
  'update:modelValue': [value: number | undefined]
  'update:imageUrl': [value: string | undefined]
}>()

const showPicker = ref(false)
const currentUrl = ref(props.imageUrl)

watch(() => props.imageUrl, (val) => {
  currentUrl.value = val
})

function handleSelect(media: MediaDto) {
  emit('update:modelValue', media.id)
  emit('update:imageUrl', media.url)
  currentUrl.value = media.url
  showPicker.value = false
}

function handleRemove() {
  emit('update:modelValue', undefined)
  emit('update:imageUrl', undefined)
  currentUrl.value = undefined
}
</script>

<template>
  <div>
    <label v-if="label" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
      {{ label }}
    </label>

    <div v-if="currentUrl" class="relative inline-block">
      <img
        :src="currentUrl"
        class="w-full max-w-xs h-40 object-cover rounded-lg border border-gray-200 dark:border-gray-700"
      />
      <div class="mt-2 flex gap-2">
        <button
          type="button"
          class="text-sm text-primary-600 hover:text-primary-800"
          @click="showPicker = true"
        >
          Change Image
        </button>
        <button
          type="button"
          class="text-sm text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-300"
          @click="handleRemove"
        >
          Remove
        </button>
      </div>
    </div>

    <div v-else>
      <button
        type="button"
        class="inline-flex items-center gap-2 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700"
        @click="showPicker = true"
      >
        <UiIcon name="photo" class="w-5 h-5 text-gray-400 dark:text-gray-500" />
        Choose Image
      </button>
    </div>

    <MediaPicker
      :open="showPicker"
      :selected-media-id="modelValue"
      @select="handleSelect"
      @close="showPicker = false"
    />
  </div>
</template>
