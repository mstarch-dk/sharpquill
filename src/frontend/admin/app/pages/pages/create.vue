<script setup lang="ts">
import { ContentType, type ArticleCreateDto } from '~/types/article'

definePageMeta({
  middleware: 'auth'
})

useHead({
  title: 'Create Page'
})

const router = useRouter()
const { createPage } = useAdminPages()

const isLoading = ref(false)
const error = ref('')

async function handleSubmit(data: ArticleCreateDto) {
  isLoading.value = true
  error.value = ''

  try {
    await createPage(data)
    router.push('/pages')
  } catch (e: unknown) {
    const apiError = e as { data?: { error?: { message?: string } } }
    error.value = apiError.data?.error?.message || 'Failed to create page'
  } finally {
    isLoading.value = false
  }
}

function handleCancel() {
  router.push('/pages')
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
        <li class="text-gray-900 dark:text-gray-100">Create</li>
      </ol>
    </nav>

    <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <ArticlesArticleForm
      :content-type="ContentType.Page"
      :loading="isLoading"
      @submit="handleSubmit"
      @cancel="handleCancel"
    />
  </div>
</template>
