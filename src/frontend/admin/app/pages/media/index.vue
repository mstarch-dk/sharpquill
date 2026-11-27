<script setup lang="ts">
import type { MediaDto, MediaUsageDto } from '~/types/media'

definePageMeta({
  middleware: 'auth'
})

useHead({
  title: 'Media'
})

const { getMedia, uploadMedia, getMediaUsages, deleteMedia } = useAdminMedia()

const PAGE_SIZE = 24

const items = ref<MediaDto[]>([])
const page = ref(1)
const totalPages = ref(1)
const totalCount = ref(0)
const isLoading = ref(true)
const isUploading = ref(false)
const error = ref('')

const deleteModalOpen = ref(false)
const mediaToDelete = ref<MediaDto | null>(null)
const usages = ref<MediaUsageDto[] | null>(null)
const isDeleting = ref(false)
const deleteError = ref('')

async function fetchMedia() {
  isLoading.value = true
  error.value = ''

  try {
    const response = await getMedia(page.value, PAGE_SIZE)
    items.value = response.data
    totalPages.value = response.meta?.totalPages ?? 1
    totalCount.value = response.meta?.totalCount ?? response.data.length
    // The last item on a page was deleted: go back one page
    if (items.value.length === 0 && page.value > 1) {
      page.value--
    }
  } catch (e) {
    error.value = 'Failed to load media'
    console.error('Failed to load media:', e)
  } finally {
    isLoading.value = false
  }
}

watch(page, fetchMedia)
onMounted(fetchMedia)

async function handleFileSelect(event: Event) {
  const input = event.target as HTMLInputElement
  const files = Array.from(input.files ?? [])
  input.value = ''
  if (files.length === 0) return

  isUploading.value = true
  error.value = ''
  try {
    for (const file of files) {
      await uploadMedia(file)
    }
  } catch (e) {
    error.value = apiErrorMessage(e, 'Upload failed')
  } finally {
    isUploading.value = false
    page.value = 1
    await fetchMedia()
  }
}

async function handleDeleteClick(media: MediaDto) {
  mediaToDelete.value = media
  usages.value = null
  deleteError.value = ''
  deleteModalOpen.value = true

  try {
    usages.value = (await getMediaUsages(media.id)).data
  } catch (e) {
    usages.value = []
    deleteError.value = apiErrorMessage(e, 'Could not check where the file is used')
  }
}

async function handleDeleteConfirm() {
  if (!mediaToDelete.value) return

  isDeleting.value = true
  deleteError.value = ''
  try {
    // Only force when the admin saw the usages and confirmed "Delete anyway"
    await deleteMedia(mediaToDelete.value.id, (usages.value?.length ?? 0) > 0)
    deleteModalOpen.value = false
    mediaToDelete.value = null
    await fetchMedia()
  } catch (e) {
    deleteError.value = apiErrorMessage(e, 'Failed to delete media')
  } finally {
    isDeleting.value = false
  }
}

function apiErrorMessage(e: unknown, fallback: string): string {
  const data = (e as { data?: { error?: { message?: string } } })?.data
  return data?.error?.message ?? fallback
}

function previewUrl(media: MediaDto): string | null {
  if (!media.mimeType.startsWith('image/')) return null
  return media.thumbnailUrl ?? media.url
}
</script>

<template>
  <div>
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Media</h1>
        <p v-if="!isLoading" class="text-sm text-gray-500 dark:text-gray-400">
          {{ totalCount }} file{{ totalCount === 1 ? '' : 's' }}
        </p>
      </div>
      <label>
        <input
          type="file"
          multiple
          accept="image/jpeg,image/png,image/gif,image/webp,application/pdf"
          class="sr-only"
          :disabled="isUploading"
          @change="handleFileSelect"
        >
        <span
          class="inline-flex cursor-pointer items-center rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700"
          :class="{ 'opacity-50 cursor-wait': isUploading }"
        >
          <UiIcon name="upload" class="w-4 h-4 mr-1.5" />
          {{ isUploading ? 'Uploading...' : 'Upload' }}
        </span>
      </label>
    </div>

    <div v-if="error" class="mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg">
      {{ error }}
    </div>

    <div v-if="isLoading" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600" />
      <p class="mt-2 text-gray-500 dark:text-gray-400">Loading media...</p>
    </div>

    <div v-else-if="items.length === 0" class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8 text-center">
      <p class="text-gray-500 dark:text-gray-400">No media yet. Upload your first file.</p>
    </div>

    <template v-else>
      <ul class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
        <li
          v-for="media in items"
          :key="media.id"
          class="group overflow-hidden rounded-lg bg-white shadow dark:bg-gray-800 dark:shadow-gray-900/30"
        >
          <a :href="media.url" target="_blank" rel="noopener" class="block aspect-square bg-gray-100 dark:bg-gray-700">
            <img
              v-if="previewUrl(media)"
              :src="previewUrl(media)!"
              :alt="media.altText || media.originalFilename"
              loading="lazy"
              class="h-full w-full object-cover"
            >
            <span v-else class="flex h-full w-full items-center justify-center">
              <UiIcon name="document-text" class="h-10 w-10 text-gray-400" />
            </span>
          </a>
          <div class="flex items-start justify-between gap-2 p-2">
            <div class="min-w-0">
              <p class="truncate text-xs font-medium text-gray-900 dark:text-gray-100" :title="media.originalFilename">
                {{ media.originalFilename }}
              </p>
              <p class="text-xs text-gray-500 dark:text-gray-400">
                {{ formatFileSize(media.size) }}<template v-if="media.width && media.height"> · {{ media.width }}×{{ media.height }}</template>
              </p>
            </div>
            <button
              type="button"
              class="flex-shrink-0 rounded p-1 text-gray-400 hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-900/30 dark:hover:text-red-400"
              :title="`Delete ${media.originalFilename}`"
              :aria-label="`Delete ${media.originalFilename}`"
              @click="handleDeleteClick(media)"
            >
              <UiIcon name="trash" class="h-4 w-4" />
            </button>
          </div>
        </li>
      </ul>

      <div v-if="totalPages > 1" class="mt-6">
        <UiPagination v-model:current-page="page" :total-pages="totalPages" />
      </div>
    </template>

    <MediaDeleteMediaModal
      :open="deleteModalOpen"
      :media="mediaToDelete"
      :usages="usages"
      :loading="isDeleting"
      :error="deleteError"
      @close="deleteModalOpen = false"
      @confirm="handleDeleteConfirm"
    />
  </div>
</template>
