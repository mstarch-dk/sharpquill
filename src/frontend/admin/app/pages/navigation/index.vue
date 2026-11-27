<script setup lang="ts">
import type { NavigationSummaryDto } from '~/types/navigation'

definePageMeta({
  middleware: 'auth'
})

useHead({
  title: 'Navigation'
})

const router = useRouter()
const { getNavigations } = useAdminNavigation()

const navigationList = ref<NavigationSummaryDto[]>([])
const isLoading = ref(true)
const error = ref('')

async function fetchNavigations() {
  isLoading.value = true
  error.value = ''

  try {
    const response = await getNavigations()
    navigationList.value = response.data
  } catch (e) {
    error.value = 'Failed to load navigations'
    console.error('Failed to load navigations:', e)
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchNavigations()
})

function handleEdit(nav: NavigationSummaryDto) {
  router.push(`/navigation/${nav.id}/edit`)
}
</script>

<template>
  <div>
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Navigation</h1>
      <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">Manage navigation menus for the public website.</p>
    </div>

    <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <div v-if="isLoading" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600" />
      <p class="mt-2 text-gray-500 dark:text-gray-400">Loading navigations...</p>
    </div>

    <div v-else-if="navigationList.length === 0" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <p class="text-gray-500 dark:text-gray-400">No navigation areas found.</p>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-6">
      <div
        v-for="nav in navigationList"
        :key="nav.id"
        class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-6 hover:shadow-md transition-shadow cursor-pointer"
        @click="handleEdit(nav)"
      >
        <h2 class="text-lg font-medium text-gray-900 dark:text-gray-100">{{ nav.name }}</h2>
        <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">{{ nav.slug }}</p>
        <button class="mt-4 text-sm text-primary-600 hover:text-primary-700 font-medium">
          Edit Items
        </button>
      </div>
    </div>
  </div>
</template>
