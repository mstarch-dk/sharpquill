<script setup lang="ts">
const route = useRoute()
const slug = route.params.slug as string

const page = computed(() => {
  const p = Number(route.query.page)
  return p > 0 ? p : 1
})

const { getCategories, getCategoryArticles } = useCategories()
const { t } = useI18n()

const { data: categoriesResponse } = await getCategories()
const categories = computed(() => categoriesResponse.value?.data || [])
const category = computed(() => categories.value.find(c => c.slug === slug))

if (!category.value) {
  throw createError({
    statusCode: 404,
    message: t('category.notFound')
  })
}

const { data: articlesResponse, error } = await getCategoryArticles(slug, page.value, 10)

const articles = computed(() => articlesResponse.value?.data || [])
const meta = computed(() => articlesResponse.value?.meta)

useSeoMeta({
  title: () => t('category.title', { name: category.value?.name || t('category.fallbackName') }),
  description: () => category.value?.description || t('category.description', { name: category.value?.name }),
  ogTitle: () => t('category.title', { name: category.value?.name || t('category.fallbackName') }),
  ogDescription: () => category.value?.description || t('category.description', { name: category.value?.name }),
  ogType: 'website'
})

useFeedLinks('category', slug, () => category.value?.name || t('category.fallbackName'))

useBreadcrumbs().set([
  { label: t('blog.title'), href: '/blog' },
  { label: category.value?.name || t('category.fallbackName') }
])
</script>

<template>
  <div class="py-12">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ category?.name }}</h1>
        <p v-if="category?.description" class="mt-2 text-gray-600 dark:text-gray-400">{{ category.description }}</p>
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
          <p class="text-gray-600 dark:text-gray-400">{{ t('category.empty') }}</p>
        </div>

        <div v-if="meta && meta.totalPages > 1" class="mt-12">
          <Pagination
            :current-page="page"
            :total-pages="meta.totalPages"
            :base-url="`/category/${slug}`"
          />
        </div>
      </template>
    </div>
  </div>
</template>
