<script setup lang="ts">
import type { BlockInputDto } from '../../types/layout'
import type { EditorProps } from '../types'

const props = defineProps<EditorProps>()
const emit = defineEmits<{ update: [block: BlockInputDto] }>()

function updateContent(value: string) {
  emit('update', { ...props.block, content: value })
}
</script>

<template>
  <div>
    <EditorTipTapEditor
      :model-value="block.content || ''"
      placeholder="Write block content..."
      @update:model-value="updateContent"
    />
  </div>
</template>
