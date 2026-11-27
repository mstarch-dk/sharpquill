<script setup lang="ts">
import type { TagDto } from '~/types/api'

definePageMeta({
  middleware: 'auth'
})

useHead({
  title: 'Tags'
})

const router = useRouter()
const { getTags, deleteTag } = useAdminTags()

const tags = ref<TagDto[]>([])
const isLoading = ref(true)
const error = ref('')

const deleteModalOpen = ref(false)
const tagToDelete = ref<TagDto | null>(null)
const isDeleting = ref(false)

async function fetchTags() {
  isLoading.value = true
  error.value = ''

  try {
    const response = await getTags()
    tags.value = response.data
  } catch (e) {
    error.value = 'Failed to load tags'
    console.error('Failed to load tags:', e)
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchTags()
})

function handleEdit(tag: TagDto) {
  router.push(`/tags/${tag.id}/edit`)
}

function handleDeleteClick(tag: TagDto) {
  tagToDelete.value = tag
  deleteModalOpen.value = true
}

async function handleDeleteConfirm() {
  if (!tagToDelete.value) return

  isDeleting.value = true

  try {
    await deleteTag(tagToDelete.value.id)
    deleteModalOpen.value = false
    tagToDelete.value = null
    await fetchTags()
  } catch (e) {
    console.error('Failed to delete tag:', e)
  } finally {
    isDeleting.value = false
  }
}
</script>

<template>
  <div>
    <div class="mb-6 flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Tags</h1>
      <NuxtLink to="/tags/create">
        <UiButton>
          <UiIcon name="plus" class="w-4 h-4 mr-1.5" />
          New Tag
        </UiButton>
      </NuxtLink>
    </div>

    <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <div v-if="isLoading" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600" />
      <p class="mt-2 text-gray-500 dark:text-gray-400">Loading tags...</p>
    </div>

    <div v-else-if="tags.length === 0" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <p class="text-gray-500 dark:text-gray-400">No tags yet. Create your first tag.</p>
    </div>

    <div v-else class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-700">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Name</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Slug</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Articles</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Actions</th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="tag in tags" :key="tag.id" class="hover:bg-gray-50 dark:hover:bg-gray-700">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">
              {{ tag.name }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
              {{ tag.slug }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
              {{ tag.articleCount }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button
                class="text-primary-600 hover:text-primary-900 mr-3"
                @click="handleEdit(tag)"
              >
                Edit
              </button>
              <button
                class="text-red-600 dark:text-red-400 hover:text-red-900 dark:hover:text-red-300"
                @click="handleDeleteClick(tag)"
              >
                Delete
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <TagsDeleteTagModal
      :open="deleteModalOpen"
      :tag="tagToDelete"
      :loading="isDeleting"
      @close="deleteModalOpen = false"
      @confirm="handleDeleteConfirm"
    />
  </div>
</template>
