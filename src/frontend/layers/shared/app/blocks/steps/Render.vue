<script setup lang="ts">
import type { RenderProps } from '../types'

defineProps<RenderProps>()
</script>

<template>
  <div class="space-y-6">
    <h2 v-if="block.headingHtml" class="text-3xl font-bold text-gray-900 dark:text-gray-100 [&_a]:text-primary-600 [&_a]:underline" v-html="sanitizeHtml(block.headingHtml)" />
    <h2 v-else-if="block.heading" class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ block.heading }}</h2>
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      <div
        v-for="(item, i) in (block.items || [])"
        :key="i"
        class="rounded-xl border border-gray-200 dark:border-gray-700 p-6 bg-white dark:bg-gray-800 shadow-sm space-y-3"
      >
        <span class="text-4xl font-bold text-primary-200 dark:text-primary-700 block">{{ item.number }}</span>
        <h3 v-if="item.titleHtml" class="text-lg font-semibold text-gray-900 dark:text-gray-100 [&_a]:text-primary-600 [&_a]:underline" v-html="sanitizeHtml(item.titleHtml)" />
        <h3 v-else class="text-lg font-semibold text-gray-900 dark:text-gray-100">{{ item.title }}</h3>
        <div
          v-if="item.descriptionHtml"
          class="text-gray-600 dark:text-gray-400 prose prose-sm dark:prose-invert prose-a:text-primary-600"
          v-html="sanitizeHtml(item.descriptionHtml)"
        />
        <p v-else class="text-gray-600 dark:text-gray-400">{{ item.description }}</p>
      </div>
    </div>
  </div>
</template>
