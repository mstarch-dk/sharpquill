<script setup lang="ts">
import { getFeaturedImageContainerClass } from '#layer/utils/featuredImage'

const { getArticles } = useArticles()
const { getPageBySlug } = usePages()

const { data: heroResponse } = await getPageBySlug('hjem')
const { data: recentResponse } = await getArticles(1, 6)

const heroPage = computed(() => heroResponse.value?.data)
const recentArticles = computed(() => recentResponse.value?.data || [])
const heroDisplayImageUrl = computed(() =>
  heroPage.value?.featuredImageDisplayUrl || heroPage.value?.featuredImageMediumUrl || heroPage.value?.featuredImageUrl
)
const heroFeaturedImageContainerClass = computed(() =>
  getFeaturedImageContainerClass(heroPage.value?.featuredImageSize)
)

const { siteName, siteDescription } = useSiteSettings()
const { t } = useI18n()

useSeoMeta({
  title: heroPage.value?.metaTitle || heroPage.value?.title || siteName.value,
  description: heroPage.value?.metaDescription || siteDescription.value,
  ogTitle: heroPage.value?.title || siteName.value,
  ogDescription: heroPage.value?.metaDescription || siteDescription.value,
  ogType: 'website'
})
</script>

<template>
  <div>
    <!-- Page layout blocks (CMS-managed content) -->
    <div v-if="heroPage?.layout" class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-12">
      <div v-if="heroDisplayImageUrl" :class="['mb-8', heroFeaturedImageContainerClass]">
        <img
          :src="heroDisplayImageUrl"
          :alt="heroPage.title"
          class="w-full h-auto rounded-lg"
        />
      </div>
      <ArticleLayout :layout="heroPage.layout" />
    </div>

    <!-- Fallback hero when no layout blocks exist -->
    <section v-else class="bg-gradient-to-br from-primary-600 to-primary-800 text-white">
      <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-20">
        <h1 class="text-4xl md:text-5xl font-bold mb-6">
          {{ heroPage?.title || t('home.welcome') }}
        </h1>
        <p class="text-xl text-primary-100 max-w-2xl mb-8">
          {{ t('home.intro') }}
        </p>
        <NuxtLink
          to="/blog"
          class="inline-flex items-center px-6 py-3 bg-white text-primary-600 font-semibold rounded-lg hover:bg-primary-50 transition-colors"
        >
          {{ t('home.viewArticles') }}
          <UiIcon name="arrow-long-right" class="ml-2 w-5 h-5" />
        </NuxtLink>
      </div>
    </section>

    <!-- Recent Articles -->
    <section v-if="recentArticles.length" class="py-16 bg-gray-50 dark:bg-gray-900">
      <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div class="flex items-center justify-between mb-8">
          <h2 class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ t('home.latestArticles') }}</h2>
          <NuxtLink
            to="/blog"
            class="text-primary-600 hover:text-primary-700 font-medium flex items-center gap-1"
          >
            {{ t('home.viewAll') }}
            <UiIcon name="chevron-right" class="w-4 h-4" />
          </NuxtLink>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
          <ArticleCard
            v-for="article in recentArticles"
            :key="article.id"
            :article="article"
          />
        </div>
      </div>
    </section>

    <!-- Empty State -->
    <section v-if="!recentArticles.length && !heroPage?.layout" class="py-20">
      <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 text-center">
        <h2 class="text-2xl font-semibold text-gray-600 dark:text-gray-400 mb-4">{{ t('home.emptyTitle') }}</h2>
        <p class="text-gray-500 dark:text-gray-400">{{ t('home.emptyText') }}</p>
      </div>
    </section>
  </div>
</template>
