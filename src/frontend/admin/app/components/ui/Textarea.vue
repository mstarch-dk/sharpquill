<script setup lang="ts">
interface Props {
  modelValue: string
  label?: string
  placeholder?: string
  error?: string
  required?: boolean
  disabled?: boolean
  rows?: number
  maxlength?: number
  spellcheck?: boolean
}

withDefaults(defineProps<Props>(), {
  placeholder: '',
  required: false,
  disabled: false,
  rows: 3,
  spellcheck: false
})

defineEmits<{
  'update:modelValue': [value: string]
}>()
</script>

<template>
  <div>
    <label v-if="label" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
      {{ label }}
      <span v-if="required" class="text-red-500">*</span>
    </label>
    <textarea
      :value="modelValue"
      :placeholder="placeholder"
      :required="required"
      :disabled="disabled"
      :rows="rows"
      :maxlength="maxlength"
      :spellcheck="spellcheck"
      autocorrect="off"
      autocapitalize="off"
      class="w-full px-3 py-2 border rounded-lg shadow-sm dark:shadow-gray-900/30 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-100 dark:disabled:bg-gray-700 disabled:cursor-not-allowed resize-vertical dark:bg-gray-800 dark:text-gray-100"
      :class="error ? 'border-red-300 dark:border-red-700' : 'border-gray-300 dark:border-gray-600'"
      @input="$emit('update:modelValue', ($event.target as HTMLTextAreaElement).value)"
    />
    <p v-if="error" class="mt-1 text-sm text-red-600 dark:text-red-400">{{ error }}</p>
  </div>
</template>
