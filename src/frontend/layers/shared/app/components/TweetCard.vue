<script setup lang="ts">
const props = defineProps<{
  url: string
  authorName?: string
  authorHandle?: string
  tweetText?: string
  tweetDate?: string
  profileUrl?: string
}>()

const { t } = useLayerText()

const hasMetadata = computed(() => props.authorName || props.tweetText)
const authorInitial = computed(() => (props.authorName || '?').charAt(0).toUpperCase())
</script>

<template>
  <div class="my-6">
    <!-- Full tweet card with metadata -->
    <a
      v-if="hasMetadata"
      :href="safeHref(url)"
      target="_blank"
      rel="noopener noreferrer"
      class="block border border-gray-200 dark:border-gray-700 rounded-xl p-5 max-w-lg mx-auto bg-white dark:bg-gray-800 hover:border-gray-300 dark:hover:border-gray-500 transition-colors not-prose"
    >
      <!-- Header: avatar + author info + X logo -->
      <div class="flex items-start justify-between mb-3">
        <div class="flex items-center gap-3">
          <div
            v-if="profileUrl"
            class="w-10 h-10 rounded-full overflow-hidden bg-gray-200 dark:bg-gray-700 flex-shrink-0"
          >
            <img
              :src="profileUrl"
              :alt="authorName || ''"
              class="w-full h-full object-cover"
              loading="lazy"
            />
          </div>
          <div
            v-else
            class="w-10 h-10 rounded-full bg-gray-800 dark:bg-gray-600 flex items-center justify-center text-white text-sm font-bold flex-shrink-0"
          >
            {{ authorInitial }}
          </div>
          <div class="min-w-0">
            <p class="font-semibold text-gray-900 dark:text-gray-100 text-sm truncate">
              {{ authorName }}
            </p>
            <p v-if="authorHandle" class="text-sm text-gray-500 dark:text-gray-400 truncate">
              {{ authorHandle }}
            </p>
          </div>
        </div>
        <UiIcon name="x-twitter" class="w-5 h-5 text-gray-400 dark:text-gray-500 flex-shrink-0" />
      </div>

      <!-- Tweet text -->
      <p v-if="tweetText" class="text-gray-900 dark:text-gray-100 text-[15px] leading-relaxed whitespace-pre-line mb-3">
        {{ tweetText }}
      </p>

      <!-- Date -->
      <p v-if="tweetDate" class="text-sm text-gray-500 dark:text-gray-400">
        {{ tweetDate }}
      </p>
    </a>

    <!-- Fallback: simple link card for tweets without metadata -->
    <a
      v-else
      :href="safeHref(url)"
      target="_blank"
      rel="noopener noreferrer"
      class="block border border-gray-200 dark:border-gray-700 rounded-xl p-4 max-w-lg mx-auto text-center bg-white dark:bg-gray-800 hover:border-gray-300 dark:hover:border-gray-500 transition-colors not-prose"
    >
      <UiIcon name="x-twitter" class="w-6 h-6 mx-auto mb-2 text-gray-400 dark:text-gray-500" />
      <span class="text-primary-600 dark:text-primary-400 hover:underline text-sm">
        {{ t('tweet.viewOnX') }}
      </span>
    </a>
  </div>
</template>
