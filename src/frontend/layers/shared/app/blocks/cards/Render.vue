<script setup lang="ts">
import type { RenderProps } from '../types'

defineProps<RenderProps>()
</script>

<template>
  <div class="space-y-6">
    <h2 v-if="block.headingHtml" class="text-3xl font-bold text-gray-900 dark:text-gray-100 [&_a]:text-primary-600 [&_a]:underline" v-html="sanitizeHtml(block.headingHtml)" />
    <h2 v-else-if="block.heading" class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ block.heading }}</h2>
    <div
      v-if="block.html"
      class="prose prose-lg dark:prose-invert max-w-none prose-primary"
      v-html="sanitizeHtml(block.html)"
    />
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div
        v-for="(item, i) in (block.items || [])"
        :key="i"
        class="rounded-xl border border-gray-200 dark:border-gray-700 p-6 bg-white dark:bg-gray-800 shadow-sm space-y-3 transition-shadow hover:shadow-md"
      >
        <FeatureIcon v-if="item.icon" :icon="item.icon" />
        <h3 v-if="item.titleHtml" class="text-lg font-semibold text-gray-900 dark:text-gray-100 [&_a]:text-inherit [&_a]:no-underline hover:[&_a]:text-primary-600 hover:[&_a]:underline" v-html="sanitizeHtml(item.titleHtml)" />
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
