<script setup lang="ts">
import type { ArticleListDto } from '~/types/api'

const props = defineProps<{
  article: ArticleListDto
}>()

const { formatDate } = useLocaleDate()

const displayAuthorName = computed(() => props.article.authorName || props.article.author?.name || null)
</script>

<template>
  <article class="bg-white dark:bg-gray-800 rounded-lg shadow-sm dark:shadow-gray-900/30 overflow-hidden hover:shadow-md dark:hover:shadow-gray-900/30 transition-shadow">
    <NuxtLink :to="`/blog/${article.slug}`">
      <div v-if="article.featuredImageUrl" class="aspect-video bg-gray-200 dark:bg-gray-600">
        <img
          :src="article.featuredImageThumbnailUrl || article.featuredImageUrl"
          :alt="article.title"
          class="w-full h-full object-cover"
        />
      </div>
      <div v-else class="aspect-video bg-gradient-to-br from-primary-400 to-primary-600" />
    </NuxtLink>
    <div class="p-6">
      <div class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400 mb-3">
        <NuxtLink
          v-if="article.category"
          :to="`/category/${article.category.slug}`"
          class="text-primary-600 hover:text-primary-700"
        >
          {{ article.category.name }}
        </NuxtLink>
        <span v-if="article.category && article.publishedAt">·</span>
        <time v-if="article.publishedAt" :datetime="article.publishedAt">
          {{ formatDate(article.publishedAt) }}
        </time>
      </div>
      <NuxtLink :to="`/blog/${article.slug}`">
        <h2 class="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-2 hover:text-primary-600 transition-colors">
          {{ article.title }}
        </h2>
      </NuxtLink>
      <p v-if="article.excerpt" class="text-gray-600 dark:text-gray-400 mb-4 line-clamp-3">
        {{ article.excerpt }}
      </p>
      <div class="flex items-center justify-between">
        <div v-if="displayAuthorName" class="flex items-center gap-2">
          <div class="w-8 h-8 bg-gray-300 dark:bg-gray-600 rounded-full flex items-center justify-center">
            <span class="text-sm font-medium text-gray-600 dark:text-gray-400">
              {{ displayAuthorName.charAt(0).toUpperCase() }}
            </span>
          </div>
          <span class="text-sm text-gray-600 dark:text-gray-400">{{ displayAuthorName }}</span>
        </div>
        <div v-if="article.tags.length" class="flex gap-2">
          <TagBadge
            v-for="tag in article.tags.slice(0, 2)"
            :key="tag.id"
            :tag="tag"
            size="sm"
          />
        </div>
      </div>
    </div>
  </article>
</template>
