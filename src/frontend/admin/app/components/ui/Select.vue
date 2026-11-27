<script setup lang="ts">
interface Option {
  value: string | number
  label: string
}

interface Props {
  modelValue: string | number | undefined
  options: Option[]
  label?: string
  placeholder?: string
  error?: string
  required?: boolean
  disabled?: boolean
}

withDefaults(defineProps<Props>(), {
  placeholder: 'Select an option',
  required: false,
  disabled: false
})

defineEmits<{
  'update:modelValue': [value: string | number | undefined]
}>()

function handleChange(event: Event) {
  const target = event.target as HTMLSelectElement
  const value = target.value
  if (value === '') {
    return undefined
  }
  // Try to parse as number if it looks like one
  const numValue = Number(value)
  return isNaN(numValue) ? value : numValue
}
</script>

<template>
  <div>
    <label v-if="label" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
      {{ label }}
      <span v-if="required" class="text-red-500">*</span>
    </label>
    <select
      :value="modelValue ?? ''"
      :required="required"
      :disabled="disabled"
      class="w-full px-3 py-2 border rounded-lg shadow-sm dark:shadow-gray-900/30 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:bg-gray-100 dark:disabled:bg-gray-700 disabled:cursor-not-allowed bg-white dark:bg-gray-800 dark:text-gray-100"
      :class="error ? 'border-red-300 dark:border-red-700' : 'border-gray-300 dark:border-gray-600'"
      @change="$emit('update:modelValue', handleChange($event))"
    >
      <option value="">{{ placeholder }}</option>
      <option
        v-for="option in options"
        :key="option.value"
        :value="option.value"
      >
        {{ option.label }}
      </option>
    </select>
    <p v-if="error" class="mt-1 text-sm text-red-600 dark:text-red-400">{{ error }}</p>
  </div>
</template>
