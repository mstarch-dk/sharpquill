<script setup lang="ts">
import type { LocationQuery, LocationQueryRaw } from 'vue-router'
import type {
  ArticleAdminDto,
  ArticleListFilters,
  ArticleListSort,
  ArticleListStatusFilter
} from '~/types/article'

definePageMeta({
  middleware: 'auth'
})

useHead({
  title: 'Articles'
})

const route = useRoute()
const router = useRouter()
const { getArticles, deleteArticle } = useAdminArticles()
const { getCategories } = useAdminCategories()
const { getAuthors } = useAdminAuthors()
const { isAdmin } = useAuth()

const pageSize = 10

const statusOptions: { value: ArticleListStatusFilter, label: string }[] = [
  { value: 'draft', label: 'Draft' },
  { value: 'published', label: 'Published' },
  { value: 'scheduled', label: 'Scheduled' },
  { value: 'archived', label: 'Archived' }
]

// Relevance is the default while searching (the empty choice), so it needs no option of its own.
const sortOptions: { value: ArticleListSort, label: string }[] = [
  { value: 'newest', label: 'Newest first' },
  { value: 'oldest', label: 'Oldest first' },
  { value: 'updated', label: 'Recently updated' },
  { value: 'title', label: 'Title A-Z' }
]

const categoryOptions = ref<{ value: number, label: string }[]>([])
const authorOptions = ref<{ value: number, label: string }[]>([])

// The URL query is the source of truth for page and filters, so reload, links and back/forward restore the list.
function queryString(query: LocationQuery, key: string): string | undefined {
  const value = query[key]
  const first = Array.isArray(value) ? value[0] : value
  return typeof first === 'string' && first.trim() !== '' ? first.trim() : undefined
}

function queryNumber(query: LocationQuery, key: string): number | undefined {
  const value = Number(queryString(query, key))
  return Number.isInteger(value) && value > 0 ? value : undefined
}

function oneOf<T extends string>(value: string | undefined, allowed: { value: T }[]): T | undefined {
  return allowed.find(o => o.value === value)?.value
}

const filters = computed<ArticleListFilters>(() => ({
  search: queryString(route.query, 'search'),
  status: oneOf(queryString(route.query, 'status'), statusOptions),
  categoryId: queryNumber(route.query, 'categoryId'),
  authorId: queryNumber(route.query, 'authorId'),
  sort: oneOf(queryString(route.query, 'sort'), sortOptions)
}))

const currentPage = computed({
  get: () => queryNumber(route.query, 'page') ?? 1,
  set: (page: number) => updateQuery({ page: page > 1 ? page : undefined }, false)
})

// Without a search the empty choice already means newest first, so that option is not listed twice.
const sortSelectOptions = computed(() =>
  filters.value.search ? sortOptions : sortOptions.filter(o => o.value !== 'newest'))
const sortSelectValue = computed(() =>
  !filters.value.search && filters.value.sort === 'newest' ? undefined : filters.value.sort)

const hasFilters = computed(() => Object.values(filters.value).some(v => v !== undefined))

/** Writes changes into the URL query; a filter change starts again at page 1. */
function updateQuery(changes: Partial<Record<keyof ArticleListFilters | 'page', string | number | undefined>>, resetPage = true) {
  const query: LocationQueryRaw = { ...route.query, ...changes }
  if (resetPage) delete query.page
  for (const key of Object.keys(query)) {
    if (query[key] === undefined || query[key] === '') delete query[key]
  }
  router.push({ query })
}

function clearFilters() {
  searchInput.value = ''
  router.push({ query: {} })
}

// Search box: typed text goes into the URL after a short pause, and follows the URL on back/forward.
const searchInput = ref(filters.value.search ?? '')
let searchTimer: ReturnType<typeof setTimeout> | undefined

watch(searchInput, (value) => {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    if (value.trim() !== (filters.value.search ?? '')) updateQuery({ search: value.trim() || undefined })
  }, 300)
})

watch(() => filters.value.search, (search) => {
  if ((search ?? '') !== searchInput.value.trim()) searchInput.value = search ?? ''
})

onBeforeUnmount(() => clearTimeout(searchTimer))

// Data state
const articles = ref<ArticleAdminDto[]>([])
const totalPages = ref(0)
const isLoading = ref(true)
const error = ref('')

// Delete modal state
const deleteModalOpen = ref(false)
const articleToDelete = ref<ArticleAdminDto | null>(null)
const isDeleting = ref(false)

// Only the latest request may fill the list, so a slow earlier search cannot overwrite a newer one.
let requestId = 0

async function fetchArticles() {
  const thisRequest = ++requestId
  isLoading.value = true
  error.value = ''

  try {
    const response = await getArticles(currentPage.value, pageSize, filters.value)
    if (thisRequest !== requestId) return
    articles.value = response.data
    totalPages.value = response.meta?.totalPages || 1
  } catch (e) {
    if (thisRequest !== requestId) return
    error.value = 'Failed to load articles'
    console.error('Failed to load articles:', e)
  } finally {
    if (thisRequest === requestId) isLoading.value = false
  }
}

async function loadFilterOptions() {
  try {
    const categories = await getCategories()
    categoryOptions.value = categories.data.map(c => ({ value: c.id, label: c.name }))
  } catch (e) {
    console.error('Failed to load categories:', e)
  }

  // The author list is admin-only; other authors get no author filter.
  if (!isAdmin.value) return
  try {
    const authors = await getAuthors()
    authorOptions.value = authors.data.map(a => ({ value: a.id, label: a.name || a.email }))
  } catch (e) {
    console.error('Failed to load authors:', e)
  }
}

// Refetch when the query changes on this page (not when navigating away to another page).
const listRouteName = route.name
watch(() => route.query, () => {
  if (route.name === listRouteName) fetchArticles()
})

onMounted(() => {
  fetchArticles()
  loadFilterOptions()
})

// Navigation handlers
function handleEdit(article: ArticleAdminDto) {
  router.push(`/articles/${article.id}/edit`)
}

function handleDeleteClick(article: ArticleAdminDto) {
  articleToDelete.value = article
  deleteModalOpen.value = true
}

async function handleDeleteConfirm() {
  if (!articleToDelete.value) return

  isDeleting.value = true

  try {
    await deleteArticle(articleToDelete.value.id)
    deleteModalOpen.value = false
    articleToDelete.value = null
    await fetchArticles()
  } catch (e) {
    console.error('Failed to delete article:', e)
  } finally {
    isDeleting.value = false
  }
}
</script>

<template>
  <div>
    <!-- Header -->
    <div class="mb-6 flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Articles</h1>
      <NuxtLink to="/articles/create">
        <UiButton>
          <UiIcon name="plus" class="w-4 h-4 mr-1.5" />
          New Article
        </UiButton>
      </NuxtLink>
    </div>

    <!-- Error -->
    <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <!-- Search and filters -->
    <div class="mb-4 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-6">
      <div class="sm:col-span-2 lg:col-span-2">
        <ArticlesArticleJumpSearch v-model="searchInput" label="Search" placeholder="Title or text" />
      </div>
      <UiSelect
        :model-value="filters.status"
        :options="statusOptions"
        label="Status"
        placeholder="All statuses"
        @update:model-value="updateQuery({ status: $event })"
      />
      <UiSelect
        :model-value="filters.categoryId"
        :options="categoryOptions"
        label="Category"
        placeholder="All categories"
        @update:model-value="updateQuery({ categoryId: $event })"
      />
      <UiSelect
        v-if="isAdmin"
        :model-value="filters.authorId"
        :options="authorOptions"
        label="Author"
        placeholder="All authors"
        @update:model-value="updateQuery({ authorId: $event })"
      />
      <UiSelect
        :model-value="sortSelectValue"
        :options="sortSelectOptions"
        label="Sort"
        :placeholder="filters.search ? 'Best match' : 'Newest first'"
        @update:model-value="updateQuery({ sort: $event })"
      />
    </div>
    <div v-if="hasFilters" class="mb-4 -mt-1">
      <button
        type="button"
        class="text-sm text-primary-600 hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300"
        @click="clearFilters"
      >
        Clear search and filters
      </button>
    </div>

    <!-- Article list -->
    <ArticlesArticleList
      :articles="articles"
      :loading="isLoading"
      :filtered="hasFilters"
      @edit="handleEdit"
      @delete="handleDeleteClick"
    />

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="mt-6">
      <UiPagination
        v-model:current-page="currentPage"
        :total-pages="totalPages"
      />
    </div>

    <!-- Delete modal -->
    <ArticlesDeleteArticleModal
      :open="deleteModalOpen"
      :article="articleToDelete"
      :loading="isDeleting"
      @close="deleteModalOpen = false"
      @confirm="handleDeleteConfirm"
    />
  </div>
</template>
