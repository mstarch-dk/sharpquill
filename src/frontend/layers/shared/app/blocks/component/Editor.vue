<script setup lang="ts">
import { embedDefinitions } from '../../embeds/registry'
import type { BlockInputDto } from '../../types/layout'
import type { EditorProps } from '../types'

const props = defineProps<EditorProps>()
const emit = defineEmits<{ update: [block: BlockInputDto] }>()

const componentOptions = embedDefinitions.map(embed => ({ value: embed.name, label: embed.label }))

function updateComponent(val: string | number | undefined) {
  emit('update', { ...props.block, componentName: typeof val === 'string' ? val : undefined })
}
</script>

<template>
  <div class="space-y-3">
    <UiSelect
      :model-value="block.componentName || ''"
      :options="componentOptions"
      label="Component"
      @update:model-value="updateComponent"
    />
    <div class="rounded-lg border-2 border-dashed border-gray-300 dark:border-gray-600 p-4 text-center text-gray-400 text-sm">
      This component will be rendered on the website.
    </div>
  </div>
</template>
