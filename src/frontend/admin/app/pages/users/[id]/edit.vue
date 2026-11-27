<script setup lang="ts">
import type { AuthorAdminDto, AuthorUpdateDto } from '~/types/author'

definePageMeta({
  middleware: 'auth'
})

const route = useRoute()
const router = useRouter()
const { getAuthorById, updateAuthor } = useAdminAuthors()

const authorId = computed(() => Number(route.params.id))

const author = ref<AuthorAdminDto | null>(null)
const isLoading = ref(true)
const isSaving = ref(false)
const error = ref('')
const notFound = ref(false)

useHead({
  title: computed(() => author.value ? `Edit: ${author.value.name}` : 'Edit User')
})

async function fetchAuthor() {
  try {
    const response = await getAuthorById(authorId.value)
    author.value = response.data
  } catch (e: unknown) {
    const fetchError = e as { status?: number }
    if (fetchError.status === 404) {
      notFound.value = true
    } else {
      error.value = 'Failed to load user'
    }
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchAuthor()
})

async function handleSubmit(data: AuthorUpdateDto) {
  isSaving.value = true
  error.value = ''

  try {
    await updateAuthor(authorId.value, data)
    router.push('/users')
  } catch (e: unknown) {
    const apiError = e as { data?: { error?: { message?: string } } }
    error.value = apiError.data?.error?.message || 'Failed to update user'
  } finally {
    isSaving.value = false
  }
}

function handleCancel() {
  router.push('/users')
}
</script>

<template>
  <div>
    <nav class="mb-6">
      <ol class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
        <li>
          <NuxtLink to="/users" class="hover:text-gray-700 dark:hover:text-gray-300">Users</NuxtLink>
        </li>
        <li>/</li>
        <li class="text-gray-900 dark:text-gray-100">Edit</li>
      </ol>
    </nav>

    <div v-if="isLoading" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600" />
      <p class="mt-2 text-gray-500 dark:text-gray-400">Loading user...</p>
    </div>

    <div v-else-if="notFound" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <h2 class="mt-4 text-lg font-medium text-gray-900 dark:text-gray-100">User not found</h2>
      <p class="mt-2 text-gray-500 dark:text-gray-400">The user you're looking for doesn't exist.</p>
      <NuxtLink to="/users" class="mt-4 inline-block text-primary-600 hover:text-primary-700">
        Back to users
      </NuxtLink>
    </div>

    <div v-else-if="error && !author" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <template v-else-if="author">
      <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
        {{ error }}
      </div>

      <UsersUserForm
        :author="author"
        :loading="isSaving"
        @submit="handleSubmit"
        @cancel="handleCancel"
      />
    </template>
  </div>
</template>
