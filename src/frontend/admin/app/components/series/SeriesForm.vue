<script setup lang="ts">
import type { SeriesAdminDetailDto, SeriesCreateDto, SeriesUpdateDto } from '~/types/series'
import { themeList } from '../../../../themes'

interface Props {
  series?: SeriesAdminDetailDto
  loading?: boolean
}

const props = defineProps<Props>()

const emit = defineEmits<{
  submit: [data: SeriesCreateDto | SeriesUpdateDto]
  cancel: []
}>()

const isEdit = computed(() => !!props.series)

const name = ref(props.series?.name || '')
const slug = ref(props.series?.slug || '')
const description = ref(props.series?.description || '')
const themeSlug = ref(props.series?.themeSlug || '')

const autoSlug = ref(!isEdit.value)
const errors = ref<Record<string, string>>({})

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

function validate(): boolean {
  errors.value = {}

  if (!name.value.trim()) {
    errors.value.name = 'Name is required'
  } else if (name.value.length > 200) {
    errors.value.name = 'Name must be less than 200 characters'
  }

  if (!slug.value.trim()) {
    errors.value.slug = 'Slug is required'
  } else if (slug.value.length > 200) {
    errors.value.slug = 'Slug must be less than 200 characters'
  } else if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(slug.value)) {
    errors.value.slug = 'Slug must contain only lowercase letters, numbers, and hyphens'
  }

  if (description.value && description.value.length > 2000) {
    errors.value.description = 'Description must be less than 2000 characters'
  }

  return Object.keys(errors.value).length === 0
}

function handleSubmit() {
  if (!validate()) return

  const data: SeriesCreateDto | SeriesUpdateDto = {
    name: name.value.trim(),
    slug: slug.value.trim(),
    description: description.value.trim() || undefined,
    themeSlug: themeSlug.value || undefined
  }

  emit('submit', data)
}
</script>

<template>
  <form @submit.prevent="handleSubmit" class="space-y-6">
    <div class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-6">
      <h2 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-6">
        {{ isEdit ? 'Edit Series' : 'Create Series' }}
      </h2>

      <div class="grid grid-cols-1 gap-6">
        <UiInput
          v-model="name"
          label="Name"
          placeholder="Enter series name"
          :error="errors.name"
          :maxlength="200"
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
            placeholder="series-slug"
            :error="errors.slug"
            :maxlength="200"
            :disabled="autoSlug"
            required
          />
        </div>

        <UiTextarea
          v-model="description"
          label="Description"
          placeholder="Brief description of the series"
          :error="errors.description"
          :maxlength="2000"
          :rows="3"
        />

        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Theme Override
          </label>
          <select
            v-model="themeSlug"
            class="block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm dark:shadow-gray-900/30 focus:border-primary-500 focus:ring-primary-500 sm:text-sm dark:bg-gray-800 dark:text-gray-100"
          >
            <option value="">Use site default</option>
            <option
              v-for="theme in themeList"
              :key="theme.slug"
              :value="theme.slug"
            >
              {{ theme.name }}
            </option>
          </select>
          <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
            Optionally override the site theme for this series
          </p>
        </div>
      </div>
    </div>

    <div class="flex justify-end gap-3">
      <UiButton type="button" variant="secondary" @click="$emit('cancel')">
        Cancel
      </UiButton>
      <UiButton type="submit" :loading="loading">
        {{ isEdit ? 'Update Series' : 'Create Series' }}
      </UiButton>
    </div>
  </form>
</template>
