<script setup lang="ts">
import type { AuthorCreateDto, AuthorUpdateDto } from '~/types/author'

definePageMeta({
  middleware: 'auth'
})

useHead({
  title: 'Create User'
})

const router = useRouter()
const { createAuthor } = useAdminAuthors()

const isLoading = ref(false)
const error = ref('')

async function handleSubmit(data: AuthorCreateDto | AuthorUpdateDto) {
  isLoading.value = true
  error.value = ''

  try {
    if (!data.password) {
      error.value = 'Password is required when creating a user'
      return
    }
    const payload: AuthorCreateDto = {
      email: data.email,
      name: data.name,
      role: data.role,
      password: data.password,
      bio: data.bio,
      avatarUrl: data.avatarUrl,
      certificateThumbprint: data.certificateThumbprint
    }
    await createAuthor(payload)
    router.push('/users')
  } catch (e: unknown) {
    const apiError = e as { data?: { error?: { message?: string } } }
    error.value = apiError.data?.error?.message || 'Failed to create user'
  } finally {
    isLoading.value = false
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
        <li class="text-gray-900 dark:text-gray-100">Create</li>
      </ol>
    </nav>

    <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <UsersUserForm
      :loading="isLoading"
      @submit="handleSubmit"
      @cancel="handleCancel"
    />
  </div>
</template>
