<script setup lang="ts">
import type { BlockInputDto } from '../../types/layout'
import { blockEditorServicesKey, type EditorProps } from '../types'

const props = defineProps<EditorProps>()
const emit = defineEmits<{ update: [block: BlockInputDto] }>()

const services = inject(blockEditorServicesKey)

const linkFetching = ref(false)
const linkError = ref<string | undefined>()
let linkFetchTimer: ReturnType<typeof setTimeout> | null = null

async function fetchLinkPreview(url: string) {
  if (!services || !url || !url.match(/^https?:\/\/.+/)) return

  linkFetching.value = true
  linkError.value = undefined
  try {
    const response = await services.post<{ title?: string; description?: string; imageUrl?: string; siteName?: string }>(
      '/api/admin/link-preview',
      { url }
    )
    const preview = response.data
    emit('update', {
      type: 'link',
      url,
      title: preview.title || undefined,
      description: preview.description || undefined,
      imageUrl: preview.imageUrl || undefined,
      siteName: preview.siteName || undefined
    })
  } catch {
    linkError.value = 'Could not fetch preview for this URL'
  } finally {
    linkFetching.value = false
  }
}

function handleLinkPaste(event: ClipboardEvent) {
  const pasted = event.clipboardData?.getData('text')?.trim()
  if (pasted && pasted.match(/^https?:\/\/.+/)) {
    event.preventDefault()
    emit('update', { ...props.block, url: pasted })
    nextTick(() => fetchLinkPreview(pasted))
  }
}

function handleLinkInput(val: string) {
  emit('update', { ...props.block, url: val })
  if (linkFetchTimer) clearTimeout(linkFetchTimer)
  linkFetchTimer = setTimeout(() => {
    if (val.match(/^https?:\/\/.+/)) {
      fetchLinkPreview(val)
    }
  }, 800)
}

function updateLinkTitle(val: string) {
  emit('update', { ...props.block, title: val || undefined })
}

function updateLinkDescription(val: string) {
  emit('update', { ...props.block, description: val || undefined })
}
</script>

<template>
  <div class="space-y-3">
    <div>
      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">URL</label>
      <input
        type="url"
        :value="block.url || ''"
        placeholder="Paste or type a URL..."
        class="w-full px-3 py-2 border rounded-lg shadow-sm dark:shadow-gray-900/30 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 border-gray-300 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
        @paste="handleLinkPaste"
        @input="handleLinkInput(($event.target as HTMLInputElement).value)"
      />
    </div>

    <!-- Loading state -->
    <div v-if="linkFetching" class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400 py-2">
      <UiLoadingSpinner class="h-4 w-4" />
      Fetching preview...
    </div>

    <!-- Error state -->
    <p v-else-if="linkError" class="text-sm text-red-600 dark:text-red-400">{{ linkError }}</p>

    <!-- Preview card -->
    <div v-else-if="block.title || block.description || block.imageUrl" class="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden bg-gray-50 dark:bg-gray-700">
      <img
        v-if="block.imageUrl"
        :src="block.imageUrl"
        :alt="block.title || ''"
        class="w-full h-40 object-cover"
      />
      <div class="p-3 space-y-2">
        <div v-if="block.siteName" class="text-xs text-gray-500 dark:text-gray-400 uppercase tracking-wide">
          {{ block.siteName }}
        </div>
        <UiInput
          :model-value="block.title || ''"
          label="Title"
          placeholder="Link title"
          @update:model-value="updateLinkTitle"
        />
        <UiTextarea
          :model-value="block.description || ''"
          label="Description"
          placeholder="Link description"
          :rows="2"
          @update:model-value="updateLinkDescription"
        />
      </div>
    </div>
  </div>
</template>
