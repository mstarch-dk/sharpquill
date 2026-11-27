<script setup lang="ts">
import type { MediaDto } from '~/types/media'

interface Props {
  open: boolean
  selectedMediaId?: number
}

const props = defineProps<Props>()

const emit = defineEmits<{
  select: [media: MediaDto]
  close: []
}>()

const { getMedia, uploadMedia } = useAdminMedia()

const mediaItems = ref<MediaDto[]>([])
const loading = ref(false)
const uploading = ref(false)
const selectedItem = ref<MediaDto | null>(null)
const page = ref(1)
const totalPages = ref(1)
const dragOver = ref(false)

// Cropper state
const cropperFile = ref<File | null>(null)
const showCropper = ref(false)

async function loadMedia() {
  loading.value = true
  try {
    const response = await getMedia(page.value, 20)
    mediaItems.value = response.data.filter(m => m.mimeType.startsWith('image/'))
    if (response.meta) {
      totalPages.value = response.meta.totalPages
    }
  } catch {
    // silently fail
  } finally {
    loading.value = false
  }
}

async function handleFileSelect(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  input.value = ''

  if (file.type.startsWith('image/') && !file.type.includes('svg') && !file.type.includes('pdf')) {
    cropperFile.value = file
    showCropper.value = true
  } else {
    await doUpload(file)
  }
}

function handleDrop(event: DragEvent) {
  dragOver.value = false
  const file = event.dataTransfer?.files[0]
  if (!file) return

  if (file.type.startsWith('image/') && !file.type.includes('svg') && !file.type.includes('pdf')) {
    cropperFile.value = file
    showCropper.value = true
  } else {
    doUpload(file)
  }
}

async function handleCrop(croppedFile: File) {
  showCropper.value = false
  cropperFile.value = null
  await doUpload(croppedFile)
}

function handleCropCancel() {
  showCropper.value = false
  if (cropperFile.value) {
    doUpload(cropperFile.value)
  }
  cropperFile.value = null
}

async function doUpload(file: File) {
  uploading.value = true
  try {
    const response = await uploadMedia(file)
    // Reload media to get the new item with full data
    await loadMedia()
    // Auto-select the uploaded item
    const uploaded = mediaItems.value.find(m => m.id === response.data.id)
    if (uploaded) {
      selectedItem.value = uploaded
    }
  } catch {
    // silently fail
  } finally {
    uploading.value = false
  }
}

function selectItem(item: MediaDto) {
  selectedItem.value = item
}

function confirmSelection() {
  if (selectedItem.value) {
    emit('select', selectedItem.value)
  }
}

watch(() => props.open, (isOpen) => {
  if (isOpen) {
    loadMedia()
    // Pre-select current item if any
    if (props.selectedMediaId) {
      const existing = mediaItems.value.find(m => m.id === props.selectedMediaId)
      if (existing) selectedItem.value = existing
    }
  } else {
    selectedItem.value = null
  }
})
</script>

<template>
  <UiModal :open="open" title="Select Image" @close="$emit('close')">
    <div class="space-y-4 min-h-[400px]">
      <!-- Cropper overlay -->
      <MediaImageCropper
        v-if="showCropper && cropperFile"
        :image-file="cropperFile"
        @crop="handleCrop"
        @cancel="handleCropCancel"
      />

      <!-- Upload zone -->
      <div
        v-if="!showCropper"
        class="border-2 border-dashed rounded-lg p-4 text-center transition-colors"
        :class="dragOver ? 'border-primary-400 bg-primary-50' : 'border-gray-300 dark:border-gray-600'"
        @dragover.prevent="dragOver = true"
        @dragleave="dragOver = false"
        @drop.prevent="handleDrop"
      >
        <div v-if="uploading" class="text-sm text-gray-500 dark:text-gray-400">Uploading...</div>
        <div v-else>
          <p class="text-sm text-gray-500 dark:text-gray-400 mb-2">Drag & drop an image or</p>
          <label class="inline-flex items-center px-3 py-1.5 text-sm font-medium text-primary-700 bg-primary-50 rounded-md cursor-pointer hover:bg-primary-100">
            Browse Files
            <input type="file" accept="image/jpeg,image/png,image/gif,image/webp" class="hidden" @change="handleFileSelect" />
          </label>
        </div>
      </div>

      <!-- Media grid -->
      <div v-if="!showCropper">
        <div v-if="loading" class="text-center py-8 text-sm text-gray-500 dark:text-gray-400">Loading...</div>
        <div v-else-if="mediaItems.length === 0" class="text-center py-8 text-sm text-gray-500 dark:text-gray-400">
          No images found. Upload one above.
        </div>
        <div v-else class="grid grid-cols-4 gap-2 max-h-[300px] overflow-y-auto">
          <button
            v-for="item in mediaItems"
            :key="item.id"
            type="button"
            class="relative aspect-square rounded-lg overflow-hidden border-2 transition-colors"
            :class="selectedItem?.id === item.id ? 'border-primary-500 ring-2 ring-primary-200' : 'border-transparent hover:border-gray-300 dark:hover:border-gray-600'"
            @click="selectItem(item)"
          >
            <img
              :src="item.url"
              :alt="item.altText || item.originalFilename"
              class="w-full h-full object-cover"
            />
            <div class="absolute bottom-0 inset-x-0 bg-black/50 text-white text-xs px-1 py-0.5 truncate">
              {{ item.originalFilename }}
            </div>
          </button>
        </div>

        <!-- Pagination -->
        <div v-if="totalPages > 1" class="flex justify-center gap-2 mt-2">
          <button
            type="button"
            class="text-sm text-primary-600 hover:text-primary-800 disabled:text-gray-400"
            :disabled="page <= 1"
            @click="page--; loadMedia()"
          >
            Previous
          </button>
          <span class="text-sm text-gray-500 dark:text-gray-400">{{ page }} / {{ totalPages }}</span>
          <button
            type="button"
            class="text-sm text-primary-600 hover:text-primary-800 disabled:text-gray-400"
            :disabled="page >= totalPages"
            @click="page++; loadMedia()"
          >
            Next
          </button>
        </div>
      </div>

      <!-- Selected info -->
      <div v-if="selectedItem && !showCropper" class="flex items-center gap-3 p-2 bg-gray-50 dark:bg-gray-700 rounded-lg">
        <img :src="selectedItem.url" class="w-12 h-12 object-cover rounded" />
        <div class="flex-1 min-w-0">
          <p class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">{{ selectedItem.originalFilename }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400">{{ formatFileSize(selectedItem.size) }}</p>
        </div>
      </div>
    </div>

    <template #footer>
      <UiButton variant="secondary" @click="$emit('close')">Cancel</UiButton>
      <UiButton :disabled="!selectedItem" @click="confirmSelection">Select</UiButton>
    </template>
  </UiModal>
</template>
