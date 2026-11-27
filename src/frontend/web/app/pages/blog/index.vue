<script setup lang="ts">
const route = useRoute()
const { getArticles } = useArticles()
const { t } = useI18n()

useBreadcrumbs().set([{ label: t('blog.title') }])

const page = computed(() => {
  const p = Number(route.query.page)
  return p > 0 ? p : 1
})

const { data: response, error } = await getArticles(page.value, 10)

const articles = computed(() => response.value?.data || [])
const meta = computed(() => response.value?.meta)

useSeoMeta({
  title: () => page.value > 1 ? t('blog.titleWithPage', { page: page.value }) : t('blog.title'),
  description: () => t('blog.description'),
  ogTitle: () => t('blog.title'),
  ogDescription: () => t('blog.description'),
  ogType: 'website'
})
</script>

<template>
  <div class="py-12">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ t('blog.title') }}</h1>
        <p class="mt-2 text-gray-600 dark:text-gray-400">{{ t('blog.subtitle') }}</p>
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
          <p class="text-gray-600 dark:text-gray-400">{{ t('blog.empty') }}</p>
        </div>

        <div v-if="meta && meta.totalPages > 1" class="mt-12">
          <Pagination
            :current-page="page"
            :total-pages="meta.totalPages"
            base-url="/blog"
          />
        </div>
      </template>
    </div>
  </div>
</template>
