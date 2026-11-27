<script setup lang="ts">
import type { BlockInputDto } from '../../types/layout'
import type { EditorProps } from '../types'

const props = defineProps<EditorProps>()
const emit = defineEmits<{ update: [block: BlockInputDto] }>()

const codeLanguageOptions = [
  { value: '', label: 'Plain Text' },
  { value: 'javascript', label: 'JavaScript' },
  { value: 'typescript', label: 'TypeScript' },
  { value: 'python', label: 'Python' },
  { value: 'csharp', label: 'C#' },
  { value: 'html', label: 'HTML' },
  { value: 'css', label: 'CSS' },
  { value: 'json', label: 'JSON' },
  { value: 'bash', label: 'Bash' },
  { value: 'sql', label: 'SQL' },
  { value: 'go', label: 'Go' },
  { value: 'rust', label: 'Rust' },
  { value: 'java', label: 'Java' },
  { value: 'xml', label: 'XML' },
  { value: 'yaml', label: 'YAML' },
  { value: 'markdown', label: 'Markdown' }
]

function updateCodeContent(value: string) {
  emit('update', { ...props.block, content: value })
}

function updateCodeLanguage(value: string | number | undefined) {
  const normalized = typeof value === 'string' ? value : (value === undefined ? '' : String(value))
  emit('update', { ...props.block, language: normalized || undefined })
}
</script>

<template>
  <div class="space-y-3">
    <UiSelect
      :model-value="block.language || ''"
      :options="codeLanguageOptions"
      label="Language"
      @update:model-value="updateCodeLanguage"
    />
    <div>
      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Code</label>
      <textarea
        :value="block.content || ''"
        placeholder="Paste or type code here..."
        rows="10"
        :spellcheck="false"
        autocorrect="off"
        autocapitalize="off"
        class="w-full px-3 py-2 border rounded-lg shadow-sm dark:shadow-gray-900/30 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 border-gray-300 dark:border-gray-600 font-mono text-sm bg-gray-50 dark:bg-gray-700 dark:text-gray-100"
        @input="updateCodeContent(($event.target as HTMLTextAreaElement).value)"
      />
    </div>
  </div>
</template>
