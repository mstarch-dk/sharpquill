<script setup lang="ts">
import type { BlockInputDto } from '../../types/layout'
import type { EditorProps } from '../types'

const props = defineProps<EditorProps>()
const emit = defineEmits<{ update: [block: BlockInputDto] }>()

function updateCtaField(field: string, val: string) {
  emit('update', { ...props.block, [field]: val || undefined })
}
</script>

<template>
  <div class="space-y-3">
    <EditorLinkableField
      :model-value="block.title || ''"
      label="Title"
      placeholder="CTA heading"
      @update:model-value="(val: string) => updateCtaField('title', val)"
    />
    <EditorLinkableField
      :model-value="block.description || ''"
      label="Description"
      placeholder="Supporting text (optional)"
      multiline
      :rows="2"
      @update:model-value="(val: string) => updateCtaField('description', val)"
    />
    <UiInput
      :model-value="block.buttonText || ''"
      label="Button Text"
      placeholder="e.g. Kontakt os"
      @update:model-value="(val: string) => updateCtaField('buttonText', val)"
    />
    <EditorUrlAutocomplete
      :model-value="block.buttonUrl || ''"
      label="Button URL"
      placeholder="/kontakt or https://..."
      @update:model-value="(val: string) => updateCtaField('buttonUrl', val)"
    />
  </div>
</template>
