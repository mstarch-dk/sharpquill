<script setup lang="ts">
import { type ArticleAdminDto, isScheduled, formatDateTime } from '~/types/article'

defineProps<{
  articles: ArticleAdminDto[]
  loading?: boolean
  /** Search or filters are active: the empty state says nothing matched instead of inviting a first article. */
  filtered?: boolean
}>()

defineEmits<{
  edit: [article: ArticleAdminDto]
  delete: [article: ArticleAdminDto]
}>()

function formatDate(dateString: string | undefined): string {
  if (!dateString) return '-'
  return new Date(dateString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}
</script>

<template>
  <div class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg overflow-hidden">
    <!-- Loading state -->
    <div v-if="loading" class="p-8 text-center text-gray-500 dark:text-gray-400">
      <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600" />
      <p class="mt-2">Loading articles...</p>
    </div>

    <!-- Empty state -->
    <div v-else-if="articles.length === 0" class="p-8 text-center text-gray-500 dark:text-gray-400">
      <UiIcon name="document-text" class="mx-auto h-12 w-12 text-gray-400 dark:text-gray-500" />
      <p class="mt-2">No articles found</p>
      <p class="text-sm">{{ filtered ? 'No articles match the search and filters.' : 'Get started by creating a new article.' }}</p>
    </div>

    <!-- Table -->
    <div v-else class="overflow-x-auto">
    <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
      <thead class="bg-gray-50 dark:bg-gray-700">
        <tr>
          <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
            Title
          </th>
          <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
            Author
          </th>
          <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
            Category
          </th>
          <th scope="col" class="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
            Public
          </th>
          <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
            Status
          </th>
          <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
            Date
          </th>
          <th scope="col" class="relative px-6 py-3">
            <span class="sr-only">Actions</span>
          </th>
        </tr>
      </thead>
      <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
        <tr v-for="article in articles" :key="article.id" class="hover:bg-gray-50 dark:hover:bg-gray-700">
          <td class="px-6 py-4">
            <div class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate max-w-xs">
              {{ article.title }}
            </div>
            <div class="text-sm text-gray-500 dark:text-gray-400 truncate max-w-xs">
              /{{ article.slug }}
            </div>
          </td>
          <td class="px-6 py-4 whitespace-nowrap">
            <div class="text-sm text-gray-900 dark:text-gray-100">{{ article.authorName || article.author?.name || '-' }}</div>
          </td>
          <td class="px-6 py-4 whitespace-nowrap">
            <div class="text-sm text-gray-900 dark:text-gray-100">{{ article.category?.name || '-' }}</div>
          </td>
          <td class="px-6 py-4 whitespace-nowrap text-center">
            <UiIcon v-if="!article.requiresSignIn" name="check" class="w-5 h-5 text-green-500" />
            <UiIcon v-else name="lock" class="w-5 h-5 text-gray-400 dark:text-gray-500" title="Requires sign-in" />
          </td>
          <td class="px-6 py-4 whitespace-nowrap">
            <ArticlesArticleStatusBadge :status="article.status" :published-at="article.publishedAt" />
          </td>
          <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
            <template v-if="isScheduled(article.status, article.publishedAt)">
              <span class="text-blue-700 dark:text-blue-300">{{ formatDateTime(article.publishedAt!) }}</span>
            </template>
            <template v-else>
              {{ formatDate(article.publishedAt || article.createdAt) }}
            </template>
          </td>
          <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
            <button
              type="button"
              class="text-primary-600 hover:text-primary-900 mr-4"
              @click="$emit('edit', article)"
            >
              Edit
            </button>
            <button
              type="button"
              class="text-red-600 dark:text-red-400 hover:text-red-900 dark:hover:text-red-300"
              @click="$emit('delete', article)"
            >
              Delete
            </button>
          </td>
        </tr>
      </tbody>
    </table>
    </div>
  </div>
</template>
