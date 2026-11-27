<script setup lang="ts">
import type { ArticleAdminDto } from '~/types/article'
import { getStatusLabel, getStatusColor } from '~/types/article'

interface Props {
  open: boolean
  articles: ArticleAdminDto[]
  loading?: boolean
}

const props = defineProps<Props>()

const emit = defineEmits<{
  close: []
  select: [articleId: number]
}>()

const search = ref('')

const filteredArticles = computed(() => {
  if (!search.value.trim()) return props.articles
  const term = search.value.toLowerCase()
  return props.articles.filter(a => a.title.toLowerCase().includes(term))
})

watch(() => props.open, (isOpen) => {
  if (isOpen) search.value = ''
})
</script>

<template>
  <UiModal :open="open" title="Add Article to Series" @close="$emit('close')">
    <div class="space-y-4">
      <UiInput
        v-model="search"
        placeholder="Search articles..."
      />

      <div v-if="loading" class="py-8 text-center">
        <UiLoadingSpinner class="h-6 w-6 mx-auto text-primary-600" />
      </div>

      <div v-else-if="filteredArticles.length === 0" class="py-8 text-center text-gray-500 dark:text-gray-400">
        No available articles found.
      </div>

      <ul v-else class="max-h-80 overflow-y-auto divide-y divide-gray-200 dark:divide-gray-700 border border-gray-200 dark:border-gray-700 rounded-lg">
        <li
          v-for="article in filteredArticles"
          :key="article.id"
          class="px-4 py-3 hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer flex items-center justify-between"
          @click="emit('select', article.id)"
        >
          <div class="min-w-0 flex-1">
            <p class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
              {{ article.title }}
            </p>
          </div>
          <span
            class="ml-3 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
            :class="getStatusColor(article.status)"
          >
            {{ getStatusLabel(article.status) }}
          </span>
        </li>
      </ul>
    </div>

    <template #footer>
      <UiButton variant="secondary" @click="$emit('close')">
        Cancel
      </UiButton>
    </template>
  </UiModal>
</template>
