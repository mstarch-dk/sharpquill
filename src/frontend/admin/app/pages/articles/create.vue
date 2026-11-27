<script setup lang="ts">
import type { CategoryDto, TagDto } from '~/types/api'
import type { ArticleCreateDto } from '~/types/article'

definePageMeta({
  middleware: 'auth'
})

useHead({
  title: 'Create Article'
})

const router = useRouter()
const { createArticle } = useAdminArticles()
const { getCategories } = useAdminCategories()
const { getTags } = useAdminTags()

// Data state
const categories = ref<CategoryDto[]>([])
const tags = ref<TagDto[]>([])
const isLoading = ref(false)
const error = ref('')

// Fetch categories and tags
onMounted(async () => {
  try {
    const [categoriesResponse, tagsResponse] = await Promise.all([
      getCategories(),
      getTags()
    ])
    categories.value = categoriesResponse.data
    tags.value = tagsResponse.data
  } catch (e) {
    console.error('Failed to load form data:', e)
  }
})

// Handle form submission
async function handleSubmit(data: ArticleCreateDto) {
  isLoading.value = true
  error.value = ''

  try {
    await createArticle(data)
    router.push('/articles')
  } catch (e: unknown) {
    const apiError = e as { data?: { error?: { message?: string } } }
    error.value = apiError.data?.error?.message || 'Failed to create article'
  } finally {
    isLoading.value = false
  }
}

function handleCancel() {
  router.push('/articles')
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
        <li class="text-gray-900 dark:text-gray-100">Create</li>
      </ol>
    </nav>

    <!-- Error -->
    <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <!-- Form -->
    <ArticlesArticleForm
      :categories="categories"
      :tags="tags"
      :loading="isLoading"
      @submit="handleSubmit"
      @cancel="handleCancel"
    />
  </div>
</template>
