<script setup lang="ts">
import type { ArticleSeriesInfoDto } from '~/types/api'

defineProps<{
  series: ArticleSeriesInfoDto
}>()
</script>

<template>
  <div class="bg-primary-50 border border-primary-100 rounded-lg p-6">
    <div class="text-sm text-primary-700 mb-3">
      <i18n-t keypath="series.partOf" tag="span" scope="global">
        <template #current>{{ series.currentOrder }}</template>
        <template #total>{{ series.totalInSeries }}</template>
        <template #series>
          <NuxtLink
            :to="`/series/${series.seriesSlug}`"
            class="font-medium text-primary-800 hover:text-primary-900 underline"
          >
            {{ series.seriesName }}
          </NuxtLink>
        </template>
      </i18n-t>
    </div>

    <div class="flex items-center justify-between gap-4">
      <NuxtLink
        v-if="series.previous"
        :to="`/blog/${series.previous.slug}`"
        class="flex items-center gap-2 text-sm text-primary-700 hover:text-primary-900 min-w-0"
      >
        <UiIcon name="arrow-long-left" class="w-4 h-4 flex-shrink-0" />
        <span class="truncate">{{ series.previous.title }}</span>
      </NuxtLink>
      <div v-else />

      <NuxtLink
        v-if="series.next"
        :to="`/blog/${series.next.slug}`"
        class="flex items-center gap-2 text-sm text-primary-700 hover:text-primary-900 min-w-0 text-right"
      >
        <span class="truncate">{{ series.next.title }}</span>
        <UiIcon name="arrow-long-right" class="w-4 h-4 flex-shrink-0" />
      </NuxtLink>
      <div v-else />
    </div>
  </div>
</template>
