<script setup lang="ts">
interface Props {
  modelValue: string
  placeholder?: string
}

withDefaults(defineProps<Props>(), {
  placeholder: 'Start writing...'
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
  close: []
}>()

function handleUpdate(value: string) {
  emit('update:modelValue', value)
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') {
    emit('close')
  }
}

onMounted(() => {
  document.addEventListener('keydown', handleKeydown)
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeydown)
})
</script>

<template>
  <Teleport to="body">
    <div class="fixed inset-0 z-50 flex flex-col bg-white dark:bg-gray-900">
      <!-- Header -->
      <div class="flex items-center justify-between px-6 py-3 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800">
        <span class="text-sm font-medium text-gray-600 dark:text-gray-300">Fullscreen Editor</span>
        <button
          type="button"
          class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 rounded-md hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors"
          @click="$emit('close')"
        >
          <UiIcon name="minimize" class="h-4 w-4" />
          <span>Exit</span>
          <kbd class="ml-1 text-xs px-1.5 py-0.5 bg-gray-200 dark:bg-gray-600 text-gray-500 dark:text-gray-400 rounded">Esc</kbd>
        </button>
      </div>

      <!-- Editor -->
      <div class="flex-1 overflow-auto">
        <div class="max-w-4xl mx-auto py-8 px-6">
          <EditorTipTapEditor
            :model-value="modelValue"
            :placeholder="placeholder"
            :show-fullscreen-button="false"
            :fullscreen="true"
            @update:model-value="handleUpdate"
          />
        </div>
      </div>
    </div>
  </Teleport>
</template>
