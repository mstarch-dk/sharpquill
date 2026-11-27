<script setup lang="ts">
const props = defineProps<{
  url: string
  title?: string
  description?: string
  imageUrl?: string
  siteName?: string
}>()

const domain = computed(() => {
  try {
    return new URL(props.url).hostname.replace(/^www\./, '')
  } catch {
    return props.url
  }
})
</script>

<template>
  <a
    :href="safeHref(url)"
    target="_blank"
    rel="noopener noreferrer"
    class="block border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden shadow-sm hover:shadow-md hover:border-gray-300 dark:hover:border-gray-500 transition-all not-prose"
  >
    <div class="flex flex-col sm:flex-row">
      <img
        v-if="imageUrl"
        :src="imageUrl"
        :alt="title || ''"
        class="w-full sm:w-56 h-44 sm:h-auto object-cover flex-shrink-0"
      />
      <div class="px-5 py-4 min-w-0 flex-1 flex flex-col justify-center">
        <p v-if="siteName" class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide mb-1.5">
          {{ siteName }}
        </p>
        <p v-if="title" class="font-semibold text-gray-900 dark:text-gray-100 line-clamp-2 mb-1.5">
          {{ title }}
        </p>
        <p v-if="description" class="text-sm text-gray-600 dark:text-gray-400 line-clamp-3">
          {{ description }}
        </p>
        <p class="text-xs text-gray-400 dark:text-gray-500 mt-3">
          {{ domain }}
        </p>
      </div>
    </div>
  </a>
</template>
