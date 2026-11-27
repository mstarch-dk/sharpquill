<script setup lang="ts">
const route = useRoute()
const slug = route.params.slug as string

const page = computed(() => {
  const p = Number(route.query.page)
  return p > 0 ? p : 1
})

const { getTags, getTagArticles } = useTags()
const { t } = useI18n()

const { data: tagsResponse } = await getTags()
const tags = computed(() => tagsResponse.value?.data || [])
const tag = computed(() => tags.value.find(t => t.slug === slug))

if (!tag.value) {
  throw createError({
    statusCode: 404,
    message: t('tag.notFound')
  })
}

const { data: articlesResponse, error } = await getTagArticles(slug, page.value, 10)

const articles = computed(() => articlesResponse.value?.data || [])
const meta = computed(() => articlesResponse.value?.meta)

useSeoMeta({
  title: () => t('tag.title', { name: tag.value?.name || t('tag.fallbackName') }),
  description: () => t('tag.description', { name: tag.value?.name }),
  ogTitle: () => t('tag.title', { name: tag.value?.name || t('tag.fallbackName') }),
  ogDescription: () => t('tag.description', { name: tag.value?.name }),
  ogType: 'website'
})

useFeedLinks('tag', slug, () => tag.value?.name || t('tag.fallbackName'))

useBreadcrumbs().set([
  { label: t('blog.title'), href: '/blog' },
  { label: `#${tag.value?.name || t('tag.fallbackName')}` }
])
</script>

<template>
  <div class="py-12">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100">
          <span class="text-gray-500 dark:text-gray-400">#</span>{{ tag?.name }}
        </h1>
        <p class="mt-2 text-gray-600 dark:text-gray-400">
          {{ t('tag.articleCount', meta?.totalCount || 0) }}
        </p>
      </div>

      <div v-if="error" class="text-center py-12">
        <p class="text-red-600 dark:text-red-400">{{ t('common.loadArticlesError') }}</p>
      </div>

      <template v-else>
        <div v-if="articles.length" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
          <ArticleCard
            v-for="article in articles"
            :key="article.id"
            :article="article"
          />
        </div>

        <div v-else class="text-center py-12">
          <p class="text-gray-600 dark:text-gray-400">{{ t('tag.empty') }}</p>
        </div>

        <div v-if="meta && meta.totalPages > 1" class="mt-12">
          <Pagination
            :current-page="page"
            :total-pages="meta.totalPages"
            :base-url="`/tag/${slug}`"
          />
        </div>
      </template>
    </div>
  </div>
</template>
