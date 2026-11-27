<script setup lang="ts">
import type { RenderProps } from '../types'

defineProps<RenderProps>()
</script>

<template>
  <div
    v-if="block.html"
    class="prose prose-lg dark:prose-invert max-w-none prose-primary prose-headings:text-gray-900 dark:prose-headings:text-gray-100 prose-a:text-primary-600"
    v-html="sanitizeHtml(block.html)"
  />
</template>
