<script setup lang="ts">
import { marked } from 'marked'
import { getFeaturedImageContainerClass } from '#layer/utils/featuredImage'
import type { ArticleDetailDto, ApiResponse } from '~/types/api'

const route = useRoute()
const slug = route.params.slug as string

const { getArticleBySlug } = useArticles()
const { t } = useI18n()
const { formatDate } = useLocaleDate()
const { isAuthenticated, initAuth, authFetch } = useWebsiteAuth()
const config = useRuntimeConfig()

const { data: response, error } = await getArticleBySlug(slug)

const article = ref<ArticleDetailDto | undefined>(response.value?.data)
const signInRequired = ref(false)

// Check if SSR got a 401 SIGN_IN_REQUIRED
if (error.value) {
  const fetchError = error.value as { statusCode?: number; status?: number; data?: { error?: { code?: string } } }
  const statusCode = fetchError?.statusCode || fetchError?.status
  const errorData = fetchError?.data
  const errorCode = errorData?.error?.code

  if (statusCode === 401 && errorCode === 'SIGN_IN_REQUIRED') {
    signInRequired.value = true
  } else if (!article.value) {
    throw createError({
      statusCode: 404,
      message: t('article.notFound')
    })
  }
}

if (!article.value && !signInRequired.value) {
  throw createError({
    statusCode: 404,
    message: t('article.notFound')
  })
}

// Client-side: if sign-in required and user is authenticated, re-fetch with auth header
onMounted(async () => {
  await initAuth()

  if (signInRequired.value && isAuthenticated.value) {
    try {
      const apiBase = config.public.apiBase
      const data = await authFetch<ApiResponse<ArticleDetailDto>>(`${apiBase}/api/v1/articles/${slug}`)
      if (data.data) {
        article.value = data.data
        signInRequired.value = false
      }
    } catch {
      // Still can't access - keep gate UI
    }
  }
})

const renderedContent = computed(() => {
  if (!article.value?.content) return ''
  return marked.parse(article.value.content, { async: false })
})

const displayAuthorName = computed(() =>
  article.value?.authorName || article.value?.author?.name || null
)

const displayImageUrl = computed(() =>
  article.value?.featuredImageDisplayUrl || article.value?.featuredImageMediumUrl || article.value?.featuredImageUrl
)
const featuredImageContainerClass = computed(() =>
  getFeaturedImageContainerClass(article.value?.featuredImageSize)
)

useBreadcrumbs().set([
  { label: t('blog.title'), href: '/blog' },
  { label: article.value?.title || t('article.fallbackTitle') }
])

useSeoMeta({
  title: () => article.value?.metaTitle || article.value?.title || t('article.fallbackTitle'),
  description: () => article.value?.metaDescription || article.value?.excerpt || '',
  ogTitle: () => article.value?.title || t('article.fallbackTitle'),
  ogDescription: () => article.value?.excerpt || '',
  ogType: 'article',
  ogImage: () => article.value?.featuredImageUrl || undefined,
  articlePublishedTime: () => article.value?.publishedAt || undefined,
  articleAuthor: () => {
    const name = article.value?.authorName || article.value?.author?.name
    return name ? [name] : undefined
  }
})
</script>

<template>
  <!-- Sign-in required gate -->
  <div v-if="signInRequired" class="py-16">
    <div class="mx-auto max-w-md px-4 sm:px-6 lg:px-8 text-center">
      <div class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8">
        <UiIcon name="lock-closed" class="w-16 h-16 mx-auto text-gray-400 dark:text-gray-500 mb-4" />
        <h2 class="text-xl font-bold text-gray-900 dark:text-gray-100 mb-2">{{ t('article.signInRequiredTitle') }}</h2>
        <p class="text-gray-600 dark:text-gray-400 mb-6">
          {{ t('article.signInRequiredText') }}
        </p>
        <div class="flex flex-col gap-3">
          <NuxtLink
            to="/login"
            class="block w-full bg-primary-600 text-white px-4 py-2 rounded-md hover:bg-primary-700 transition-colors text-center"
          >
            {{ t('common.logIn') }}
          </NuxtLink>
          <NuxtLink
            to="/signup"
            class="block w-full border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 px-4 py-2 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors text-center"
          >
            {{ t('common.signUp') }}
          </NuxtLink>
        </div>
        <NuxtLink
          to="/blog"
          class="inline-flex items-center mt-6 text-sm text-primary-600 hover:text-primary-700"
        >
          <UiIcon name="arrow-long-left" class="mr-1 w-4 h-4" />
          {{ t('common.backToBlog') }}
        </NuxtLink>
      </div>
    </div>
  </div>

  <!-- Article content -->
  <article
    v-else-if="article"
    :class="['py-12', { 'pt-0': article.pageStyle && article.pageStyle !== 'default' }]"
  >

    <!-- hero-overlay: full-bleed image with title text overlaid -->
    <div
      v-if="article.pageStyle === 'hero-overlay' && displayImageUrl"
      class="relative mb-12 overflow-hidden"
      style="min-height: 480px;"
    >
      <img
        :src="displayImageUrl"
        :alt="article.title"
        class="absolute inset-0 w-full h-full object-cover"
      />
      <div class="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-black/10" />
      <div class="relative mx-auto max-w-4xl px-4 sm:px-6 lg:px-8 flex flex-col justify-end h-full pb-12 pt-24">
        <div class="flex items-center gap-2 text-sm text-white/70 mb-4">
          <NuxtLink v-if="article.category" :to="`/category/${article.category.slug}`" class="text-white/90 hover:text-white">{{ article.category.name }}</NuxtLink>
          <span v-if="article.category && article.publishedAt">·</span>
          <time v-if="article.publishedAt" :datetime="article.publishedAt">{{ formatDate(article.publishedAt) }}</time>
        </div>
        <h1 class="text-4xl md:text-5xl font-bold text-white mb-4 drop-shadow-lg">{{ article.title }}</h1>
        <div v-if="article.excerpt" class="text-xl text-white/80 mb-6 max-w-2xl">{{ article.excerpt }}</div>
        <div v-if="displayAuthorName" class="flex items-center gap-3">
          <div class="w-10 h-10 bg-white/20 rounded-full flex items-center justify-center">
            <span class="text-base font-medium text-white">{{ displayAuthorName.charAt(0).toUpperCase() }}</span>
          </div>
          <div class="text-white/80 text-sm">{{ displayAuthorName }}</div>
        </div>
      </div>
    </div>

    <!-- blurred-hero: featured image blurred as background behind header card -->
    <div
      v-else-if="article.pageStyle === 'blurred-hero' && displayImageUrl"
      class="relative mb-12 overflow-hidden"
      style="min-height: 380px;"
    >
      <img
        :src="displayImageUrl"
        :alt="article.title"
        class="absolute inset-0 w-full h-full object-cover scale-110"
        style="filter: blur(18px) saturate(0.7);"
      />
      <div class="absolute inset-0 bg-white/60 dark:bg-gray-900/70" />
      <div class="relative mx-auto max-w-4xl px-4 sm:px-6 lg:px-8 flex flex-col justify-center h-full py-16">
        <div class="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 mb-4">
          <NuxtLink v-if="article.category" :to="`/category/${article.category.slug}`" class="text-primary-600 hover:text-primary-700">{{ article.category.name }}</NuxtLink>
          <span v-if="article.category && article.publishedAt">·</span>
          <time v-if="article.publishedAt" :datetime="article.publishedAt">{{ formatDate(article.publishedAt) }}</time>
        </div>
        <h1 class="text-4xl md:text-5xl font-bold text-gray-900 dark:text-gray-100 mb-4">{{ article.title }}</h1>
        <div v-if="article.excerpt" class="text-xl text-gray-600 dark:text-gray-300 mb-6 max-w-2xl">{{ article.excerpt }}</div>
        <div v-if="displayAuthorName" class="flex items-center gap-3">
          <div class="w-10 h-10 bg-gray-300 dark:bg-gray-600 rounded-full flex items-center justify-center">
            <span class="text-base font-medium text-gray-700 dark:text-gray-300">{{ displayAuthorName.charAt(0).toUpperCase() }}</span>
          </div>
          <div class="text-sm text-gray-700 dark:text-gray-300">{{ displayAuthorName }}</div>
        </div>
      </div>
    </div>

    <!-- gradient-header: primary gradient behind the header -->
    <div
      v-else-if="article.pageStyle === 'gradient-header'"
      class="relative mb-12 bg-gradient-to-br from-primary-600 via-primary-500 to-primary-700"
      style="min-height: 320px;"
    >
      <div class="mx-auto max-w-4xl px-4 sm:px-6 lg:px-8 flex flex-col justify-center h-full py-16">
        <div class="flex items-center gap-2 text-sm text-white/70 mb-4">
          <NuxtLink v-if="article.category" :to="`/category/${article.category.slug}`" class="text-white/90 hover:text-white">{{ article.category.name }}</NuxtLink>
          <span v-if="article.category && article.publishedAt">·</span>
          <time v-if="article.publishedAt" :datetime="article.publishedAt">{{ formatDate(article.publishedAt) }}</time>
        </div>
        <h1 class="text-4xl md:text-5xl font-bold text-white mb-4">{{ article.title }}</h1>
        <div v-if="article.excerpt" class="text-xl text-white/80 mb-6 max-w-2xl">{{ article.excerpt }}</div>
        <div v-if="displayAuthorName" class="flex items-center gap-3">
          <div class="w-10 h-10 bg-white/20 rounded-full flex items-center justify-center">
            <span class="text-base font-medium text-white">{{ displayAuthorName.charAt(0).toUpperCase() }}</span>
          </div>
          <div class="text-white/80 text-sm">{{ displayAuthorName }}</div>
        </div>
      </div>
    </div>

    <div class="mx-auto max-w-4xl px-4 sm:px-6 lg:px-8">
      <!-- Default header (shown when no hero style or unsupported style) -->
      <header
        v-if="!article.pageStyle || article.pageStyle === 'default' || (article.pageStyle === 'hero-overlay' && !displayImageUrl) || (article.pageStyle === 'blurred-hero' && !displayImageUrl)"
        class="mb-8"
      >
        <div class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400 mb-4">
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

        <h1 class="text-4xl font-bold text-gray-900 dark:text-gray-100 mb-6">
          {{ article.title }}
        </h1>

        <div v-if="article.excerpt" class="text-xl text-gray-600 dark:text-gray-400 mb-6">
          {{ article.excerpt }}
        </div>

        <div v-if="displayAuthorName" class="flex items-center gap-4">
          <div class="w-12 h-12 bg-gray-300 dark:bg-gray-600 rounded-full flex items-center justify-center">
            <span class="text-lg font-medium text-gray-600 dark:text-gray-400">
              {{ displayAuthorName.charAt(0).toUpperCase() }}
            </span>
          </div>
          <div>
            <div class="font-medium text-gray-900 dark:text-gray-100">{{ displayAuthorName }}</div>
            <div class="text-sm text-gray-500 dark:text-gray-400">{{ t('article.author') }}</div>
          </div>
        </div>
      </header>

      <!-- Series Navigation (top) -->
      <SeriesNavigation
        v-if="article.series"
        :series="article.series"
        class="mb-8"
      />

      <!-- Featured Image (default style only) -->
      <div
        v-if="displayImageUrl && (!article.pageStyle || article.pageStyle === 'default')"
        :class="['mb-8 rounded-lg overflow-hidden', featuredImageContainerClass]"
      >
        <img
          :src="displayImageUrl"
          :alt="article.title"
          class="w-full h-auto"
        />
      </div>

      <!-- Content: Layout mode or simple markdown -->
      <ArticleLayout
        v-if="article.layout"
        :layout="article.layout"
      />
      <div
        v-else
        class="prose prose-lg dark:prose-invert max-w-none prose-primary prose-headings:text-gray-900 dark:prose-headings:text-gray-100 prose-a:text-primary-600"
        v-html="sanitizeHtml(renderedContent)"
      />

      <!-- Series Navigation (bottom) -->
      <SeriesNavigation
        v-if="article.series"
        :series="article.series"
        class="mt-8"
      />

      <!-- Tags -->
      <div v-if="article.tags.length" class="mt-8 pt-8 border-t border-gray-200 dark:border-gray-700">
        <div class="flex items-center gap-2 flex-wrap">
          <span class="text-gray-600 dark:text-gray-400 font-medium">{{ t('article.tags') }}</span>
          <TagBadge
            v-for="tag in article.tags"
            :key="tag.id"
            :tag="tag"
          />
        </div>
      </div>

      <!-- Like Button -->
      <div class="mt-6">
        <LikeButton :article-id="article.id" />
      </div>

      <!-- Comment Section -->
      <CommentSection
        v-if="article.commentsEnabled"
        :article-id="article.id"
      />

      <!-- Back to Blog -->
      <div class="mt-12">
        <NuxtLink
          to="/blog"
          class="inline-flex items-center text-primary-600 hover:text-primary-700"
        >
          <UiIcon name="arrow-long-left" class="mr-2 w-5 h-5" />
          {{ t('common.backToBlog') }}
        </NuxtLink>
      </div>
    </div>
  </article>
</template>
