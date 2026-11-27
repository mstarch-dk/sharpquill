<script setup lang="ts">
import { NavigationItemType, type NavigationAdminDto, type NavigationItemSaveDto } from '~/types/navigation'
import { ArticleStatus, type ArticleAdminDto } from '~/types/article'

definePageMeta({
  middleware: 'auth'
})

const route = useRoute()
const router = useRouter()
const { getNavigationById, saveNavigationItems } = useAdminNavigation()
const { getPages } = useAdminPages()

const navigationId = computed(() => Number(route.params.id))

const navigation = ref<NavigationAdminDto | null>(null)
const pages = ref<ArticleAdminDto[]>([])
const isLoading = ref(true)
const isSaving = ref(false)
const error = ref('')
const notFound = ref(false)

// Local editable items
interface EditableItem {
  label: string
  itemType: NavigationItemType
  articleId?: number
  systemRoute?: string
  url?: string
  parentId?: number
  position: number
  openInNewTab: boolean
}

const items = ref<EditableItem[]>([])

useHead({
  title: computed(() => navigation.value ? `Edit: ${navigation.value.name}` : 'Edit Navigation')
})

const systemRouteOptions = [
  { value: '/', label: 'Hjem (/)' },
  { value: '/blog', label: 'Blog (/blog)' },
  { value: '/categories', label: 'Kategorier (/categories)' },
  { value: '/series', label: 'Serier (/series)' },
]

const itemTypeOptions = [
  { value: NavigationItemType.Page, label: 'Page' },
  { value: NavigationItemType.SystemRoute, label: 'System Route' },
  { value: NavigationItemType.ExternalUrl, label: 'External URL' },
]

const publishedPages = computed(() =>
  pages.value.filter(p => p.status === ArticleStatus.Published)
)

async function fetchData() {
  try {
    const [navResponse, pagesResponse] = await Promise.all([
      getNavigationById(navigationId.value),
      getPages()
    ])

    navigation.value = navResponse.data
    pages.value = pagesResponse.data

    // Map server items to editable items
    items.value = navResponse.data.items.map(item => ({
      label: item.label,
      itemType: item.itemType,
      articleId: item.articleId ?? undefined,
      systemRoute: item.systemRoute ?? undefined,
      url: item.url ?? undefined,
      parentId: item.parentId ?? undefined,
      position: item.position,
      openInNewTab: item.openInNewTab
    }))
  } catch (e: unknown) {
    const fetchError = e as { status?: number }
    if (fetchError.status === 404) {
      notFound.value = true
    } else {
      error.value = 'Failed to load navigation'
    }
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchData()
})

function addItem() {
  items.value.push({
    label: '',
    itemType: NavigationItemType.SystemRoute,
    systemRoute: '/',
    position: items.value.length + 1,
    openInNewTab: false
  })
}

function removeItem(index: number) {
  items.value.splice(index, 1)
  reindexPositions()
}

function moveUp(index: number) {
  if (index <= 0) return
  const current = items.value[index]
  const previous = items.value[index - 1]
  if (!current || !previous) return
  items.value[index] = previous
  items.value[index - 1] = current
  reindexPositions()
}

function moveDown(index: number) {
  if (index >= items.value.length - 1) return
  const current = items.value[index]
  const next = items.value[index + 1]
  if (!current || !next) return
  items.value[index] = next
  items.value[index + 1] = current
  reindexPositions()
}

function reindexPositions() {
  items.value.forEach((item, i) => {
    item.position = i + 1
  })
}

async function handleSave() {
  isSaving.value = true
  error.value = ''

  try {
    const saveItems: NavigationItemSaveDto[] = items.value.map(item => ({
      label: item.label,
      itemType: item.itemType,
      articleId: item.itemType === NavigationItemType.Page ? item.articleId : undefined,
      systemRoute: item.itemType === NavigationItemType.SystemRoute ? item.systemRoute : undefined,
      url: item.itemType === NavigationItemType.ExternalUrl ? item.url : undefined,
      parentId: item.parentId,
      position: item.position,
      openInNewTab: item.openInNewTab
    }))

    await saveNavigationItems(navigationId.value, { items: saveItems })
    router.push('/navigation')
  } catch (e: unknown) {
    const apiError = e as { data?: { error?: { message?: string } } }
    error.value = apiError.data?.error?.message || 'Failed to save navigation items'
  } finally {
    isSaving.value = false
  }
}
</script>

<template>
  <div>
    <nav class="mb-6">
      <ol class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
        <li>
          <NuxtLink to="/navigation" class="hover:text-gray-700 dark:hover:text-gray-300">Navigation</NuxtLink>
        </li>
        <li>/</li>
        <li class="text-gray-900 dark:text-gray-100">{{ navigation?.name || 'Edit' }}</li>
      </ol>
    </nav>

    <div v-if="isLoading" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600" />
      <p class="mt-2 text-gray-500 dark:text-gray-400">Loading navigation...</p>
    </div>

    <div v-else-if="notFound" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <h2 class="mt-4 text-lg font-medium text-gray-900 dark:text-gray-100">Navigation not found</h2>
      <NuxtLink to="/navigation" class="mt-4 inline-block text-primary-600 hover:text-primary-700">
        Back to navigation
      </NuxtLink>
    </div>

    <template v-else-if="navigation">
      <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
        {{ error }}
      </div>

      <div class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-6">
        <div class="flex items-center justify-between mb-6">
          <h2 class="text-lg font-medium text-gray-900 dark:text-gray-100">{{ navigation.name }}</h2>
          <UiButton size="sm" @click="addItem">
            <UiIcon name="plus" class="w-4 h-4 mr-1" />
            Add Item
          </UiButton>
        </div>

        <div v-if="items.length === 0" class="text-center py-8 text-gray-500 dark:text-gray-400">
          No items yet. Add your first navigation item.
        </div>

        <div v-else class="space-y-4">
          <div
            v-for="(item, index) in items"
            :key="index"
            class="border border-gray-200 dark:border-gray-700 rounded-lg p-4"
          >
            <div class="flex items-start gap-4">
              <!-- Reorder buttons -->
              <div class="flex flex-col gap-1 pt-1">
                <button
                  type="button"
                  class="p-1 text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-400 disabled:opacity-30"
                  :disabled="index === 0"
                  @click="moveUp(index)"
                >
                  <UiIcon name="chevron-up" class="w-4 h-4" />
                </button>
                <button
                  type="button"
                  class="p-1 text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-400 disabled:opacity-30"
                  :disabled="index === items.length - 1"
                  @click="moveDown(index)"
                >
                  <UiIcon name="chevron-down" class="w-4 h-4" />
                </button>
              </div>

              <!-- Item fields -->
              <div class="flex-1 grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Label</label>
                  <input
                    v-model="item.label"
                    type="text"
                    class="w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm dark:shadow-gray-900/30 focus:border-primary-500 focus:ring-primary-500 text-sm dark:bg-gray-800 dark:text-gray-100"
                    placeholder="Menu label"
                  />
                </div>

                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Type</label>
                  <select
                    v-model="item.itemType"
                    class="w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm dark:shadow-gray-900/30 focus:border-primary-500 focus:ring-primary-500 text-sm dark:bg-gray-800 dark:text-gray-100"
                  >
                    <option v-for="opt in itemTypeOptions" :key="opt.value" :value="opt.value">
                      {{ opt.label }}
                    </option>
                  </select>
                </div>

                <div>
                  <template v-if="item.itemType === NavigationItemType.Page">
                    <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Page</label>
                    <select
                      v-model="item.articleId"
                      class="w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm dark:shadow-gray-900/30 focus:border-primary-500 focus:ring-primary-500 text-sm dark:bg-gray-800 dark:text-gray-100"
                    >
                      <option :value="undefined">Select a page</option>
                      <option v-for="p in publishedPages" :key="p.id" :value="p.id">
                        {{ p.title }}
                      </option>
                    </select>
                  </template>

                  <template v-else-if="item.itemType === NavigationItemType.SystemRoute">
                    <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Route</label>
                    <select
                      v-model="item.systemRoute"
                      class="w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm dark:shadow-gray-900/30 focus:border-primary-500 focus:ring-primary-500 text-sm dark:bg-gray-800 dark:text-gray-100"
                    >
                      <option v-for="opt in systemRouteOptions" :key="opt.value" :value="opt.value">
                        {{ opt.label }}
                      </option>
                    </select>
                  </template>

                  <template v-else-if="item.itemType === NavigationItemType.ExternalUrl">
                    <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">URL</label>
                    <input
                      v-model="item.url"
                      type="url"
                      class="w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm dark:shadow-gray-900/30 focus:border-primary-500 focus:ring-primary-500 text-sm dark:bg-gray-800 dark:text-gray-100"
                      placeholder="https://example.com"
                    />
                  </template>
                </div>
              </div>

              <!-- Open in new tab + Remove -->
              <div class="flex items-center gap-3 pt-5">
                <label class="flex items-center text-xs text-gray-500 dark:text-gray-400">
                  <input
                    v-model="item.openInNewTab"
                    type="checkbox"
                    class="h-4 w-4 rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500 mr-1"
                  />
                  New tab
                </label>
                <button
                  type="button"
                  class="p-1 text-red-400 dark:text-red-500 hover:text-red-600 dark:hover:text-red-400"
                  @click="removeItem(index)"
                >
                  <UiIcon name="trash" class="w-5 h-5" />
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="mt-6 flex justify-end gap-3">
        <UiButton variant="secondary" @click="router.push('/navigation')">
          Cancel
        </UiButton>
        <UiButton :loading="isSaving" @click="handleSave">
          Save Items
        </UiButton>
      </div>
    </template>
  </div>
</template>
