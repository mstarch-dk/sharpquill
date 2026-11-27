<script setup lang="ts">
import { getEmbedComponent, getEmbedDefinition } from '../../embeds/registry'
import type { RenderProps } from '../types'

const props = defineProps<RenderProps>()

const embed = computed(() => getEmbedDefinition(props.block.componentName))
const component = computed(() => getEmbedComponent(props.block.componentName))
</script>

<template>
  <!-- The admin preview shows where the embed goes; the live component (forms, API calls) runs only on the website -->
  <div
    v-if="preview && embed"
    class="rounded-lg border-2 border-dashed border-gray-300 dark:border-gray-600 p-6 text-center text-sm text-gray-500 dark:text-gray-400"
  >
    {{ embed.label }}
  </div>
  <component :is="component" v-else-if="component" />
</template>
