<script setup lang="ts">
import type { SeriesAdminDetailDto, SeriesUpdateDto } from '~/types/series'
import type { ArticleAdminDto } from '~/types/article'

definePageMeta({
  middleware: 'auth'
})

const route = useRoute()
const router = useRouter()
const { getSeriesById, updateSeries, addArticleToSeries, removeArticleFromSeries, reorderArticles } = useAdminSeries()
const { getArticles } = useAdminArticles()

const seriesId = computed(() => Number(route.params.id))

const series = ref<SeriesAdminDetailDto | null>(null)
const isLoading = ref(true)
const isSaving = ref(false)
const error = ref('')
const notFound = ref(false)

const addModalOpen = ref(false)
const availableArticles = ref<ArticleAdminDto[]>([])
const isLoadingArticles = ref(false)

useHead({
  title: computed(() => series.value ? `Edit: ${series.value.name}` : 'Edit Series')
})

async function fetchSeries() {
  try {
    const response = await getSeriesById(seriesId.value)
    series.value = response.data
  } catch (e: unknown) {
    const fetchError = e as { status?: number }
    if (fetchError.status === 404) {
      notFound.value = true
    } else {
      error.value = 'Failed to load series'
    }
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchSeries()
})

async function handleSubmit(data: SeriesUpdateDto) {
  isSaving.value = true
  error.value = ''

  try {
    await updateSeries(seriesId.value, data)
    router.push('/series')
  } catch (e: unknown) {
    const apiError = e as { data?: { error?: { message?: string } } }
    error.value = apiError.data?.error?.message || 'Failed to update series'
  } finally {
    isSaving.value = false
  }
}

function handleCancel() {
  router.push('/series')
}

async function handleAddArticle() {
  addModalOpen.value = true
  isLoadingArticles.value = true

  try {
    const response = await getArticles(1, 100)
    // Filter out articles already in a series
    const seriesArticleIds = new Set(series.value?.articles.map(a => a.id) || [])
    availableArticles.value = response.data.filter(a => !a.series && !seriesArticleIds.has(a.id))
  } catch (e) {
    console.error('Failed to load articles:', e)
  } finally {
    isLoadingArticles.value = false
  }
}

async function handleSelectArticle(articleId: number) {
  try {
    await addArticleToSeries(seriesId.value, { articleId })
    addModalOpen.value = false
    isLoading.value = true
    await fetchSeries()
  } catch (e) {
    console.error('Failed to add article:', e)
  }
}

async function handleRemoveArticle(articleId: number) {
  try {
    await removeArticleFromSeries(seriesId.value, articleId)
    isLoading.value = true
    await fetchSeries()
  } catch (e) {
    console.error('Failed to remove article:', e)
  }
}

async function handleMoveUp(articleId: number) {
  if (!series.value) return

  const articles = [...series.value.articles]
  const index = articles.findIndex(a => a.id === articleId)
  if (index <= 0) return

  const previous = articles[index - 1]
  const current = articles[index]
  if (!previous || !current) return
  articles[index - 1] = current
  articles[index] = previous
  const articleIds = articles.map(a => a.id)

  try {
    await reorderArticles(seriesId.value, { articleIds })
    isLoading.value = true
    await fetchSeries()
  } catch (e) {
    console.error('Failed to reorder:', e)
  }
}

async function handleMoveDown(articleId: number) {
  if (!series.value) return

  const articles = [...series.value.articles]
  const index = articles.findIndex(a => a.id === articleId)
  if (index < 0 || index >= articles.length - 1) return

  const current = articles[index]
  const next = articles[index + 1]
  if (!current || !next) return
  articles[index] = next
  articles[index + 1] = current
  const articleIds = articles.map(a => a.id)

  try {
    await reorderArticles(seriesId.value, { articleIds })
    isLoading.value = true
    await fetchSeries()
  } catch (e) {
    console.error('Failed to reorder:', e)
  }
}
</script>

<template>
  <div>
    <nav class="mb-6">
      <ol class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
        <li>
          <NuxtLink to="/series" class="hover:text-gray-700 dark:hover:text-gray-300">Series</NuxtLink>
        </li>
        <li>/</li>
        <li class="text-gray-900 dark:text-gray-100">Edit</li>
      </ol>
    </nav>

    <div v-if="isLoading" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600" />
      <p class="mt-2 text-gray-500 dark:text-gray-400">Loading series...</p>
    </div>

    <div v-else-if="notFound" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <h2 class="mt-4 text-lg font-medium text-gray-900 dark:text-gray-100">Series not found</h2>
      <p class="mt-2 text-gray-500 dark:text-gray-400">The series you're looking for doesn't exist.</p>
      <NuxtLink to="/series" class="mt-4 inline-block text-primary-600 hover:text-primary-700">
        Back to series
      </NuxtLink>
    </div>

    <div v-else-if="error && !series" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <template v-else-if="series">
      <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
        {{ error }}
      </div>

      <SeriesForm
        :series="series"
        :loading="isSaving"
        @submit="handleSubmit"
        @cancel="handleCancel"
      />

      <div class="mt-8">
        <SeriesArticleList
          :articles="series.articles"
          @move-up="handleMoveUp"
          @move-down="handleMoveDown"
          @remove="handleRemoveArticle"
        >
          <template #actions>
            <UiButton size="sm" @click="handleAddArticle">
              <UiIcon name="plus" class="w-4 h-4 mr-1" />
              Add Article
            </UiButton>
          </template>
        </SeriesArticleList>
      </div>

      <SeriesAddArticleModal
        :open="addModalOpen"
        :articles="availableArticles"
        :loading="isLoadingArticles"
        @close="addModalOpen = false"
        @select="handleSelectArticle"
      />
    </template>
  </div>
</template>
