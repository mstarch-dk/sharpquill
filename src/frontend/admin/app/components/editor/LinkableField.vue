<script setup lang="ts">
interface Props {
  modelValue: string
  label: string
  placeholder?: string
  multiline?: boolean
  rows?: number
}

const props = withDefaults(defineProps<Props>(), {
  placeholder: '',
  multiline: false,
  rows: 2
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const showLinkPicker = ref(false)
const savedSelection = ref<{ start: number; end: number } | null>(null)
const inputRef = ref<HTMLInputElement | null>(null)
const textareaRef = ref<HTMLTextAreaElement | null>(null)

function openLinkPicker() {
  const el = props.multiline ? textareaRef.value : inputRef.value
  if (el) {
    savedSelection.value = { start: el.selectionStart ?? 0, end: el.selectionEnd ?? 0 }
  } else {
    savedSelection.value = null
  }
  showLinkPicker.value = true
}

function getInitialText(): string {
  if (!savedSelection.value) return ''
  const { start, end } = savedSelection.value
  return props.modelValue.slice(start, end)
}

function handleLinkConfirm({ url, text }: { url: string; text?: string }) {
  showLinkPicker.value = false
  const linkText = text || getInitialText() || url
  const markdown = `[${linkText}](${url})`
  const { start, end } = savedSelection.value ?? { start: props.modelValue.length, end: props.modelValue.length }
  const newValue = props.modelValue.slice(0, start) + markdown + props.modelValue.slice(end)
  emit('update:modelValue', newValue)
  savedSelection.value = null
}
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-1">
      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">{{ label }}</label>
      <button
        type="button"
        class="p-1 text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 transition-colors rounded"
        title="Insert link"
        @click="openLinkPicker"
      >
        <UiIcon name="link" class="w-3.5 h-3.5" />
      </button>
    </div>

    <textarea
      v-if="multiline"
      ref="textareaRef"
      :value="modelValue"
      :placeholder="placeholder"
      :rows="rows"
      :spellcheck="false"
      autocorrect="off"
      autocapitalize="off"
      class="w-full px-3 py-2 border rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 border-gray-300 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
      @input="emit('update:modelValue', ($event.target as HTMLTextAreaElement).value)"
    />
    <input
      v-else
      ref="inputRef"
      type="text"
      :value="modelValue"
      :placeholder="placeholder"
      class="w-full px-3 py-2 border rounded-lg shadow-sm dark:shadow-gray-900/30 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 border-gray-300 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
      @input="emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />

    <EditorLinkPickerModal
      :open="showLinkPicker"
      :initial-text="getInitialText()"
      @confirm="handleLinkConfirm"
      @close="showLinkPicker = false"
    />
  </div>
</template>
