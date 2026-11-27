<script setup lang="ts">
import type { CategoryDto, CategoryUpdateDto } from '~/types/api'

definePageMeta({
  middleware: 'auth'
})

const route = useRoute()
const router = useRouter()
const { getCategoryById, updateCategory } = useAdminCategories()

const categoryId = computed(() => Number(route.params.id))

const category = ref<CategoryDto | null>(null)
const isLoading = ref(true)
const isSaving = ref(false)
const error = ref('')
const notFound = ref(false)

useHead({
  title: computed(() => category.value ? `Edit: ${category.value.name}` : 'Edit Category')
})

async function fetchCategory() {
  try {
    const response = await getCategoryById(categoryId.value)
    category.value = response.data
  } catch (e: unknown) {
    const fetchError = e as { status?: number }
    if (fetchError.status === 404) {
      notFound.value = true
    } else {
      error.value = 'Failed to load category'
    }
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchCategory()
})

async function handleSubmit(data: CategoryUpdateDto) {
  isSaving.value = true
  error.value = ''

  try {
    await updateCategory(categoryId.value, data)
    router.push('/categories')
  } catch (e: unknown) {
    const apiError = e as { data?: { error?: { message?: string } } }
    error.value = apiError.data?.error?.message || 'Failed to update category'
  } finally {
    isSaving.value = false
  }
}

function handleCancel() {
  router.push('/categories')
}
</script>

<template>
  <div>
    <nav class="mb-6">
      <ol class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
        <li>
          <NuxtLink to="/categories" class="hover:text-gray-700 dark:hover:text-gray-300">Categories</NuxtLink>
        </li>
        <li>/</li>
        <li class="text-gray-900 dark:text-gray-100">Edit</li>
      </ol>
    </nav>

    <div v-if="isLoading" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600" />
      <p class="mt-2 text-gray-500 dark:text-gray-400">Loading category...</p>
    </div>

    <div v-else-if="notFound" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <h2 class="mt-4 text-lg font-medium text-gray-900 dark:text-gray-100">Category not found</h2>
      <p class="mt-2 text-gray-500 dark:text-gray-400">The category you're looking for doesn't exist.</p>
      <NuxtLink to="/categories" class="mt-4 inline-block text-primary-600 hover:text-primary-700">
        Back to categories
      </NuxtLink>
    </div>

    <div v-else-if="error && !category" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <template v-else-if="category">
      <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
        {{ error }}
      </div>

      <CategoriesCategoryForm
        :category="category"
        :loading="isSaving"
        @submit="handleSubmit"
        @cancel="handleCancel"
      />
    </template>
  </div>
</template>
