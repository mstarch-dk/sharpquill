<script setup lang="ts">
import type { ArticleVersionDto, ArticleVersionDetailDto } from '~/types/article'

const props = defineProps<{
  articleId: number
  publishedVersionId?: number
}>()

const emit = defineEmits<{
  versionPublished: []
}>()

const { getVersions, getVersion, publishVersion } = useAdminArticles()

const versions = ref<ArticleVersionDto[]>([])
const selectedVersion = ref<ArticleVersionDetailDto | null>(null)
const isLoading = ref(true)
const isPublishing = ref(false)
const error = ref('')

async function loadVersions() {
  isLoading.value = true
  error.value = ''
  try {
    const response = await getVersions(props.articleId)
    versions.value = response.data
  } catch {
    error.value = 'Failed to load versions'
  } finally {
    isLoading.value = false
  }
}

async function viewVersion(versionId: number) {
  error.value = ''
  try {
    const response = await getVersion(props.articleId, versionId)
    selectedVersion.value = response.data
  } catch {
    error.value = 'Failed to load version details'
  }
}

async function handlePublish(versionId: number) {
  if (!confirm('Set this version as the published version?')) return

  isPublishing.value = true
  error.value = ''
  try {
    await publishVersion(props.articleId, versionId)
    emit('versionPublished')
    await loadVersions()
    selectedVersion.value = null
  } catch {
    error.value = 'Failed to publish version'
  } finally {
    isPublishing.value = false
  }
}

function closeDetail() {
  selectedVersion.value = null
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString()
}

onMounted(loadVersions)
</script>

<template>
  <div>
    <!-- Error -->
    <div v-if="error" class="mb-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="text-center py-8">
      <UiLoadingSpinner class="h-6 w-6 mx-auto text-primary-600" />
    </div>

    <!-- Version detail modal -->
    <div v-else-if="selectedVersion" class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg">
      <div class="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
        <h3 class="text-lg font-medium">
          Version {{ selectedVersion.versionNumber }}
          <span class="text-sm text-gray-500 dark:text-gray-400 ml-2">{{ formatDate(selectedVersion.createdAt) }}</span>
        </h3>
        <div class="flex items-center gap-2">
          <button
            v-if="selectedVersion.id !== publishedVersionId"
            class="px-3 py-1.5 text-sm bg-primary-600 text-white rounded-md hover:bg-primary-700 disabled:opacity-50"
            :disabled="isPublishing"
            @click="handlePublish(selectedVersion.id)"
          >
            {{ isPublishing ? 'Publishing...' : 'Set as Published' }}
          </button>
          <span v-else class="px-3 py-1.5 text-sm bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300 rounded-md">
            Currently Published
          </span>
          <button
            class="px-3 py-1.5 text-sm text-gray-600 dark:text-gray-400 border border-gray-300 dark:border-gray-600 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700"
            @click="closeDetail"
          >
            Back to list
          </button>
        </div>
      </div>
      <div class="p-4 space-y-4">
        <div v-if="selectedVersion.excerpt">
          <label class="block text-sm font-medium text-gray-500 dark:text-gray-400">Excerpt</label>
          <p class="mt-1 text-sm text-gray-900 dark:text-gray-100">{{ selectedVersion.excerpt }}</p>
        </div>
        <div v-if="selectedVersion.metaTitle">
          <label class="block text-sm font-medium text-gray-500 dark:text-gray-400">Meta Title</label>
          <p class="mt-1 text-sm text-gray-900 dark:text-gray-100">{{ selectedVersion.metaTitle }}</p>
        </div>
        <div v-if="selectedVersion.metaDescription">
          <label class="block text-sm font-medium text-gray-500 dark:text-gray-400">Meta Description</label>
          <p class="mt-1 text-sm text-gray-900 dark:text-gray-100">{{ selectedVersion.metaDescription }}</p>
        </div>
        <div v-if="selectedVersion.content">
          <label class="block text-sm font-medium text-gray-500 dark:text-gray-400">Content</label>
          <pre class="mt-1 text-sm text-gray-900 dark:text-gray-100 bg-gray-50 dark:bg-gray-700 rounded-md p-4 overflow-x-auto whitespace-pre-wrap">{{ selectedVersion.content }}</pre>
        </div>
      </div>
    </div>

    <!-- Version list -->
    <div v-else>
      <div v-if="versions.length === 0" class="text-center py-8 text-gray-500 dark:text-gray-400">
        No versions found.
      </div>
      <div v-else class="divide-y divide-gray-200 dark:divide-gray-700 border border-gray-200 dark:border-gray-700 rounded-lg">
        <div
          v-for="version in versions"
          :key="version.id"
          class="flex items-center justify-between p-4 hover:bg-gray-50 dark:hover:bg-gray-700"
        >
          <div>
            <span class="font-medium">Version {{ version.versionNumber }}</span>
            <span v-if="version.id === publishedVersionId" class="ml-2 inline-flex items-center px-2 py-0.5 text-xs font-medium bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300 rounded-full">
              Published
            </span>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">{{ formatDate(version.createdAt) }}</p>
          </div>
          <div class="flex items-center gap-2">
            <button
              class="px-3 py-1.5 text-sm text-gray-600 dark:text-gray-400 border border-gray-300 dark:border-gray-600 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700"
              @click="viewVersion(version.id)"
            >
              View
            </button>
            <button
              v-if="version.id !== publishedVersionId"
              class="px-3 py-1.5 text-sm bg-primary-600 text-white rounded-md hover:bg-primary-700 disabled:opacity-50"
              :disabled="isPublishing"
              @click="handlePublish(version.id)"
            >
              Publish
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
