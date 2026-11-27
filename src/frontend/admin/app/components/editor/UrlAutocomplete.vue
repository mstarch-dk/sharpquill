<script setup lang="ts">
interface Props {
  modelValue: string
  label?: string
  placeholder?: string
}

withDefaults(defineProps<Props>(), {
  placeholder: '/page-slug or https://...'
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const { results, loading, search, clear } = useTitleAutocomplete()
const showDropdown = ref(false)
const inputRef = ref<HTMLInputElement | null>(null)

function handleInput(val: string) {
  emit('update:modelValue', val)
  // Only search internal if not an http URL
  if (val && !val.startsWith('http')) {
    search(val)
    showDropdown.value = true
  } else {
    clear()
    showDropdown.value = false
  }
}

function selectResult(url: string) {
  emit('update:modelValue', url)
  showDropdown.value = false
  clear()
}

function handleBlur() {
  // Delay to allow click on dropdown items
  setTimeout(() => {
    showDropdown.value = false
  }, 150)
}
</script>

<template>
  <div class="relative">
    <label v-if="label" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
      {{ label }}
    </label>
    <div class="relative">
      <input
        ref="inputRef"
        type="text"
        :value="modelValue"
        :placeholder="placeholder"
        class="w-full px-3 py-2 border rounded-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 border-gray-300 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
        @input="handleInput(($event.target as HTMLInputElement).value)"
        @blur="handleBlur"
        @focus="showDropdown = results.length > 0"
      />
      <UiLoadingSpinner v-if="loading" class="absolute right-3 top-2.5 h-4 w-4 text-gray-400" />
    </div>
    <div
      v-if="showDropdown && results.length > 0"
      class="absolute z-40 mt-1 w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded-lg shadow-lg overflow-hidden"
    >
      <button
        v-for="result in results"
        :key="result.id"
        type="button"
        class="w-full flex items-center justify-between px-3 py-2 text-left hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors border-b border-gray-100 dark:border-gray-700 last:border-0"
        @mousedown.prevent="selectResult(result.url)"
      >
        <div class="min-w-0">
          <p class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">{{ result.title }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 truncate font-mono">{{ result.url }}</p>
        </div>
        <span
          class="ml-2 flex-shrink-0 text-xs px-2 py-0.5 rounded-full"
          :class="result.contentType === 'page'
            ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300'
            : 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'"
        >
          {{ result.contentType }}
        </span>
      </button>
    </div>
  </div>
</template>
