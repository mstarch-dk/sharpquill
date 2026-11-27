<script setup lang="ts">
import type { BlockInputDto, SectionInputDto } from '../../types/layout'
import type { EditorProps } from '../types'

const props = defineProps<EditorProps>()
const emit = defineEmits<{ update: [block: BlockInputDto] }>()

function updateSections(sections: SectionInputDto[]) {
  emit('update', { type: 'container', sections })
}
</script>

<template>
  <div class="border-l-2 border-primary-200 pl-4 space-y-3">
    <div class="text-xs text-gray-500 dark:text-gray-400 font-medium uppercase">
      Nested Container (Level {{ depth + 2 }})
    </div>

    <!-- The admin's draggable section list (admin/app/components/article-layout/SectionList.vue) -->
    <ArticleLayoutSectionList
      :sections="props.block.sections || []"
      :depth="depth + 1"
      add-label="+ Add Nested Section"
      @update="updateSections"
    />
  </div>
</template>
