<script setup lang="ts">
import type { CategoryDto, TagDto } from '~/types/api'
import type { ArticleAdminDto, ArticleUpdateDto } from '~/types/article'

definePageMeta({
  middleware: 'auth'
})

const route = useRoute()
const router = useRouter()
const { getArticle, updateArticle } = useAdminArticles()
const { getCategories } = useAdminCategories()
const { getTags } = useAdminTags()

const articleId = computed(() => Number(route.params.id))

// Data state
const article = ref<ArticleAdminDto | null>(null)
const categories = ref<CategoryDto[]>([])
const tags = ref<TagDto[]>([])
const isLoading = ref(true)
const isSaving = ref(false)
const error = ref('')
const notFound = ref(false)

// Tab state
const activeTab = ref<'content' | 'versions'>('content')

// Form ref for dirty tracking + auto-save
const formRef = ref<InstanceType<typeof import('~/components/articles/ArticleForm.vue').default> | null>(null)

const isDirty = computed(() => formRef.value?.isDirty ?? false)
const isOpeningPreview = ref(false)

// Unsaved changes protection
useUnsavedChanges(isDirty)

// Auto-save
const autoSave = useAutoSave(async () => {
  if (!formRef.value || !article.value) return
  const data = formRef.value.getFormData() as ArticleUpdateDto
  const response = await updateArticle(articleId.value, data)
  article.value = response.data
  formRef.value.markClean()
})

function handleFormChange() {
  autoSave.notifyChange()
}

useHead({
  title: computed(() => article.value ? `Edit: ${article.value.title}` : 'Edit Article')
})

// Fetch data
onMounted(async () => {
  try {
    const [articleResponse, categoriesResponse, tagsResponse] = await Promise.all([
      getArticle(articleId.value),
      getCategories(),
      getTags()
    ])

    article.value = articleResponse.data
    categories.value = categoriesResponse.data
    tags.value = tagsResponse.data
  } catch (e: unknown) {
    const fetchError = e as { status?: number }
    if (fetchError.status === 404) {
      notFound.value = true
    } else {
      error.value = 'Failed to load article'
    }
  } finally {
    isLoading.value = false
  }
})

// Handle form submission (manual save)
async function handleSubmit(data: ArticleUpdateDto) {
  isSaving.value = true
  error.value = ''
  autoSave.pause()

  try {
    const response = await updateArticle(articleId.value, data)
    article.value = response.data
    formRef.value?.markClean()
    router.push('/articles')
  } catch (e: unknown) {
    const apiError = e as { data?: { error?: { message?: string } } }
    error.value = apiError.data?.error?.message || 'Failed to update article'
  } finally {
    isSaving.value = false
    autoSave.resume()
  }
}

function handleCancel() {
  router.push('/articles')
}

async function handlePreview() {
  if (isOpeningPreview.value) return
  isOpeningPreview.value = true
  try {
    if (isDirty.value && formRef.value && article.value) {
      const data = formRef.value.getFormData() as ArticleUpdateDto
      const response = await updateArticle(articleId.value, data)
      article.value = response.data
      formRef.value.markClean()
    }
    window.open(`/articles/${articleId.value}/preview`, '_blank')
  } finally {
    isOpeningPreview.value = false
  }
}

async function handleVersionPublished() {
  // Reload article to get updated publishedVersionId
  try {
    const response = await getArticle(articleId.value)
    article.value = response.data
  } catch {
    // Ignore - version list will still be visible
  }
}
</script>

<template>
  <div>
    <!-- Breadcrumb -->
    <nav class="mb-6">
      <ol class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
        <li>
          <NuxtLink to="/articles" class="hover:text-gray-700 dark:hover:text-gray-300">Articles</NuxtLink>
        </li>
        <li>/</li>
        <li class="text-gray-900 dark:text-gray-100">Edit</li>

        <!-- Preview button + Auto-save / unsaved status indicator -->
        <li v-if="article" class="ml-auto flex items-center gap-3">
          <button
            type="button"
            class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-gray-600 dark:text-gray-400 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
            :disabled="isOpeningPreview"
            @click="handlePreview"
          >
            <UiIcon name="eye" class="w-3.5 h-3.5" />
            Preview
          </button>
        </li>
        <li v-if="article" class="flex items-center gap-1.5 text-xs">
          <!-- Saving -->
          <template v-if="autoSave.status.value === 'saving'">
            <UiLoadingSpinner class="h-3.5 w-3.5 text-gray-400" />
            <span class="text-gray-400">Saving...</span>
          </template>
          <!-- Saved -->
          <template v-else-if="autoSave.status.value === 'saved'">
            <UiIcon name="check" class="h-3.5 w-3.5 text-green-500" />
            <span class="text-green-600 dark:text-green-400">Saved</span>
          </template>
          <!-- Error -->
          <template v-else-if="autoSave.status.value === 'error'">
            <UiIcon name="warning" class="h-3.5 w-3.5 text-red-500" />
            <span class="text-red-600 dark:text-red-400">Save failed</span>
          </template>
          <!-- Unsaved changes (dirty, but not currently saving) -->
          <template v-else-if="isDirty">
            <span class="relative flex h-2 w-2">
              <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-amber-400 opacity-75" />
              <span class="relative inline-flex rounded-full h-2 w-2 bg-amber-500" />
            </span>
            <span class="text-amber-600 dark:text-amber-400">Unsaved changes</span>
          </template>
        </li>
      </ol>
    </nav>

    <!-- Loading -->
    <div v-if="isLoading" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600" />
      <p class="mt-2 text-gray-500 dark:text-gray-400">Loading article...</p>
    </div>

    <!-- Not found -->
    <div v-else-if="notFound" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <UiIcon name="face-frown" class="mx-auto h-12 w-12 text-gray-400 dark:text-gray-500" />
      <h2 class="mt-4 text-lg font-medium text-gray-900 dark:text-gray-100">Article not found</h2>
      <p class="mt-2 text-gray-500 dark:text-gray-400">The article you're looking for doesn't exist.</p>
      <NuxtLink to="/articles" class="mt-4 inline-block text-primary-600 hover:text-primary-700">
        Back to articles
      </NuxtLink>
    </div>

    <!-- Error -->
    <div v-else-if="error && !article" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <!-- Content with tabs -->
    <template v-else-if="article">
      <div v-if="error" class="mb-6 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
        {{ error }}
      </div>

      <!-- Tabs -->
      <div class="mb-6 border-b border-gray-200 dark:border-gray-700">
        <nav class="flex gap-6" aria-label="Tabs">
          <button
            class="pb-3 text-sm font-medium border-b-2 transition-colors"
            :class="activeTab === 'content'
              ? 'border-primary-600 text-primary-600'
              : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600'"
            @click="activeTab = 'content'"
          >
            Content
          </button>
          <button
            class="pb-3 text-sm font-medium border-b-2 transition-colors flex items-center gap-2"
            :class="activeTab === 'versions'
              ? 'border-primary-600 text-primary-600'
              : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600'"
            @click="activeTab = 'versions'"
          >
            Versions
            <span
              v-if="article.totalVersions > 0"
              class="inline-flex items-center justify-center px-2 py-0.5 text-xs font-medium bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400 rounded-full"
            >
              {{ article.totalVersions }}
            </span>
          </button>
        </nav>
      </div>

      <!-- Tab content -->
      <div v-show="activeTab === 'content'">
        <ArticlesArticleForm
          ref="formRef"
          :article="article"
          :categories="categories"
          :tags="tags"
          :loading="isSaving"
          @submit="handleSubmit"
          @cancel="handleCancel"
          @change="handleFormChange"
        />
      </div>

      <div v-show="activeTab === 'versions'">
        <ArticlesVersionHistory
          :article-id="articleId"
          :published-version-id="article.publishedVersionId"
          @version-published="handleVersionPublished"
        />
      </div>
    </template>
  </div>
</template>
