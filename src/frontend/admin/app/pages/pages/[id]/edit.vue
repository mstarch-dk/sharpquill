<script setup lang="ts">
import { ContentType, type ArticleAdminDto, type ArticleUpdateDto } from '~/types/article'

definePageMeta({
  middleware: 'auth'
})

const route = useRoute()
const router = useRouter()
const { getPageById, updatePage } = useAdminPages()

const pageId = computed(() => Number(route.params.id))

const page = ref<ArticleAdminDto | null>(null)
const isLoading = ref(true)
const isSaving = ref(false)
const error = ref('')
const notFound = ref(false)

// Form ref for dirty tracking + auto-save
const formRef = ref<InstanceType<typeof import('~/components/articles/ArticleForm.vue').default> | null>(null)

const isDirty = computed(() => formRef.value?.isDirty ?? false)
const isOpeningPreview = ref(false)

// Unsaved changes protection
useUnsavedChanges(isDirty)

// Auto-save
const autoSave = useAutoSave(async () => {
  if (!formRef.value || !page.value) return
  const data = formRef.value.getFormData() as ArticleUpdateDto
  const response = await updatePage(pageId.value, data)
  page.value = response.data
  formRef.value.markClean()
})

function handleFormChange() {
  autoSave.notifyChange()
}

useHead({
  title: computed(() => page.value ? `Edit: ${page.value.title}` : 'Edit Page')
})

async function fetchPage() {
  try {
    const response = await getPageById(pageId.value)
    page.value = response.data
  } catch (e: unknown) {
    const fetchError = e as { status?: number }
    if (fetchError.status === 404) {
      notFound.value = true
    } else {
      error.value = 'Failed to load page'
    }
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchPage()
})

async function handleSubmit(data: ArticleUpdateDto) {
  isSaving.value = true
  error.value = ''
  autoSave.pause()

  try {
    await updatePage(pageId.value, data)
    formRef.value?.markClean()
    router.push('/pages')
  } catch (e: unknown) {
    const apiError = e as { data?: { error?: { message?: string } } }
    error.value = apiError.data?.error?.message || 'Failed to update page'
  } finally {
    isSaving.value = false
    autoSave.resume()
  }
}

function handleCancel() {
  router.push('/pages')
}

async function handlePreview() {
  if (isOpeningPreview.value) return
  isOpeningPreview.value = true
  try {
    if (isDirty.value && formRef.value && page.value) {
      const data = formRef.value.getFormData() as ArticleUpdateDto
      const response = await updatePage(pageId.value, data)
      page.value = response.data
      formRef.value.markClean()
    }
    window.open(`/pages/${pageId.value}/preview`, '_blank')
  } finally {
    isOpeningPreview.value = false
  }
}
</script>

<template>
  <div>
    <nav class="mb-6">
      <ol class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
        <li>
          <NuxtLink to="/pages" class="hover:text-gray-700 dark:hover:text-gray-300">Pages</NuxtLink>
        </li>
        <li>/</li>
        <li class="text-gray-900 dark:text-gray-100">Edit</li>

        <!-- Preview button -->
        <li v-if="page" class="ml-auto flex items-center gap-3">
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

        <!-- Auto-save / unsaved status indicator -->
        <li v-if="page" class="flex items-center gap-1.5 text-xs">
          <template v-if="autoSave.status.value === 'saving'">
            <UiLoadingSpinner class="h-3.5 w-3.5 text-gray-400" />
            <span class="text-gray-400">Saving...</span>
          </template>
          <template v-else-if="autoSave.status.value === 'saved'">
            <UiIcon name="check" class="h-3.5 w-3.5 text-green-500" />
            <span class="text-green-600 dark:text-green-400">Saved</span>
          </template>
          <template v-else-if="autoSave.status.value === 'error'">
            <UiIcon name="warning" class="h-3.5 w-3.5 text-red-500" />
            <span class="text-red-600 dark:text-red-400">Save failed</span>
          </template>
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

    <div v-if="isLoading" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600" />
      <p class="mt-2 text-gray-500 dark:text-gray-400">Loading page...</p>
    </div>

    <div v-else-if="notFound" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <h2 class="mt-4 text-lg font-medium text-gray-900 dark:text-gray-100">Page not found</h2>
      <p class="mt-2 text-gray-500 dark:text-gray-400">The page you're looking for doesn't exist.</p>
      <NuxtLink to="/pages" class="mt-4 inline-block text-primary-600 hover:text-primary-700">
        Back to pages
      </NuxtLink>
    </div>

    <div v-else-if="error && !page" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <template v-else-if="page">
      <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
        {{ error }}
      </div>

      <ArticlesArticleForm
        ref="formRef"
        :article="page"
        :content-type="ContentType.Page"
        :loading="isSaving"
        @submit="handleSubmit"
        @cancel="handleCancel"
        @change="handleFormChange"
      />
    </template>
  </div>
</template>
