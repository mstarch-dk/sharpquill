<script setup lang="ts">
import { marked } from 'marked'
import { getFeaturedImageContainerClass } from '#layer/utils/featuredImage'

const route = useRoute()
const slug = route.params.slug as string

const { getPageBySlug } = usePages()
const { t } = useI18n()
const { data: response, error } = await getPageBySlug(slug)

const page = computed(() => response.value?.data)

if (error.value || !page.value) {
  throw createError({
    statusCode: 404,
    message: t('error.notFoundTitle')
  })
}

const renderedContent = computed(() => {
  if (!page.value?.content) return ''
  return marked.parse(page.value.content, { async: false })
})

const displayImageUrl = computed(() =>
  page.value?.featuredImageDisplayUrl || page.value?.featuredImageMediumUrl || page.value?.featuredImageUrl
)
const featuredImageContainerClass = computed(() =>
  getFeaturedImageContainerClass(page.value?.featuredImageSize)
)

useBreadcrumbs().set([{ label: page.value?.title || t('page.fallbackTitle') }])

useSeoMeta({
  title: () => page.value?.metaTitle || page.value?.title || t('page.fallbackTitle'),
  description: () => page.value?.metaDescription || page.value?.excerpt || '',
  ogTitle: () => page.value?.title || t('page.fallbackTitle'),
  ogDescription: () => page.value?.excerpt || '',
  ogImage: () => page.value?.featuredImageUrl || '',
  ogType: 'website'
})
</script>

<template>
  <div
    v-if="page"
    :class="['py-12', { 'pt-0': page.pageStyle && page.pageStyle !== 'default' }]"
  >

    <!-- hero-overlay: full-bleed image with title text overlaid -->
    <div
      v-if="page.pageStyle === 'hero-overlay' && displayImageUrl"
      class="relative mb-12 overflow-hidden"
      style="min-height: 480px;"
    >
      <img
        :src="displayImageUrl"
        :alt="page.title"
        class="absolute inset-0 w-full h-full object-cover"
      />
      <div class="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-black/10" />
      <div class="relative mx-auto max-w-4xl px-4 sm:px-6 lg:px-8 flex flex-col justify-end h-full pb-12 pt-24">
        <h1 class="text-4xl md:text-5xl font-bold text-white mb-4 drop-shadow-lg">{{ page.title }}</h1>
        <div v-if="page.excerpt" class="text-xl text-white/80 max-w-2xl">{{ page.excerpt }}</div>
      </div>
    </div>

    <!-- blurred-hero: featured image blurred as background behind header card -->
    <div
      v-else-if="page.pageStyle === 'blurred-hero' && displayImageUrl"
      class="relative mb-12 overflow-hidden"
      style="min-height: 380px;"
    >
      <img
        :src="displayImageUrl"
        :alt="page.title"
        class="absolute inset-0 w-full h-full object-cover scale-110"
        style="filter: blur(18px) saturate(0.7);"
      />
      <div class="absolute inset-0 bg-white/60 dark:bg-gray-900/70" />
      <div class="relative mx-auto max-w-4xl px-4 sm:px-6 lg:px-8 flex flex-col justify-center h-full py-16">
        <h1 class="text-4xl md:text-5xl font-bold text-gray-900 dark:text-gray-100 mb-4">{{ page.title }}</h1>
        <div v-if="page.excerpt" class="text-xl text-gray-600 dark:text-gray-300 max-w-2xl">{{ page.excerpt }}</div>
      </div>
    </div>

    <!-- gradient-header: primary gradient behind the header -->
    <div
      v-else-if="page.pageStyle === 'gradient-header'"
      class="relative mb-12 bg-gradient-to-br from-primary-600 via-primary-500 to-primary-700"
      style="min-height: 320px;"
    >
      <div class="mx-auto max-w-4xl px-4 sm:px-6 lg:px-8 flex flex-col justify-center h-full py-16">
        <h1 class="text-4xl md:text-5xl font-bold text-white mb-4">{{ page.title }}</h1>
        <div v-if="page.excerpt" class="text-xl text-white/80 max-w-2xl">{{ page.excerpt }}</div>
      </div>
    </div>

    <div class="mx-auto max-w-4xl px-4 sm:px-6 lg:px-8">
      <!-- Default header (shown when no hero style or unsupported style) -->
      <header
        v-if="!page.pageStyle || page.pageStyle === 'default' || (page.pageStyle === 'hero-overlay' && !displayImageUrl) || (page.pageStyle === 'blurred-hero' && !displayImageUrl)"
        class="mb-8"
      >
        <h1 class="text-4xl font-bold text-gray-900 dark:text-gray-100 mb-4">{{ page.title }}</h1>
        <div v-if="page.excerpt" class="text-xl text-gray-600 dark:text-gray-400">{{ page.excerpt }}</div>
      </header>

      <!-- Featured Image (default style only) -->
      <div
        v-if="displayImageUrl && (!page.pageStyle || page.pageStyle === 'default')"
        :class="['mb-8', featuredImageContainerClass]"
      >
        <img
          :src="displayImageUrl"
          :alt="page.title"
          class="w-full h-auto rounded-lg"
        />
      </div>

      <!-- Content: Layout mode or simple markdown -->
      <ArticleLayout
        v-if="page.layout"
        :layout="page.layout"
      />
      <div
        v-else
        class="prose prose-lg dark:prose-invert max-w-none prose-primary prose-headings:text-gray-900 dark:prose-headings:text-gray-100 prose-a:text-primary-600"
        v-html="sanitizeHtml(renderedContent)"
      />
    </div>
  </div>
</template>
