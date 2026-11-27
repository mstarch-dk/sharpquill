<script setup lang="ts">
import type { TagDto } from '~/types/api'

interface Props {
  open: boolean
}

defineProps<Props>()

const emit = defineEmits<{
  close: []
  created: [tag: TagDto]
}>()

const { createTag } = useAdminTags()

const name = ref('')
const slug = ref('')
const autoSlug = ref(true)
const errors = ref<Record<string, string>>({})
const isSubmitting = ref(false)

function generateSlug(text: string): string {
  return text
    .toLowerCase()
    .trim()
    .replace(/æ/g, 'ae')
    .replace(/ø/g, 'oe')
    .replace(/å/g, 'aa')
    .replace(/[^\w\s-]/g, '')
    .replace(/[\s_-]+/g, '-')
    .replace(/^-+|-+$/g, '')
}

watch(name, (newName) => {
  if (autoSlug.value) {
    slug.value = generateSlug(newName)
  }
})

function reset() {
  name.value = ''
  slug.value = ''
  autoSlug.value = true
  errors.value = {}
}

function validate(): boolean {
  errors.value = {}

  if (!name.value.trim()) {
    errors.value.name = 'Name is required'
  }

  if (!slug.value.trim()) {
    errors.value.slug = 'Slug is required'
  } else if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(slug.value)) {
    errors.value.slug = 'Slug must contain only lowercase letters, numbers, and hyphens'
  }

  return Object.keys(errors.value).length === 0
}

async function handleSubmit() {
  if (!validate()) return

  isSubmitting.value = true

  try {
    const response = await createTag({
      name: name.value.trim(),
      slug: slug.value.trim()
    })
    emit('created', response.data)
    reset()
  } catch (e: unknown) {
    const apiError = e as { data?: { error?: { message?: string } } }
    errors.value.name = apiError.data?.error?.message || 'Failed to create tag'
  } finally {
    isSubmitting.value = false
  }
}

function handleClose() {
  reset()
  emit('close')
}
</script>

<template>
  <UiModal :open="open" title="Create Tag" @close="handleClose">
    <form @submit.prevent="handleSubmit" class="space-y-4">
      <UiInput
        v-model="name"
        label="Name"
        placeholder="Enter tag name"
        :error="errors.name"
        required
      />

      <div>
        <div class="flex items-center justify-between mb-1">
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">
            Slug <span class="text-red-500">*</span>
          </label>
          <label class="flex items-center text-sm text-gray-500 dark:text-gray-400">
            <input
              v-model="autoSlug"
              type="checkbox"
              class="h-4 w-4 rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500 mr-1.5"
            />
            Auto-generate
          </label>
        </div>
        <UiInput
          v-model="slug"
          placeholder="tag-slug"
          :error="errors.slug"
          :disabled="autoSlug"
          required
        />
      </div>
    </form>

    <template #footer>
      <UiButton variant="secondary" @click="handleClose">
        Cancel
      </UiButton>
      <UiButton :loading="isSubmitting" @click="handleSubmit">
        Create Tag
      </UiButton>
    </template>
  </UiModal>
</template>
