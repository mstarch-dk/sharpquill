<script setup lang="ts">
import type { BlockInputDto } from '../../types/layout'
import { blockEditorServicesKey, type EditorProps } from '../types'

const props = defineProps<EditorProps>()
const emit = defineEmits<{ update: [block: BlockInputDto] }>()

const services = inject(blockEditorServicesKey)

const tweetFetching = ref(false)
const tweetError = ref<string | undefined>()
let tweetFetchTimer: ReturnType<typeof setTimeout> | null = null

async function fetchTweetPreview(url: string) {
  if (!services || !url || (!url.includes('twitter.com') && !url.includes('x.com'))) return

  tweetFetching.value = true
  tweetError.value = undefined
  try {
    const response = await services.post<{ authorName?: string; authorHandle?: string; tweetText?: string; tweetDate?: string; profileUrl?: string }>(
      '/api/admin/tweet-preview',
      { url }
    )
    const preview = response.data
    emit('update', {
      type: 'tweet',
      url,
      title: preview.authorName || undefined,
      description: preview.tweetText || undefined,
      siteName: preview.authorHandle || undefined,
      imageUrl: preview.profileUrl || undefined,
      tweetDate: preview.tweetDate || undefined
    })
  } catch {
    tweetError.value = 'Could not fetch tweet preview'
  } finally {
    tweetFetching.value = false
  }
}

function handleTweetPaste(event: ClipboardEvent) {
  const pasted = event.clipboardData?.getData('text')?.trim()
  if (pasted && (pasted.includes('twitter.com') || pasted.includes('x.com'))) {
    event.preventDefault()
    emit('update', { ...props.block, url: pasted })
    nextTick(() => fetchTweetPreview(pasted))
  }
}

function handleTweetInput(val: string) {
  emit('update', { ...props.block, url: val })
  if (tweetFetchTimer) clearTimeout(tweetFetchTimer)
  tweetFetchTimer = setTimeout(() => {
    if (val.includes('twitter.com') || val.includes('x.com')) {
      fetchTweetPreview(val)
    }
  }, 800)
}
</script>

<template>
  <div class="space-y-3">
    <div>
      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Tweet URL</label>
      <input
        type="url"
        :value="block.url || ''"
        placeholder="https://x.com/user/status/123..."
        class="w-full px-3 py-2 border rounded-lg shadow-sm dark:shadow-gray-900/30 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 border-gray-300 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
        @paste="handleTweetPaste"
        @input="handleTweetInput(($event.target as HTMLInputElement).value)"
      />
    </div>

    <!-- Loading state -->
    <div v-if="tweetFetching" class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400 py-2">
      <UiLoadingSpinner class="h-4 w-4" />
      Fetching tweet preview...
    </div>

    <!-- Error state -->
    <p v-else-if="tweetError" class="text-sm text-red-600 dark:text-red-400">{{ tweetError }}</p>

    <!-- Inline preview card -->
    <div v-else-if="block.title || block.description" class="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden bg-gray-50 dark:bg-gray-700 p-3">
      <div class="flex items-center gap-2 mb-2">
        <div
          v-if="block.imageUrl"
          class="w-8 h-8 rounded-full overflow-hidden bg-gray-200 dark:bg-gray-600 flex-shrink-0"
        >
          <img :src="block.imageUrl" :alt="block.title || ''" class="w-full h-full object-cover" />
        </div>
        <div class="min-w-0">
          <span class="text-sm font-semibold text-gray-900 dark:text-gray-100">{{ block.title }}</span>
          <span v-if="block.siteName" class="text-sm text-gray-500 dark:text-gray-400 ml-1">{{ block.siteName }}</span>
        </div>
      </div>
      <p v-if="block.description" class="text-sm text-gray-700 dark:text-gray-300 line-clamp-3">
        {{ block.description }}
      </p>
      <p v-if="block.tweetDate" class="text-xs text-gray-500 dark:text-gray-400 mt-1">
        {{ block.tweetDate }}
      </p>
    </div>

    <p v-else class="text-xs text-gray-500 dark:text-gray-400">
      Paste a link to a tweet from twitter.com or x.com
    </p>
  </div>
</template>
