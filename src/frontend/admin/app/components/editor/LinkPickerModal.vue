<script setup lang="ts">
import type { TitleAutocompleteResult } from '~/composables/useTitleAutocomplete'

interface Props {
  open: boolean
  initialUrl?: string
  initialText?: string
}

const props = defineProps<Props>()

const emit = defineEmits<{
  confirm: [{ url: string; text?: string }]
  close: []
}>()

type Mode = 'internal' | 'external'

const mode = ref<Mode>('internal')
const linkText = ref('')
const externalUrl = ref('')
const searchQuery = ref('')

const { results, loading, search, clear } = useTitleAutocomplete()
const selectedResult = ref<TitleAutocompleteResult | null>(null)

watch(() => props.open, (isOpen) => {
  if (isOpen) {
    mode.value = 'internal'
    linkText.value = props.initialText || ''
    externalUrl.value = props.initialUrl?.startsWith('http') ? props.initialUrl : ''
    searchQuery.value = ''
    selectedResult.value = null
    clear()
    if (props.initialUrl && !props.initialUrl.startsWith('http')) {
      mode.value = 'internal'
    } else if (props.initialUrl?.startsWith('http')) {
      mode.value = 'external'
    }
  }
})

function handleSearchInput(val: string) {
  searchQuery.value = val
  selectedResult.value = null
  search(val)
}

function selectResult(result: TitleAutocompleteResult) {
  selectedResult.value = result
  searchQuery.value = result.title
  clear()
  if (!linkText.value) {
    linkText.value = result.title
  }
}

const confirmUrl = computed(() => {
  if (mode.value === 'internal') {
    return selectedResult.value?.url || ''
  }
  return externalUrl.value
})

const canConfirm = computed(() => {
  if (mode.value === 'internal') return !!selectedResult.value
  return !!externalUrl.value.trim()
})

function confirm() {
  if (!canConfirm.value) return
  emit('confirm', { url: confirmUrl.value, text: linkText.value || undefined })
}
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="fixed inset-0 z-50 flex items-center justify-center">
      <div class="absolute inset-0 bg-black/50" @click="$emit('close')" />
      <div class="relative bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-lg mx-4 p-6 space-y-4">
        <div class="flex items-center justify-between">
          <h2 class="text-lg font-semibold text-gray-900 dark:text-gray-100">Insert Link</h2>
          <button type="button" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200" @click="$emit('close')">
            <UiIcon name="x-mark" class="w-5 h-5" />
          </button>
        </div>

        <!-- Mode toggle -->
        <div class="flex gap-2">
          <button
            type="button"
            class="flex-1 py-2 px-3 rounded-lg text-sm font-medium transition-colors"
            :class="mode === 'internal'
              ? 'bg-primary-100 text-primary-700 dark:bg-primary-900/30 dark:text-primary-300'
              : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-600'"
            @click="mode = 'internal'"
          >
            Internal Page
          </button>
          <button
            type="button"
            class="flex-1 py-2 px-3 rounded-lg text-sm font-medium transition-colors"
            :class="mode === 'external'
              ? 'bg-primary-100 text-primary-700 dark:bg-primary-900/30 dark:text-primary-300'
              : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-600'"
            @click="mode = 'external'"
          >
            External URL
          </button>
        </div>

        <!-- Internal search -->
        <div v-if="mode === 'internal'" class="space-y-2">
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Search pages &amp; articles</label>
          <div class="relative">
            <input
              type="text"
              :value="searchQuery"
              placeholder="Type to search..."
              class="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
              @input="handleSearchInput(($event.target as HTMLInputElement).value)"
            />
            <UiLoadingSpinner v-if="loading" class="absolute right-3 top-2.5 h-4 w-4 text-gray-400" />
          </div>
          <!-- Results -->
          <div v-if="results.length > 0" class="border border-gray-200 dark:border-gray-600 rounded-lg overflow-hidden">
            <button
              v-for="result in results"
              :key="result.id"
              type="button"
              class="w-full flex items-center justify-between px-3 py-2 text-left hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors border-b border-gray-100 dark:border-gray-700 last:border-0"
              :class="selectedResult?.id === result.id ? 'bg-primary-50 dark:bg-primary-900/20' : ''"
              @click="selectResult(result)"
            >
              <div class="min-w-0">
                <p class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">{{ result.title }}</p>
                <p class="text-xs text-gray-500 dark:text-gray-400 truncate">{{ result.url }}</p>
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
          <p v-else-if="searchQuery && !loading" class="text-sm text-gray-500 dark:text-gray-400">
            No results found
          </p>
          <p v-if="selectedResult" class="text-sm text-primary-600 dark:text-primary-400">
            Selected: {{ selectedResult.title }} → <span class="font-mono">{{ selectedResult.url }}</span>
          </p>
        </div>

        <!-- External URL -->
        <div v-else class="space-y-2">
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">URL</label>
          <input
            v-model="externalUrl"
            type="url"
            placeholder="https://..."
            class="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
          />
        </div>

        <!-- Link text -->
        <div class="space-y-2">
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Link Text (optional)</label>
          <input
            v-model="linkText"
            type="text"
            placeholder="Leave empty to use selected text"
            class="w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
          />
        </div>

        <!-- Actions -->
        <div class="flex justify-end gap-3 pt-2">
          <button
            type="button"
            class="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
            @click="$emit('close')"
          >
            Cancel
          </button>
          <button
            type="button"
            class="px-4 py-2 text-sm font-medium text-white bg-primary-600 rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            :disabled="!canConfirm"
            @click="confirm"
          >
            Insert Link
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
