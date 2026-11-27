<script setup lang="ts">
const { getAllSeries } = useSeries()
const { t } = useI18n()
const { data: response, error } = await getAllSeries()

useBreadcrumbs().set([{ label: t('series.title') }])

const seriesList = computed(() => response.value?.data || [])

useSeoMeta({
  title: () => t('series.title'),
  description: () => t('series.description'),
  ogTitle: () => t('series.title'),
  ogDescription: () => t('series.description'),
  ogType: 'website'
})
</script>

<template>
  <div class="py-12">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ t('series.title') }}</h1>
        <p class="mt-2 text-gray-600 dark:text-gray-400">{{ t('series.subtitle') }}</p>
      </div>

      <div v-if="error" class="text-center py-12">
        <p class="text-red-600 dark:text-red-400">{{ t('series.loadError') }}</p>
      </div>

      <template v-else>
        <div v-if="seriesList.length" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
          <NuxtLink
            v-for="series in seriesList"
            :key="series.id"
            :to="`/series/${series.slug}`"
            class="bg-white dark:bg-gray-800 rounded-lg shadow-sm dark:shadow-gray-900/30 overflow-hidden hover:shadow-md dark:hover:shadow-gray-900/30 transition-shadow p-6"
          >
            <h2 class="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-2 hover:text-primary-600 transition-colors">
              {{ series.name }}
            </h2>
            <p v-if="series.description" class="text-gray-600 dark:text-gray-400 mb-4 line-clamp-3">
              {{ series.description }}
            </p>
            <span class="text-sm text-primary-600 font-medium">
              {{ t('common.articleCount', series.articleCount) }}
            </span>
          </NuxtLink>
        </div>

        <div v-else class="text-center py-12">
          <p class="text-gray-600 dark:text-gray-400">{{ t('series.empty') }}</p>
        </div>
      </template>
    </div>
  </div>
</template>
