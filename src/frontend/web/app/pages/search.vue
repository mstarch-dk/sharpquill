<script setup lang="ts">
import type { SearchSnippetPartDto } from '~/types/api'

const route = useRoute()
const { t } = useI18n()
const { formatDate } = useLocaleDate()

const query = computed(() => String(route.query.q ?? '').trim().slice(0, 200))
const page = computed(() => {
  const p = Number(route.query.page)
  return Number.isInteger(p) && p > 0 ? p : 1
})

const { data: response, error, status } = await useSearch(query, page)

const results = computed(() => response.value?.data || [])
const meta = computed(() => response.value?.meta)
const totalCount = computed(() => meta.value?.totalCount || 0)

// A match-free snippet still shows the start of the article; fall back to the excerpt when the body is empty.
function snippetOf(parts: SearchSnippetPartDto[], excerpt?: string): SearchSnippetPartDto[] {
  return parts.length ? parts : (excerpt ? [{ text: excerpt, match: false }] : [])
}

useSeoMeta({
  title: () => query.value ? t('search.titleWithQuery', { query: query.value }) : t('search.title'),
  description: () => t('search.description'),
  ogTitle: () => t('search.title'),
  ogType: 'website',
  // Result pages are endless variations of the same content: keep them out of search engines, follow the links.
  robots: 'noindex, follow'
})

useBreadcrumbs().set([{ label: t('search.title') }])
</script>

<template>
  <div class="py-12">
    <div class="mx-auto max-w-3xl px-4 sm:px-6 lg:px-8">
      <h1 class="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-6">{{ t('search.title') }}</h1>

      <SearchBox block :autofocus="!query" class="mb-8" />

      <p v-if="!query" class="text-gray-600 dark:text-gray-400">{{ t('search.prompt') }}</p>

      <div v-else-if="error" class="text-center py-12">
        <p class="text-red-600 dark:text-red-400">{{ t('search.error') }}</p>
      </div>

      <template v-else>
        <p class="text-sm text-gray-600 dark:text-gray-400 mb-6" aria-live="polite">
          <template v-if="status === 'pending' && !results.length">{{ t('search.searching') }}</template>
          <template v-else-if="totalCount">{{ t('search.resultCount', { n: totalCount, query }, totalCount) }}</template>
        </p>

        <ol v-if="results.length" class="space-y-8">
          <li v-for="result in results" :key="result.article.id">
            <article>
              <h2 class="text-xl font-semibold">
                <NuxtLink
                  :to="`/blog/${result.article.slug}`"
                  class="text-gray-900 dark:text-gray-100 hover:text-primary-600 dark:hover:text-primary-400"
                >
                  {{ result.article.title }}
                </NuxtLink>
              </h2>
              <div class="mt-1 flex flex-wrap items-center gap-x-2 text-sm text-gray-500 dark:text-gray-400">
                <time v-if="result.article.publishedAt" :datetime="result.article.publishedAt">
                  {{ formatDate(result.article.publishedAt) }}
                </time>
                <template v-if="result.article.category">
                  <span aria-hidden="true">·</span>
                  <NuxtLink :to="`/category/${result.article.category.slug}`" class="text-primary-600 hover:text-primary-700">
                    {{ result.article.category.name }}
                  </NuxtLink>
                </template>
                <span v-if="result.article.requiresSignIn" class="inline-flex items-center gap-1">
                  <span aria-hidden="true">·</span>
                  <UiIcon name="lock-closed" class="w-3.5 h-3.5" />
                  {{ t('search.membersOnly') }}
                </span>
              </div>
              <p class="mt-2 text-gray-700 dark:text-gray-300 leading-relaxed">
                <template v-for="(part, index) in snippetOf(result.snippet, result.article.excerpt)" :key="index">
                  <mark v-if="part.match" class="bg-primary-100 dark:bg-primary-900/60 text-inherit rounded-sm px-0.5">{{ part.text }}</mark>
                  <template v-else>{{ part.text }}</template>
                </template>
              </p>
            </article>
          </li>
        </ol>

        <div v-else-if="status !== 'pending'" class="py-8">
          <p class="text-gray-700 dark:text-gray-300 font-medium">{{ t('search.noResults', { query }) }}</p>
          <p class="mt-2 text-gray-600 dark:text-gray-400">{{ t('search.noResultsHint') }}</p>
        </div>

        <div v-if="meta && meta.totalPages > 1" class="mt-12">
          <Pagination
            :current-page="page"
            :total-pages="meta.totalPages"
            :base-url="`/search?q=${encodeURIComponent(query)}`"
          />
        </div>
      </template>
    </div>
  </div>
</template>
