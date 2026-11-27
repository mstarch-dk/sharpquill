<script setup lang="ts">
const route = useRoute()
const slug = route.params.slug as string

const { getSeriesBySlug } = useSeries()
const { t } = useI18n()
const { formatDate } = useLocaleDate()
const { data: response, error } = await getSeriesBySlug(slug)

const series = computed(() => response.value?.data)

if (error.value || !series.value) {
  throw createError({
    statusCode: 404,
    message: t('series.notFound')
  })
}

// Apply series-specific theme override if set
const { applyTheme, getThemeBySlug: resolveTheme, fetchSiteSettings } = useTheme()
if (series.value?.themeSlug) {
  const settings = await fetchSiteSettings()
  const seriesTheme = resolveTheme(series.value.themeSlug)
  applyTheme(seriesTheme, settings.darkMode, settings.globalFont)
}

// Restore site theme when leaving the page
onBeforeUnmount(() => {
  if (series.value?.themeSlug) {
    fetchSiteSettings().then(settings => {
      const siteTheme = resolveTheme(settings.themeSlug)
      applyTheme(siteTheme, settings.darkMode, settings.globalFont)
    })
  }
})

useSeoMeta({
  title: () => series.value?.name || t('series.fallbackName'),
  description: () => series.value?.description || t('series.detailDescription', { name: series.value?.name }),
  ogTitle: () => series.value?.name || t('series.fallbackName'),
  ogDescription: () => series.value?.description || '',
  ogType: 'website'
})

useFeedLinks('series', slug, () => series.value?.name || t('series.fallbackName'))

useBreadcrumbs().set([
  { label: t('series.title'), href: '/series' },
  { label: series.value?.name || t('series.fallbackName') }
])
</script>

<template>
  <div v-if="series" class="py-12">
    <div class="mx-auto max-w-4xl px-4 sm:px-6 lg:px-8">
      <header class="mb-8">
        <h1 class="text-4xl font-bold text-gray-900 dark:text-gray-100 mb-4">
          {{ series.name }}
        </h1>
        <p v-if="series.description" class="text-xl text-gray-600 dark:text-gray-400">
          {{ series.description }}
        </p>
        <p class="mt-2 text-sm text-gray-500 dark:text-gray-400">
          {{ t('common.articleCount', series.articles.length) }}
        </p>
      </header>

      <div class="space-y-4">
        <NuxtLink
          v-for="article in series.articles"
          :key="article.id"
          :to="`/blog/${article.slug}`"
          class="block bg-white dark:bg-gray-800 rounded-lg shadow-sm dark:shadow-gray-900/30 hover:shadow-md dark:hover:shadow-gray-900/30 transition-shadow p-6"
        >
          <div class="flex items-start gap-4">
            <span class="flex-shrink-0 w-8 h-8 bg-primary-100 text-primary-700 rounded-full flex items-center justify-center text-sm font-medium">
              {{ article.order }}
            </span>
            <div class="min-w-0">
              <h2 class="text-lg font-semibold text-gray-900 dark:text-gray-100 hover:text-primary-600 transition-colors">
                {{ article.title }}
              </h2>
              <p v-if="article.excerpt" class="mt-1 text-gray-600 dark:text-gray-400 line-clamp-2">
                {{ article.excerpt }}
              </p>
              <time v-if="article.publishedAt" :datetime="article.publishedAt" class="mt-2 block text-sm text-gray-500 dark:text-gray-400">
                {{ formatDate(article.publishedAt) }}
              </time>
            </div>
          </div>
        </NuxtLink>
      </div>

    </div>
  </div>
</template>
