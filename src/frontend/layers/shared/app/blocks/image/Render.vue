<script setup lang="ts">
import type { RenderProps } from '../types'

defineProps<RenderProps>()
</script>

<template>
  <figure v-if="block.imageUrl">
    <img
      :src="block.imageUrl"
      :alt="block.alt || ''"
      class="w-full h-auto rounded-lg"
    />
    <figcaption v-if="block.captionHtml" class="mt-2 text-sm text-gray-500 dark:text-gray-400 text-center prose prose-sm dark:prose-invert prose-a:text-primary-600" v-html="sanitizeHtml(block.captionHtml)" />
    <figcaption v-else-if="block.caption" class="mt-2 text-sm text-gray-500 dark:text-gray-400 text-center">
      {{ block.caption }}
    </figcaption>
  </figure>
</template>
