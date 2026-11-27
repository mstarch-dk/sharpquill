<script setup lang="ts">
import type { RenderProps } from '../types'

defineProps<RenderProps>()

const { t } = useLayerText()
</script>

<template>
  <div class="text-center bg-primary-50 dark:bg-primary-900/20 rounded-2xl p-10 space-y-4">
    <h2 v-if="block.titleHtml" class="text-3xl font-bold text-gray-900 dark:text-gray-100 [&_a]:text-primary-600 [&_a]:underline" v-html="sanitizeHtml(block.titleHtml)" />
    <h2 v-else class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ block.title }}</h2>
    <div
      v-if="block.descriptionHtml"
      class="text-lg text-gray-600 dark:text-gray-400 max-w-2xl mx-auto prose prose-lg dark:prose-invert prose-a:text-primary-600"
      v-html="sanitizeHtml(block.descriptionHtml)"
    />
    <p v-else-if="block.description" class="text-lg text-gray-600 dark:text-gray-400 max-w-2xl mx-auto">{{ block.description }}</p>
    <a
      v-if="block.buttonUrl"
      :href="safeHref(block.buttonUrl)"
      class="inline-flex items-center px-8 py-3 bg-primary-600 text-white font-semibold rounded-xl hover:bg-primary-700 transition-colors"
    >
      {{ block.buttonText || t('common.readMore') }}
    </a>
  </div>
</template>
