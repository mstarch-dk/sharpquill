<script setup lang="ts">
import { type ArticleAdminDto, getStatusLabel, getStatusColor } from '~/types/article'

definePageMeta({
  middleware: 'auth'
})

useHead({
  title: 'Pages'
})

const router = useRouter()
const { getPages, deletePage } = useAdminPages()

const pagesList = ref<ArticleAdminDto[]>([])
const isLoading = ref(true)
const error = ref('')

const deleteModalOpen = ref(false)
const pageToDelete = ref<ArticleAdminDto | null>(null)
const isDeleting = ref(false)

async function fetchPages() {
  isLoading.value = true
  error.value = ''

  try {
    const response = await getPages()
    pagesList.value = response.data
  } catch (e) {
    error.value = 'Failed to load pages'
    console.error('Failed to load pages:', e)
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchPages()
})

function handleEdit(page: ArticleAdminDto) {
  router.push(`/pages/${page.id}/edit`)
}

function handleDeleteClick(page: ArticleAdminDto) {
  pageToDelete.value = page
  deleteModalOpen.value = true
}

async function handleDeleteConfirm() {
  if (!pageToDelete.value) return

  isDeleting.value = true

  try {
    await deletePage(pageToDelete.value.id)
    deleteModalOpen.value = false
    pageToDelete.value = null
    await fetchPages()
  } catch (e) {
    console.error('Failed to delete page:', e)
  } finally {
    isDeleting.value = false
  }
}
</script>

<template>
  <div>
    <div class="mb-6 flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Pages</h1>
      <NuxtLink to="/pages/create">
        <UiButton>
          <UiIcon name="plus" class="w-4 h-4 mr-1.5" />
          New Page
        </UiButton>
      </NuxtLink>
    </div>

    <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <div v-if="isLoading" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600" />
      <p class="mt-2 text-gray-500 dark:text-gray-400">Loading pages...</p>
    </div>

    <div v-else-if="pagesList.length === 0" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <p class="text-gray-500 dark:text-gray-400">No pages yet. Create your first page.</p>
    </div>

    <div v-else class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-700">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Title</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Slug</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Status</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Actions</th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="page in pagesList" :key="page.id" class="hover:bg-gray-50 dark:hover:bg-gray-700">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">
              {{ page.title }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
              {{ page.slug }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm">
              <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium" :class="getStatusColor(page.status, page.publishedAt)">
                {{ getStatusLabel(page.status, page.publishedAt) }}
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button
                class="text-primary-600 hover:text-primary-900 mr-3"
                @click="handleEdit(page)"
              >
                Edit
              </button>
              <button
                class="text-red-600 dark:text-red-400 hover:text-red-900 dark:hover:text-red-300"
                @click="handleDeleteClick(page)"
              >
                Delete
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Delete Confirmation Modal -->
    <UiModal :open="deleteModalOpen" @close="deleteModalOpen = false">
      <div class="p-6">
        <h3 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">Delete Page</h3>
        <p class="text-gray-600 dark:text-gray-400 mb-1">
          Are you sure you want to delete <strong>{{ pageToDelete?.title }}</strong>?
        </p>
        <p class="text-sm text-gray-500 dark:text-gray-400 mb-6">
          Navigation items linking to this page will be unlinked. This action cannot be undone.
        </p>
        <div class="flex justify-end gap-3">
          <UiButton variant="secondary" @click="deleteModalOpen = false">Cancel</UiButton>
          <UiButton variant="danger" :loading="isDeleting" @click="handleDeleteConfirm">Delete</UiButton>
        </div>
      </div>
    </UiModal>
  </div>
</template>
