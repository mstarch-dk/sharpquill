<script setup lang="ts">
import type { RenderProps } from '../types'

defineProps<RenderProps>()
</script>

<template>
  <div
    v-if="block.html"
    class="rounded-lg overflow-hidden bg-gray-900 dark:bg-gray-950 text-gray-100"
  >
    <div v-if="block.language" class="px-4 py-1.5 text-xs text-gray-400 dark:text-gray-500 bg-gray-800 dark:bg-gray-900 border-b border-gray-700 dark:border-gray-600">
      {{ block.language }}
    </div>
    <div class="p-4 overflow-x-auto text-sm font-mono [&_pre]:m-0 [&_pre]:bg-transparent [&_pre]:p-0 [&_code]:bg-transparent [&_code]:p-0 [&_code]:text-gray-100" v-html="sanitizeHtml(block.html)" />
  </div>
</template>
