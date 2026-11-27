<script setup lang="ts">
import type { RenderProps } from '../types'

defineProps<RenderProps>()
</script>

<template>
  <div v-if="block.sections" class="border-l-2 border-gray-200 dark:border-gray-700 pl-4">
    <ArticleLayout :layout="{ sections: block.sections }" :preview="preview" />
  </div>
</template>
