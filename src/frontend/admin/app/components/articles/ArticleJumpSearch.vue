<script setup lang="ts">
// The article list's search box. Typing filters the list (through v-model, as before) and shows a jump-to list of
// matching titles in every status. Arrow keys move through it, Enter opens the highlighted article in the editor
// (with nothing highlighted Enter just closes the list), and Escape closes it. ARIA 1.2 combobox.
const props = withDefaults(defineProps<{
  modelValue: string
  label?: string
  placeholder?: string
}>(), {
  label: 'Search',
  placeholder: 'Title or text'
})

const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const { suggestions, query: suggestedFor, update, clear } = useArticleSuggestions()
const inputId = useId()
const listId = useId()
const open = ref(false)
const activeIndex = ref(-1)

const expanded = computed(() => open.value && suggestions.value.length > 0)

// The page also sets the value (clear filters, back/forward): keep the suggestions in step, without opening the list.
watch(() => props.modelValue, (value) => {
  if (!value.trim()) clear()
})

function optionId(index: number) {
  return `${listId}-option-${index}`
}

function close() {
  open.value = false
  activeIndex.value = -1
}

function onInput(event: Event) {
  const value = (event.target as HTMLInputElement).value
  emit('update:modelValue', value)
  activeIndex.value = -1
  open.value = true
  update(value)
}

function onKeydown(event: KeyboardEvent) {
  const count = suggestions.value.length
  switch (event.key) {
    case 'ArrowDown':
    case 'ArrowUp':
      if (!count) return
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
      event.preventDefault()
      if (expanded.value && activeIndex.value >= 0) {
        choose(activeIndex.value)
      } else {
        close()
      }
  }
}

async function choose(index: number) {
  const suggestion = suggestions.value[index]
  if (!suggestion) return
  close()
  await navigateTo(`/articles/${suggestion.id}/edit`)
}

let blurTimer: ReturnType<typeof setTimeout> | undefined
function onBlur() {
  blurTimer = setTimeout(close, 100)
}
function onFocus() {
  clearTimeout(blurTimer)
  if (props.modelValue.trim()) open.value = true
}
onBeforeUnmount(() => clearTimeout(blurTimer))
</script>

<template>
  <div class="relative">
    <label :for="inputId" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">{{ label }}</label>
    <input
      :id="inputId"
      type="search"
      :value="modelValue"
      :placeholder="placeholder"
      maxlength="200"
      autocomplete="off"
      role="combobox"
      aria-autocomplete="list"
      :aria-expanded="expanded"
      :aria-controls="listId"
      :aria-activedescendant="expanded && activeIndex >= 0 ? optionId(activeIndex) : undefined"
      class="w-full px-3 py-2 border rounded-lg shadow-sm dark:shadow-gray-900/30 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 dark:bg-gray-800 dark:text-gray-100 border-gray-300 dark:border-gray-600"
      @input="onInput"
      @keydown="onKeydown"
      @blur="onBlur"
      @focus="onFocus"
    />
    <div
      v-show="expanded"
      :id="listId"
      role="listbox"
      aria-label="Open article"
      class="absolute z-40 mt-1 w-full min-w-72 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded-lg shadow-lg overflow-hidden"
    >
      <div
        v-for="(suggestion, index) in suggestions"
        :id="optionId(index)"
        :key="suggestion.id"
        role="option"
        :aria-selected="index === activeIndex"
        :class="[
          'flex cursor-pointer items-center justify-between gap-2 px-3 py-2 text-sm border-b border-gray-100 dark:border-gray-700 last:border-0',
          index === activeIndex ? 'bg-primary-50 dark:bg-gray-700' : 'hover:bg-gray-50 dark:hover:bg-gray-700'
        ]"
        @mousedown.prevent
        @click="choose(index)"
        @mousemove="activeIndex = index"
      >
        <span class="min-w-0 truncate text-gray-900 dark:text-gray-100">
          <template v-for="(part, i) in highlightPrefixes(suggestion.title, suggestedFor)" :key="i">
            <mark v-if="part.match" class="bg-transparent font-semibold text-primary-700 dark:text-primary-300">{{ part.text }}</mark>
            <template v-else>{{ part.text }}</template>
          </template>
        </span>
        <ArticlesArticleStatusBadge :status="suggestion.status" :published-at="suggestion.publishedAt" class="flex-shrink-0" />
      </div>
    </div>
  </div>
</template>
