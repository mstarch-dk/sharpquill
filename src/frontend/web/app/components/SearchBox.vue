<script setup lang="ts">
import type { SearchSuggestionDto } from '~/types/api'

// A plain GET form to /search, so searching also works before hydration or without JavaScript. Once hydrated it is
// an ARIA 1.2 combobox with search-as-you-type suggestions: arrow keys move through them, Enter opens the highlighted
// one (or submits the search when none is highlighted) and Escape closes the list.
const props = defineProps<{
  /** Stretch to the container width (mobile menu, search page) */
  block?: boolean
  autofocus?: boolean
}>()

const emit = defineEmits<{ submitted: [] }>()

const route = useRoute()
const { t } = useI18n()
const { suggestions, query: suggestedFor, update, clear } = useSearchSuggestions()

const inputId = props.block ? 'site-search-block' : 'site-search'
const listId = useId()
const term = ref(route.path === '/search' ? String(route.query.q ?? '') : '')
const open = ref(false)
const activeIndex = ref(-1)

watch(() => [route.path, route.query.q], () => {
  term.value = route.path === '/search' ? String(route.query.q ?? '') : ''
  close()
})

const groupOrder: SearchSuggestionDto['type'][] = ['article', 'tag', 'category', 'series']
const groupLabels = computed<Record<SearchSuggestionDto['type'], string>>(() => ({
  article: t('search.suggestions.articles'),
  tag: t('search.suggestions.tags'),
  category: t('search.suggestions.categories'),
  series: t('search.suggestions.series')
}))

// Suggestions grouped by type, each with its position in the flat option list that the arrow keys walk through.
const groups = computed(() => {
  let index = 0
  return groupOrder
    .map(type => ({
      type,
      items: suggestions.value.filter(s => s.type === type).map(suggestion => ({ suggestion, index: index++ }))
    }))
    .filter(group => group.items.length)
})

const seeAllIndex = computed(() => suggestions.value.length)
const expanded = computed(() => open.value && suggestions.value.length > 0)

function optionId(index: number) {
  return `${listId}-option-${index}`
}

function close() {
  open.value = false
  activeIndex.value = -1
}

function onInput() {
  activeIndex.value = -1
  open.value = true
  update(term.value)
}

function onKeydown(event: KeyboardEvent) {
  const count = suggestions.value.length + 1 // + "see all results"
  switch (event.key) {
    case 'ArrowDown':
    case 'ArrowUp':
      if (!suggestions.value.length) return
      event.preventDefault()
      if (!open.value) {
        open.value = true
        activeIndex.value = event.key === 'ArrowDown' ? 0 : count - 1
        return
      }
      activeIndex.value = event.key === 'ArrowDown'
        ? (activeIndex.value + 1) % count
        : (activeIndex.value <= 0 ? count - 1 : activeIndex.value - 1)
      return
    case 'Escape':
      if (expanded.value) {
        event.preventDefault()
        close()
      }
      return
    case 'Enter':
      if (expanded.value && activeIndex.value >= 0) {
        event.preventDefault()
        choose(activeIndex.value)
      }
  }
}

async function choose(index: number) {
  const suggestion = suggestions.value[index]
  if (!suggestion) {
    await submit()
    return
  }
  close()
  clear()
  term.value = ''
  emit('submitted')
  await navigateTo(suggestion.url)
}

async function submit() {
  const q = term.value.trim()
  close()
  emit('submitted')
  await navigateTo({ path: '/search', query: q ? { q } : {} })
}

let blurTimer: ReturnType<typeof setTimeout> | undefined
function onBlur() {
  blurTimer = setTimeout(close, 100)
}
function onFocus() {
  clearTimeout(blurTimer)
  if (term.value.trim()) open.value = true
}
onBeforeUnmount(() => clearTimeout(blurTimer))
</script>

<template>
  <form
    action="/search"
    method="get"
    role="search"
    :class="['relative', props.block ? 'w-full' : 'w-48 lg:w-56']"
    @submit.prevent="submit"
  >
    <label :for="inputId" class="sr-only">{{ t('search.label') }}</label>
    <input
      :id="inputId"
      v-model="term"
      type="search"
      name="q"
      maxlength="200"
      autocomplete="off"
      role="combobox"
      aria-autocomplete="list"
      :aria-expanded="expanded"
      :aria-controls="listId"
      :aria-activedescendant="expanded && activeIndex >= 0 ? optionId(activeIndex) : undefined"
      :autofocus="props.autofocus"
      :placeholder="t('search.placeholder')"
      class="w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 py-1.5 pl-3 pr-9 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
      @input="onInput"
      @keydown="onKeydown"
      @blur="onBlur"
      @focus="onFocus"
    />
    <button
      type="submit"
      class="absolute inset-y-0 right-0 flex items-center px-2.5 text-gray-500 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400"
      :aria-label="t('search.submit')"
    >
      <UiIcon name="magnifying-glass" class="w-4 h-4" />
    </button>

    <div
      v-show="expanded"
      :id="listId"
      role="listbox"
      :aria-label="t('search.suggestions.label')"
      :class="[
        'absolute z-50 mt-1 overflow-hidden rounded-md border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 shadow-lg text-sm',
        props.block ? 'inset-x-0' : 'right-0 w-80 max-w-[calc(100vw-2rem)]'
      ]"
    >
      <div v-for="group in groups" :key="group.type" role="group" :aria-labelledby="`${listId}-${group.type}`">
        <div
          :id="`${listId}-${group.type}`"
          role="presentation"
          class="px-3 pt-2 pb-1 text-xs font-medium uppercase tracking-wider text-gray-400 dark:text-gray-500"
        >
          {{ groupLabels[group.type] }}
        </div>
        <div
          v-for="{ suggestion, index } in group.items"
          :id="optionId(index)"
          :key="suggestion.url"
          role="option"
          :aria-selected="index === activeIndex"
          :class="[
            'cursor-pointer px-3 py-1.5 truncate text-gray-700 dark:text-gray-300',
            index === activeIndex ? 'bg-primary-50 dark:bg-gray-800 text-primary-700 dark:text-primary-300' : 'hover:bg-gray-50 dark:hover:bg-gray-800'
          ]"
          @mousedown.prevent
          @click="choose(index)"
          @mousemove="activeIndex = index"
        >
          <template v-for="(part, i) in highlightPrefixes(suggestion.title, suggestedFor)" :key="i">
            <mark v-if="part.match" class="bg-transparent font-semibold text-gray-900 dark:text-gray-100">{{ part.text }}</mark>
            <template v-else>{{ part.text }}</template>
          </template>
        </div>
      </div>
      <div
        :id="optionId(seeAllIndex)"
        role="option"
        :aria-selected="seeAllIndex === activeIndex"
        :class="[
          'cursor-pointer border-t border-gray-100 dark:border-gray-800 px-3 py-2 truncate text-primary-600 dark:text-primary-400',
          seeAllIndex === activeIndex ? 'bg-primary-50 dark:bg-gray-800' : 'hover:bg-gray-50 dark:hover:bg-gray-800'
        ]"
        @mousedown.prevent
        @click="submit"
        @mousemove="activeIndex = seeAllIndex"
      >
        {{ t('search.suggestions.seeAll', { query: term.trim() }) }}
      </div>
    </div>
  </form>
</template>
