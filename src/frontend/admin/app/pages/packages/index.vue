<script setup lang="ts">
import type { ArticleAdminDto } from '~/types/article'
import type { ImportValidationDto, ImportResultDto } from '~/types/export'

definePageMeta({
  middleware: 'auth'
})

useHead({
  title: 'Packages'
})

const { exportContent, validateImport, importContent, downloadBlob } = useContentPackage()
const { getArticles } = useAdminArticles()
const { getPages } = useAdminPages()

// Export state
const exportType = ref<string | null>(null)
const selectiveExport = ref(false)
const selectedIds = ref<number[]>([])
const contentItems = ref<ArticleAdminDto[]>([])
const isLoadingItems = ref(false)
const isExporting = ref(false)
const exportError = ref('')

// Data export options
const includeTags = ref(false)
const includeCategories = ref(false)
const includeSettings = ref(false)

// Import state
const importFile = ref<File | null>(null)
const isDragOver = ref(false)
const isValidating = ref(false)
const isImporting = ref(false)
const validation = ref<ImportValidationDto | null>(null)
const importResult = ref<ImportResultDto | null>(null)
const importError = ref('')

// Load content items when export type changes or selective mode is toggled
watch([exportType, selectiveExport], async () => {
  if (!selectiveExport.value) {
    contentItems.value = []
    selectedIds.value = []
    return
  }

  isLoadingItems.value = true
  try {
    if (exportType.value === 'page') {
      const response = await getPages(1, 100)
      contentItems.value = response.data
    } else if (exportType.value === 'article') {
      const response = await getArticles(1, 100)
      contentItems.value = response.data
    } else {
      const [articlesRes, pagesRes] = await Promise.all([
        getArticles(1, 100),
        getPages(1, 100)
      ])
      contentItems.value = [...articlesRes.data, ...pagesRes.data]
    }
    selectedIds.value = []
  } catch {
    contentItems.value = []
  } finally {
    isLoadingItems.value = false
  }
})

function toggleItem(id: number) {
  const index = selectedIds.value.indexOf(id)
  if (index === -1) {
    selectedIds.value.push(id)
  } else {
    selectedIds.value.splice(index, 1)
  }
}

function selectAll() {
  selectedIds.value = contentItems.value.map(item => item.id)
}

function deselectAll() {
  selectedIds.value = []
}

const isExportDisabled = computed(() => {
  if (selectiveExport.value && selectedIds.value.length === 0) {
    return !includeTags.value && !includeCategories.value && !includeSettings.value
  }
  return false
})

// Export handler
async function handleExport() {
  isExporting.value = true
  exportError.value = ''

  try {
    const ids = selectiveExport.value && selectedIds.value.length > 0
      ? selectedIds.value
      : null
    const blob = await exportContent({
      contentType: exportType.value,
      articleIds: ids,
      includeTags: includeTags.value,
      includeCategories: includeCategories.value,
      includeSettings: includeSettings.value
    })
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19)
    downloadBlob(blob, `sharpquill-export-${timestamp}.zip`)
  } catch (e: unknown) {
    const err = e as Error
    exportError.value = err.message || 'Export failed'
  } finally {
    isExporting.value = false
  }
}

// File selection
function handleFileSelect(event: Event) {
  const input = event.target as HTMLInputElement
  if (input.files?.[0]) {
    setFile(input.files[0])
  }
}

function handleDrop(event: DragEvent) {
  isDragOver.value = false
  if (event.dataTransfer?.files?.[0]) {
    setFile(event.dataTransfer.files[0])
  }
}

function setFile(file: File) {
  importFile.value = file
  validation.value = null
  importResult.value = null
  importError.value = ''
}

function clearFile() {
  importFile.value = null
  validation.value = null
  importResult.value = null
  importError.value = ''
}

// Validate handler
async function handleValidate() {
  if (!importFile.value) return

  isValidating.value = true
  importError.value = ''
  validation.value = null

  try {
    const response = await validateImport(importFile.value)
    validation.value = response.data
  } catch (e: unknown) {
    const apiError = e as { data?: { error?: { message?: string } } }
    importError.value = apiError.data?.error?.message || 'Validation failed'
  } finally {
    isValidating.value = false
  }
}

// Import handler
async function handleImport() {
  if (!importFile.value) return

  isImporting.value = true
  importError.value = ''

  try {
    const response = await importContent(importFile.value)
    importResult.value = response.data
    validation.value = null
  } catch (e: unknown) {
    const apiError = e as { data?: { error?: { message?: string } } }
    importError.value = apiError.data?.error?.message || 'Import failed'
  } finally {
    isImporting.value = false
  }
}

function getStatusLabel(status: number): string {
  switch (status) {
    case 0: return 'Draft'
    case 1: return 'Published'
    case 2: return 'Archived'
    default: return 'Unknown'
  }
}

function getTypeLabel(contentType: number): string {
  return contentType === 1 ? 'Page' : 'Article'
}
</script>

<template>
  <div>
    <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-6">Packages</h1>

    <div class="grid grid-cols-1 lg:grid-cols-2 gap-8">
      <!-- Export Section -->
      <div class="bg-white dark:bg-gray-800 rounded-lg shadow dark:shadow-gray-900/30 p-6">
        <h2 class="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Export</h2>
        <p class="text-sm text-gray-600 dark:text-gray-400 mb-4">
          Export content as a ZIP package for backup or migration.
        </p>

        <div class="space-y-3 mb-4">
          <label class="flex items-center">
            <input
              v-model="exportType"
              type="radio"
              :value="null"
              class="h-4 w-4 text-primary-600 focus:ring-primary-500"
            />
            <span class="ml-2 text-sm text-gray-700 dark:text-gray-300">All Content</span>
          </label>
          <label class="flex items-center">
            <input
              v-model="exportType"
              type="radio"
              value="article"
              class="h-4 w-4 text-primary-600 focus:ring-primary-500"
            />
            <span class="ml-2 text-sm text-gray-700 dark:text-gray-300">Articles Only</span>
          </label>
          <label class="flex items-center">
            <input
              v-model="exportType"
              type="radio"
              value="page"
              class="h-4 w-4 text-primary-600 focus:ring-primary-500"
            />
            <span class="ml-2 text-sm text-gray-700 dark:text-gray-300">Pages Only</span>
          </label>
        </div>

        <label class="flex items-center mb-4">
          <input
            v-model="selectiveExport"
            type="checkbox"
            class="h-4 w-4 rounded text-primary-600 focus:ring-primary-500"
          />
          <span class="ml-2 text-sm text-gray-700 dark:text-gray-300">Select specific items</span>
        </label>

        <!-- Item Selection List -->
        <div v-if="selectiveExport" class="mb-4">
          <div v-if="isLoadingItems" class="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">
            Loading items...
          </div>
          <div v-else-if="contentItems.length > 0" class="border dark:border-gray-600 rounded-lg max-h-64 overflow-y-auto">
            <div class="flex items-center justify-between px-3 py-2 bg-gray-50 dark:bg-gray-700 border-b dark:border-gray-600">
              <span class="text-xs text-gray-500 dark:text-gray-400">{{ selectedIds.length }} of {{ contentItems.length }} selected</span>
              <div class="flex gap-2">
                <button class="text-xs text-primary-600 hover:text-primary-800" @click="selectAll">Select all</button>
                <button class="text-xs text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300" @click="deselectAll">Clear</button>
              </div>
            </div>
            <label
              v-for="item in contentItems"
              :key="item.id"
              class="flex items-center px-3 py-2 hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer border-b dark:border-gray-600 last:border-b-0"
            >
              <input
                type="checkbox"
                :checked="selectedIds.includes(item.id)"
                class="h-4 w-4 rounded text-primary-600 focus:ring-primary-500"
                @change="toggleItem(item.id)"
              />
              <div class="ml-3 flex-1 min-w-0">
                <p class="text-sm text-gray-900 dark:text-gray-100 truncate">{{ item.title }}</p>
                <p class="text-xs text-gray-500 dark:text-gray-400">
                  {{ getTypeLabel(item.contentType) }} &middot; {{ getStatusLabel(item.status) }}
                </p>
              </div>
            </label>
          </div>
          <div v-else class="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">
            No items found
          </div>
        </div>

        <!-- Additional Data Options -->
        <div class="mb-4 border-t dark:border-gray-600 pt-4">
          <p class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Include additional data</p>
          <div class="space-y-2">
            <label class="flex items-center">
              <input
                v-model="includeTags"
                type="checkbox"
                class="h-4 w-4 rounded text-primary-600 focus:ring-primary-500"
              />
              <span class="ml-2 text-sm text-gray-700 dark:text-gray-300">Tags</span>
            </label>
            <label class="flex items-center">
              <input
                v-model="includeCategories"
                type="checkbox"
                class="h-4 w-4 rounded text-primary-600 focus:ring-primary-500"
              />
              <span class="ml-2 text-sm text-gray-700 dark:text-gray-300">Categories</span>
            </label>
            <label class="flex items-center">
              <input
                v-model="includeSettings"
                type="checkbox"
                class="h-4 w-4 rounded text-primary-600 focus:ring-primary-500"
              />
              <span class="ml-2 text-sm text-gray-700 dark:text-gray-300">Site Settings</span>
            </label>
          </div>
        </div>

        <div v-if="exportError" class="mb-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg text-sm">
          {{ exportError }}
        </div>

        <UiButton
          :loading="isExporting"
          :disabled="isExportDisabled"
          @click="handleExport"
        >
          <UiIcon name="download" class="w-4 h-4 mr-1.5" />
          Export
        </UiButton>
      </div>

      <!-- Import Section -->
      <div class="bg-white dark:bg-gray-800 rounded-lg shadow dark:shadow-gray-900/30 p-6">
        <h2 class="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Import</h2>
        <p class="text-sm text-gray-600 dark:text-gray-400 mb-4">
          Import a previously exported ZIP package.
        </p>

        <!-- File Drop Zone -->
        <div
          v-if="!importFile"
          class="border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition-colors"
          :class="isDragOver ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20' : 'border-gray-300 dark:border-gray-600 hover:border-gray-400 dark:hover:border-gray-500'"
          @dragover.prevent="isDragOver = true"
          @dragleave="isDragOver = false"
          @drop.prevent="handleDrop"
          @click="($refs.fileInput as HTMLInputElement)?.click()"
        >
          <UiIcon name="cloud-upload" class="mx-auto h-10 w-10 text-gray-400 dark:text-gray-500 mb-3" />
          <p class="text-sm text-gray-600 dark:text-gray-400">
            Drop a ZIP file here or click to browse
          </p>
          <input
            ref="fileInput"
            type="file"
            accept=".zip"
            class="hidden"
            @change="handleFileSelect"
          />
        </div>

        <!-- Selected File -->
        <div v-else class="mb-4">
          <div class="flex items-center justify-between bg-gray-50 dark:bg-gray-700 rounded-lg px-4 py-3">
            <div class="flex items-center">
              <UiIcon name="document-text" class="h-5 w-5 text-gray-400 dark:text-gray-500 mr-2" />
              <span class="text-sm text-gray-700 dark:text-gray-300">{{ importFile.name }}</span>
              <span class="text-xs text-gray-500 dark:text-gray-400 ml-2">({{ (importFile.size / 1024).toFixed(1) }} KB)</span>
            </div>
            <button class="text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-400" @click="clearFile">
              <UiIcon name="x-mark" class="h-5 w-5" />
            </button>
          </div>

          <div class="flex gap-2 mt-3">
            <UiButton variant="secondary" :loading="isValidating" @click="handleValidate">
              Validate
            </UiButton>
            <UiButton
              :loading="isImporting"
              :disabled="!validation?.isValid"
              @click="handleImport"
            >
              <UiIcon name="upload" class="w-4 h-4 mr-1.5" />
              Import
            </UiButton>
          </div>
        </div>

        <!-- Error -->
        <div v-if="importError" class="mt-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg text-sm">
          {{ importError }}
        </div>

        <!-- Validation Results -->
        <div v-if="validation" class="mt-4 space-y-3">
          <div
            class="px-4 py-3 rounded-lg text-sm"
            :class="validation.isValid ? 'bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-700 dark:text-green-300' : 'bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300'"
          >
            {{ validation.isValid ? 'Package is valid and ready to import' : 'Package has errors' }}
          </div>

          <div v-if="validation.manifest" class="bg-gray-50 dark:bg-gray-700 rounded-lg px-4 py-3 text-sm">
            <p class="font-medium text-gray-700 dark:text-gray-300 mb-1">Package Contents</p>
            <p class="text-gray-600 dark:text-gray-400">Articles: {{ validation.manifest.contentCount.articles }}</p>
            <p class="text-gray-600 dark:text-gray-400">Pages: {{ validation.manifest.contentCount.pages }}</p>
            <p class="text-gray-600 dark:text-gray-400">Media files: {{ validation.manifest.mediaCount }}</p>
            <template v-if="validation.manifest.dataCount">
              <p v-if="validation.manifest.dataCount.tags > 0" class="text-gray-600 dark:text-gray-400">Tags: {{ validation.manifest.dataCount.tags }}</p>
              <p v-if="validation.manifest.dataCount.categories > 0" class="text-gray-600 dark:text-gray-400">Categories: {{ validation.manifest.dataCount.categories }}</p>
              <p v-if="validation.manifest.dataCount.settings > 0" class="text-gray-600 dark:text-gray-400">Settings: {{ validation.manifest.dataCount.settings }}</p>
            </template>
          </div>

          <div v-if="validation.conflicts.length > 0" class="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg px-4 py-3 text-sm">
            <p class="font-medium text-yellow-800 dark:text-yellow-300 mb-1">Conflicts (will be skipped)</p>
            <ul class="list-disc list-inside text-yellow-700 dark:text-yellow-400">
              <li v-for="conflict in validation.conflicts" :key="conflict">{{ conflict }}</li>
            </ul>
          </div>

          <div v-if="validation.warnings.length > 0" class="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg px-4 py-3 text-sm">
            <p class="font-medium text-blue-800 dark:text-blue-300 mb-1">New items to create</p>
            <ul class="list-disc list-inside text-blue-700 dark:text-blue-400">
              <li v-for="warning in validation.warnings" :key="warning">{{ warning }}</li>
            </ul>
          </div>

          <div v-if="validation.errors.length > 0" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg px-4 py-3 text-sm">
            <p class="font-medium text-red-800 dark:text-red-300 mb-1">Errors</p>
            <ul class="list-disc list-inside text-red-700 dark:text-red-400">
              <li v-for="error in validation.errors" :key="error">{{ error }}</li>
            </ul>
          </div>
        </div>

        <!-- Import Results -->
        <div v-if="importResult" class="mt-4 space-y-3">
          <div class="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg px-4 py-3 text-sm">
            <p class="font-medium text-green-800 dark:text-green-300 mb-2">Import Complete</p>
            <div class="text-green-700 dark:text-green-400 space-y-1">
              <p v-if="importResult.articlesImported > 0">Articles imported: {{ importResult.articlesImported }}</p>
              <p v-if="importResult.pagesImported > 0">Pages imported: {{ importResult.pagesImported }}</p>
              <p v-if="importResult.mediaImported > 0">Media files imported: {{ importResult.mediaImported }}</p>
              <p v-if="importResult.categoriesCreated > 0">Categories created: {{ importResult.categoriesCreated }}</p>
              <p v-if="importResult.tagsCreated > 0">Tags created: {{ importResult.tagsCreated }}</p>
              <p v-if="importResult.seriesCreated > 0">Series created: {{ importResult.seriesCreated }}</p>
              <p v-if="importResult.settingsImported > 0">Settings imported: {{ importResult.settingsImported }}</p>
            </div>
          </div>

          <div v-if="importResult.skipped > 0" class="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg px-4 py-3 text-sm">
            <p class="font-medium text-yellow-800 dark:text-yellow-300 mb-1">Skipped ({{ importResult.skipped }})</p>
            <ul class="list-disc list-inside text-yellow-700 dark:text-yellow-400">
              <li v-for="slug in importResult.skippedSlugs" :key="slug">{{ slug }}</li>
            </ul>
          </div>

          <div v-if="importResult.errors.length > 0" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg px-4 py-3 text-sm">
            <p class="font-medium text-red-800 dark:text-red-300 mb-1">Errors</p>
            <ul class="list-disc list-inside text-red-700 dark:text-red-400">
              <li v-for="error in importResult.errors" :key="error">{{ error }}</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
