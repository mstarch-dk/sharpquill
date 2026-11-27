<script setup lang="ts">
import type { TagCreateDto } from '~/types/api'

definePageMeta({
  middleware: 'auth'
})

useHead({
  title: 'Create Tag'
})

const router = useRouter()
const { createTag } = useAdminTags()

const isLoading = ref(false)
const error = ref('')

async function handleSubmit(data: TagCreateDto) {
  isLoading.value = true
  error.value = ''

  try {
    await createTag(data)
    router.push('/tags')
  } catch (e: unknown) {
    const apiError = e as { data?: { error?: { message?: string } } }
    error.value = apiError.data?.error?.message || 'Failed to create tag'
  } finally {
    isLoading.value = false
  }
}

function handleCancel() {
  router.push('/tags')
}
</script>

<template>
  <div>
    <nav class="mb-6">
      <ol class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
        <li>
          <NuxtLink to="/tags" class="hover:text-gray-700 dark:hover:text-gray-300">Tags</NuxtLink>
        </li>
        <li>/</li>
        <li class="text-gray-900 dark:text-gray-100">Create</li>
      </ol>
    </nav>

    <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <TagsTagForm
      :loading="isLoading"
      @submit="handleSubmit"
      @cancel="handleCancel"
    />
  </div>
</template>
