<script setup lang="ts">
import { featureIconCategories, featureIcons, isFeatureIcon } from '#layer/utils/featureIcons'

// Picker for the curated outline card icons (utils/featureIcons.ts). Emits the icon key, or '' to clear.
interface Props {
  modelValue: string
  label?: string
}

const props = defineProps<Props>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const open = ref(false)
const search = ref('')
const containerEl = ref<HTMLElement | null>(null)
const searchEl = ref<HTMLInputElement | null>(null)

const selected = computed(() => (isFeatureIcon(props.modelValue) ? featureIcons[props.modelValue] : undefined))

const groups = computed(() => {
  const q = search.value.trim().toLowerCase()
  return featureIconCategories
    .map(category => ({
      category,
      icons: Object.entries(featureIcons)
        .filter(([key, def]) => def.category === category &&
          (!q || key.includes(q) || def.label.toLowerCase().includes(q) || def.keywords.includes(q)))
        .map(([key, def]) => ({ key, def }))
    }))
    .filter(group => group.icons.length > 0)
})

function close() {
  open.value = false
  search.value = ''
}

function select(key: string) {
  emit('update:modelValue', key)
  close()
}

function clear() {
  emit('update:modelValue', '')
  close()
}

function toggle() {
  open.value = !open.value
  if (open.value) {
    search.value = ''
    nextTick(() => searchEl.value?.focus())
  }
}

function onDocumentMousedown(e: MouseEvent) {
  if (open.value && containerEl.value && !containerEl.value.contains(e.target as Node)) close()
}

function onDocumentKeydown(e: KeyboardEvent) {
  if (open.value && e.key === 'Escape') close()
}

onMounted(() => {
  document.addEventListener('mousedown', onDocumentMousedown)
  document.addEventListener('keydown', onDocumentKeydown)
})
onUnmounted(() => {
  document.removeEventListener('mousedown', onDocumentMousedown)
  document.removeEventListener('keydown', onDocumentKeydown)
})
</script>

<template>
  <div ref="containerEl" class="relative">
    <label v-if="label" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
      {{ label }}
    </label>
    <button
      type="button"
      class="flex items-center gap-2 px-3 py-2 border rounded-lg shadow-sm bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 hover:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-500 transition-colors"
      @click="toggle"
    >
      <svg v-if="selected" class="h-5 w-5 text-primary-600 dark:text-primary-300" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
        <path v-for="(d, i) in selected.paths" :key="i" :d="d" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
      <span class="text-sm text-gray-500 dark:text-gray-400">{{ selected ? selected.label : 'Pick icon…' }}</span>
    </button>

    <div
      v-if="open"
      class="absolute z-50 mt-1 w-80 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg"
    >
      <div class="p-2 border-b border-gray-100 dark:border-gray-700 flex gap-2">
        <input
          ref="searchEl"
          v-model="search"
          type="text"
          placeholder="Search icons…"
          class="flex-1 px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 dark:text-gray-100 focus:outline-none focus:ring-1 focus:ring-primary-500"
        />
        <button
          type="button"
          class="text-xs px-2 py-1 text-gray-500 dark:text-gray-400 hover:text-red-500 border border-gray-300 dark:border-gray-600 rounded-md transition-colors"
          @click="clear"
        >
          Clear
        </button>
      </div>

      <div class="overflow-y-auto max-h-72 p-2 space-y-3">
        <template v-if="groups.length > 0">
          <div v-for="group in groups" :key="group.category">
            <p class="text-xs font-medium text-gray-400 dark:text-gray-500 mb-1">{{ group.category }}</p>
            <div class="grid grid-cols-7 gap-1">
              <button
                v-for="entry in group.icons"
                :key="entry.key"
                type="button"
                :title="entry.def.label"
                :class="[
                  'flex items-center justify-center p-1.5 rounded-md transition-colors',
                  entry.key === modelValue
                    ? 'bg-primary-100 text-primary-700 dark:bg-primary-900/40 dark:text-primary-300'
                    : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
                ]"
                @click="select(entry.key)"
              >
                <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                  <path v-for="(d, i) in entry.def.paths" :key="i" :d="d" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
              </button>
            </div>
          </div>
        </template>
        <p v-else class="text-sm text-center text-gray-400 py-4">
          No icons found
        </p>
      </div>
    </div>
  </div>
</template>
