<script setup lang="ts">
import type { CategoryDto } from '~/types/api'

definePageMeta({
  middleware: 'auth'
})

useHead({
  title: 'Categories'
})

const router = useRouter()
const { getCategories, deleteCategory } = useAdminCategories()

const categories = ref<CategoryDto[]>([])
const isLoading = ref(true)
const error = ref('')

const deleteModalOpen = ref(false)
const categoryToDelete = ref<CategoryDto | null>(null)
const isDeleting = ref(false)

async function fetchCategories() {
  isLoading.value = true
  error.value = ''

  try {
    const response = await getCategories()
    categories.value = response.data
  } catch (e) {
    error.value = 'Failed to load categories'
    console.error('Failed to load categories:', e)
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchCategories()
})

function handleEdit(category: CategoryDto) {
  router.push(`/categories/${category.id}/edit`)
}

function handleDeleteClick(category: CategoryDto) {
  categoryToDelete.value = category
  deleteModalOpen.value = true
}

async function handleDeleteConfirm() {
  if (!categoryToDelete.value) return

  isDeleting.value = true

  try {
    await deleteCategory(categoryToDelete.value.id)
    deleteModalOpen.value = false
    categoryToDelete.value = null
    await fetchCategories()
  } catch (e) {
    console.error('Failed to delete category:', e)
  } finally {
    isDeleting.value = false
  }
}
</script>

<template>
  <div>
    <div class="mb-6 flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Categories</h1>
      <NuxtLink to="/categories/create">
        <UiButton>
          <UiIcon name="plus" class="w-4 h-4 mr-1.5" />
          New Category
        </UiButton>
      </NuxtLink>
    </div>

    <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <div v-if="isLoading" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600" />
      <p class="mt-2 text-gray-500 dark:text-gray-400">Loading categories...</p>
    </div>

    <div v-else-if="categories.length === 0" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <p class="text-gray-500 dark:text-gray-400">No categories yet. Create your first category.</p>
    </div>

    <div v-else class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-700">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Name</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Slug</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Description</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Articles</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Actions</th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="category in categories" :key="category.id" class="hover:bg-gray-50 dark:hover:bg-gray-700">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">
              {{ category.name }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
              {{ category.slug }}
            </td>
            <td class="px-6 py-4 text-sm text-gray-500 dark:text-gray-400 max-w-xs truncate">
              {{ category.description || '-' }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
              {{ category.articleCount }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button
                class="text-primary-600 hover:text-primary-900 mr-3"
                @click="handleEdit(category)"
              >
                Edit
              </button>
              <button
                class="text-red-600 dark:text-red-400 hover:text-red-900 dark:hover:text-red-300"
                @click="handleDeleteClick(category)"
              >
                Delete
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <CategoriesDeleteCategoryModal
      :open="deleteModalOpen"
      :category="categoryToDelete"
      :loading="isDeleting"
      @close="deleteModalOpen = false"
      @confirm="handleDeleteConfirm"
    />
  </div>
</template>
