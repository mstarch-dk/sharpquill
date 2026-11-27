<script setup lang="ts">
import type { SeriesDto } from '~/types/series'

definePageMeta({
  middleware: 'auth'
})

useHead({
  title: 'Series'
})

const router = useRouter()
const { getSeries, deleteSeries } = useAdminSeries()

const seriesList = ref<SeriesDto[]>([])
const isLoading = ref(true)
const error = ref('')

const deleteModalOpen = ref(false)
const seriesToDelete = ref<SeriesDto | null>(null)
const isDeleting = ref(false)

async function fetchSeries() {
  isLoading.value = true
  error.value = ''

  try {
    const response = await getSeries()
    seriesList.value = response.data
  } catch (e) {
    error.value = 'Failed to load series'
    console.error('Failed to load series:', e)
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchSeries()
})

function handleEdit(series: SeriesDto) {
  router.push(`/series/${series.id}/edit`)
}

function handleDeleteClick(series: SeriesDto) {
  seriesToDelete.value = series
  deleteModalOpen.value = true
}

async function handleDeleteConfirm() {
  if (!seriesToDelete.value) return

  isDeleting.value = true

  try {
    await deleteSeries(seriesToDelete.value.id)
    deleteModalOpen.value = false
    seriesToDelete.value = null
    await fetchSeries()
  } catch (e) {
    console.error('Failed to delete series:', e)
  } finally {
    isDeleting.value = false
  }
}
</script>

<template>
  <div>
    <div class="mb-6 flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Series</h1>
      <NuxtLink to="/series/create">
        <UiButton>
          <UiIcon name="plus" class="w-4 h-4 mr-1.5" />
          New Series
        </UiButton>
      </NuxtLink>
    </div>

    <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <div v-if="isLoading" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600" />
      <p class="mt-2 text-gray-500 dark:text-gray-400">Loading series...</p>
    </div>

    <div v-else-if="seriesList.length === 0" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <p class="text-gray-500 dark:text-gray-400">No series yet. Create your first series.</p>
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
          <tr v-for="series in seriesList" :key="series.id" class="hover:bg-gray-50 dark:hover:bg-gray-700">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">
              {{ series.name }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
              {{ series.slug }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
              {{ series.articleCount }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button
                class="text-primary-600 hover:text-primary-900 mr-3"
                @click="handleEdit(series)"
              >
                Edit
              </button>
              <button
                class="text-red-600 dark:text-red-400 hover:text-red-900 dark:hover:text-red-300"
                @click="handleDeleteClick(series)"
              >
                Delete
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <SeriesDeleteSeriesModal
      :open="deleteModalOpen"
      :series="seriesToDelete"
      :loading="isDeleting"
      @close="deleteModalOpen = false"
      @confirm="handleDeleteConfirm"
    />
  </div>
</template>
