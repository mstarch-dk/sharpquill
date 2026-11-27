<script setup lang="ts">
import { marked } from 'marked'
import type { ArticlePreviewDto } from '~/types/article'
import { getFeaturedImageContainerClass } from '#layer/utils/featuredImage'
import { getVariantFallbackMessage } from '~/utils/featuredImageVariants'

definePageMeta({
  layout: 'preview',
  middleware: 'auth'
})

const route = useRoute()
const articleId = computed(() => Number(route.params.id))

const { getPreview } = useAdminArticles()

const article = ref<ArticlePreviewDto | null>(null)
const isLoading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    const response = await getPreview(articleId.value)
    article.value = response.data
  } catch {
    error.value = 'Failed to load preview'
  } finally {
    isLoading.value = false
  }
})

const renderedContent = computed(() => {
  if (!article.value?.content) return ''
  return marked.parse(article.value.content, { async: false })
})

const displayImageUrl = computed(() =>
  article.value?.featuredImageDisplayUrl || article.value?.featuredImageMediumUrl || article.value?.featuredImageUrl
)
const featuredImageContainerClass = computed(() =>
  getFeaturedImageContainerClass(article.value?.featuredImageSize)
)
const featuredImageFallbackMessage = computed(() =>
  getVariantFallbackMessage(
    article.value?.featuredImageSize,
    displayImageUrl.value,
    article.value?.featuredImageUrl,
    article.value?.featuredImageMediumUrl
  )
)

function formatDate(dateStr?: string): string {
  if (!dateStr) return ''
  return new Date(dateStr).toLocaleDateString('da-DK', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}
</script>

<template>
  <div>
    <!-- Preview banner -->
    <div class="bg-amber-50 dark:bg-amber-900/30 border-b border-amber-200 dark:border-amber-800 px-4 py-2.5 flex items-center justify-between">
      <div class="flex items-center gap-2">
        <span class="inline-flex items-center gap-1.5 px-2.5 py-1 text-xs font-semibold bg-amber-200 dark:bg-amber-800 text-amber-800 dark:text-amber-200 rounded-full">
          <UiIcon name="eye" class="w-3 h-3" />
          Preview Mode
        </span>
        <span class="text-xs text-amber-700 dark:text-amber-300">This is a preview of the latest draft content</span>
      </div>
      <NuxtLink
        :to="`/articles/${articleId}/edit`"
        class="text-sm font-medium text-amber-700 dark:text-amber-300 hover:text-amber-900 dark:hover:text-amber-100 flex items-center gap-1"
      >
        <UiIcon name="arrow-uturn-left" class="w-4 h-4" />
        Back to editor
      </NuxtLink>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="flex justify-center items-center py-24">
      <UiLoadingSpinner class="h-8 w-8 text-gray-400" />
    </div>

    <!-- Error -->
    <div v-else-if="error" class="mx-auto max-w-4xl px-4 py-12">
      <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
        {{ error }}
      </div>
    </div>

    <!-- Article preview -->
    <article v-else-if="article" class="py-12">
      <div class="mx-auto max-w-4xl px-4 sm:px-6 lg:px-8">
        <!-- Header -->
        <header class="mb-8">
          <div class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400 mb-4">
            <span
              v-if="article.category"
              class="text-primary-600"
            >
              {{ article.category.name }}
            </span>
            <span v-if="article.category && article.publishedAt">·</span>
            <time v-if="article.publishedAt" :datetime="article.publishedAt">
              {{ formatDate(article.publishedAt) }}
            </time>
          </div>

          <h1 class="text-4xl font-bold text-gray-900 dark:text-gray-100 mb-6">
            {{ article.title }}
          </h1>

          <div v-if="article.excerpt" class="text-xl text-gray-600 dark:text-gray-400 mb-6">
            {{ article.excerpt }}
          </div>

          <div v-if="article.authorName || article.author" class="flex items-center gap-4">
            <div class="w-12 h-12 bg-gray-300 dark:bg-gray-600 rounded-full flex items-center justify-center">
              <span class="text-lg font-medium text-gray-600 dark:text-gray-400">
                {{ (article.authorName || article.author?.name || '?').charAt(0).toUpperCase() }}
              </span>
            </div>
            <div>
              <div class="font-medium text-gray-900 dark:text-gray-100">{{ article.authorName || article.author?.name }}</div>
            </div>
          </div>
        </header>

        <!-- Featured Image -->
        <div v-if="displayImageUrl" :class="['mb-8 rounded-lg overflow-hidden', featuredImageContainerClass]">
          <img
            :src="displayImageUrl"
            :alt="article.title"
            class="w-full h-auto"
          />
        </div>
        <p v-if="featuredImageFallbackMessage" class="mb-8 -mt-5 text-xs text-amber-700 dark:text-amber-300">
          {{ featuredImageFallbackMessage }}
        </p>

        <!-- Content: Layout mode or simple markdown -->
        <ArticleLayout
          v-if="article.layout"
          preview
          :layout="article.layout"
        />
        <div
          v-else
          class="prose prose-lg dark:prose-invert max-w-none prose-primary prose-headings:text-gray-900 dark:prose-headings:text-gray-100 prose-a:text-primary-600"
          v-html="sanitizeHtml(renderedContent)"
        />

        <!-- Tags -->
        <div v-if="article.tags.length" class="mt-8 pt-8 border-t border-gray-200 dark:border-gray-700">
          <div class="flex items-center gap-2 flex-wrap">
            <span class="text-gray-600 dark:text-gray-400 font-medium">Tags:</span>
            <span
              v-for="tag in article.tags"
              :key="tag.id"
              class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300"
            >
              {{ tag.name }}
            </span>
          </div>
        </div>
      </div>
    </article>
  </div>
</template>
