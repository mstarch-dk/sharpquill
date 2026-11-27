<script setup lang="ts">
import type { SeriesArticleAdminDto } from '~/types/series'
import { getStatusLabel, getStatusColor } from '~/types/article'

interface Props {
  articles: SeriesArticleAdminDto[]
  loading?: boolean
}

defineProps<Props>()

const emit = defineEmits<{
  moveUp: [articleId: number]
  moveDown: [articleId: number]
  remove: [articleId: number]
}>()
</script>

<template>
  <div class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg overflow-hidden">
    <div class="px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
      <h3 class="text-lg font-medium text-gray-900 dark:text-gray-100">Articles in Series</h3>
      <slot name="actions" />
    </div>

    <div v-if="loading" class="p-8 text-center">
      <UiLoadingSpinner class="h-6 w-6 mx-auto text-primary-600" />
    </div>

    <div v-else-if="articles.length === 0" class="p-8 text-center text-gray-500 dark:text-gray-400">
      No articles in this series yet.
    </div>

    <ul v-else class="divide-y divide-gray-200 dark:divide-gray-700">
      <li
        v-for="(article, index) in articles"
        :key="article.id"
        class="px-6 py-4 flex items-center gap-4"
      >
        <span class="text-sm font-medium text-gray-400 dark:text-gray-500 w-8 text-center">
          {{ index + 1 }}
        </span>

        <div class="flex-1 min-w-0">
          <p class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
            {{ article.title }}
          </p>
          <p class="text-sm text-gray-500 dark:text-gray-400">
            by {{ article.authorName }}
          </p>
        </div>

        <span
          class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
          :class="getStatusColor(article.status, article.publishedAt)"
        >
          {{ getStatusLabel(article.status, article.publishedAt) }}
        </span>

        <div class="flex items-center gap-1">
          <button
            type="button"
            class="p-1 text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-400 disabled:opacity-30 disabled:cursor-not-allowed"
            :disabled="index === 0"
            title="Move up"
            @click="emit('moveUp', article.id)"
          >
            <UiIcon name="chevron-up" class="h-5 w-5" />
          </button>
          <button
            type="button"
            class="p-1 text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-400 disabled:opacity-30 disabled:cursor-not-allowed"
            :disabled="index === articles.length - 1"
            title="Move down"
            @click="emit('moveDown', article.id)"
          >
            <UiIcon name="chevron-down" class="h-5 w-5" />
          </button>
          <button
            type="button"
            class="p-1 text-red-400 dark:text-red-500 hover:text-red-600 dark:hover:text-red-400"
            title="Remove from series"
            @click="emit('remove', article.id)"
          >
            <UiIcon name="x-mark" class="h-5 w-5" />
          </button>
        </div>
      </li>
    </ul>
  </div>
</template>
