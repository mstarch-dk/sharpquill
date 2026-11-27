<script setup lang="ts">
import type { AuthorAdminDto } from '~/types/author'

definePageMeta({
  middleware: 'auth'
})

useHead({
  title: 'Users'
})

const router = useRouter()
const { getAuthors, deleteAuthor } = useAdminAuthors()
const { user } = useAuth()

const authors = ref<AuthorAdminDto[]>([])
const isLoading = ref(true)
const error = ref('')

const deleteModalOpen = ref(false)
const authorToDelete = ref<AuthorAdminDto | null>(null)
const isDeleting = ref(false)

async function fetchAuthors() {
  isLoading.value = true
  error.value = ''

  try {
    const response = await getAuthors()
    authors.value = response.data
  } catch (e) {
    error.value = 'Failed to load users'
    console.error('Failed to load users:', e)
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchAuthors()
})

function handleEdit(author: AuthorAdminDto) {
  router.push(`/users/${author.id}/edit`)
}

function handleDeleteClick(author: AuthorAdminDto) {
  authorToDelete.value = author
  deleteModalOpen.value = true
}

async function handleDeleteConfirm() {
  if (!authorToDelete.value) return

  isDeleting.value = true

  try {
    await deleteAuthor(authorToDelete.value.id)
    deleteModalOpen.value = false
    authorToDelete.value = null
    await fetchAuthors()
  } catch (e: unknown) {
    const apiError = e as { data?: { error?: { message?: string } } }
    error.value = apiError.data?.error?.message || 'Failed to delete user'
  } finally {
    isDeleting.value = false
  }
}

function formatRole(role: string): string {
  return role.charAt(0).toUpperCase() + role.slice(1)
}
</script>

<template>
  <div>
    <div class="mb-6 flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Users</h1>
      <NuxtLink to="/users/create">
        <UiButton>
          <UiIcon name="plus" class="w-4 h-4 mr-1.5" />
          New User
        </UiButton>
      </NuxtLink>
    </div>

    <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <div v-if="isLoading" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600" />
      <p class="mt-2 text-gray-500 dark:text-gray-400">Loading users...</p>
    </div>

    <div v-else-if="authors.length === 0" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <p class="text-gray-500 dark:text-gray-400">No users found.</p>
    </div>

    <div v-else class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-700">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Name</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Email</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Role</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Actions</th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="author in authors" :key="author.id" class="hover:bg-gray-50 dark:hover:bg-gray-700">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">
              {{ author.name }}
              <span v-if="author.id === user?.id" class="ml-1 text-xs text-gray-400 dark:text-gray-500">(you)</span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
              {{ author.email }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm">
              <span
                class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
                :class="author.role === 'admin'
                  ? 'bg-purple-100 dark:bg-purple-900/30 text-purple-800 dark:text-purple-300'
                  : 'bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200'"
              >
                {{ formatRole(author.role) }}
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button
                class="text-primary-600 hover:text-primary-900 dark:hover:text-primary-400 mr-3"
                @click="handleEdit(author)"
              >
                Edit
              </button>
              <button
                v-if="author.id !== user?.id"
                class="text-red-600 hover:text-red-900 dark:hover:text-red-400"
                @click="handleDeleteClick(author)"
              >
                Delete
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <UsersDeleteUserModal
      :open="deleteModalOpen"
      :author="authorToDelete"
      :loading="isDeleting"
      @close="deleteModalOpen = false"
      @confirm="handleDeleteConfirm"
    />
  </div>
</template>
