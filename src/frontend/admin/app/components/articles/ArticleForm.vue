<script setup lang="ts">
import type { CategoryDto, TagDto } from '~/types/api'
import { ArticleStatus, ContentType, isScheduled, formatDateTime, type ArticleCreateDto, type ArticleUpdateDto, type ArticleAdminDto, type FeaturedImageSize, type LayoutInputDto } from '~/types/article'

interface Props {
  article?: ArticleAdminDto
  categories?: CategoryDto[]
  tags?: TagDto[]
  contentType?: ContentType
  loading?: boolean
}

const props = defineProps<Props>()

const emit = defineEmits<{
  submit: [data: ArticleCreateDto | ArticleUpdateDto]
  cancel: []
  change: []
}>()

const isEdit = computed(() => !!props.article)
const isPage = computed(() => props.contentType === ContentType.Page)
const entityLabel = computed(() => isPage.value ? 'Page' : 'Article')

// Form state
const title = ref(props.article?.title || '')
const slug = ref(props.article?.slug || '')
const content = ref(props.article?.content || '')
const excerpt = ref(props.article?.excerpt || '')
const metaTitle = ref(props.article?.metaTitle || '')
const metaDescription = ref(props.article?.metaDescription || '')
const categoryId = ref<number | undefined>(props.article?.category?.id)
const selectedTagIds = ref<number[]>(props.article?.tags?.map(t => t.id) || [])
const featuredImageId = ref<number | undefined>(props.article?.featuredImageId)
const featuredImageUrl = ref<string | undefined>(props.article?.featuredImageUrl)
const status = ref<ArticleStatus>(props.article?.status ?? ArticleStatus.Draft)
const commentsEnabled = ref(props.article?.commentsEnabled ?? true)
const requiresSignIn = ref(props.article?.requiresSignIn ?? false)
const pageStyle = ref<string>(props.article?.pageStyle ?? '')
const featuredImageSize = ref<FeaturedImageSize>(props.article?.featuredImageSize ?? 'medium')
const authorName = ref(props.article?.authorName || '')
// <input type="datetime-local"> works in the browser's local time without an offset; the API stores UTC.
function toLocalInputValue(iso: string): string {
  const date = new Date(iso)
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
}
const publishedAt = ref(props.article?.publishedAt ? toLocalInputValue(props.article.publishedAt) : '')
const publishedAtIso = computed(() => publishedAt.value ? new Date(publishedAt.value).toISOString() : undefined)
const scheduled = computed(() => isScheduled(status.value, publishedAtIso.value))

// Layout mode
const useLayoutMode = ref(!!props.article?.layout)
const layoutData = ref<LayoutInputDto>(
  props.article?.layout ?? { sections: [] }
)

// Auto-slug toggle (disabled for edit mode by default)
const autoSlug = ref(!isEdit.value)

// Validation errors
const errors = ref<Record<string, string>>({})

// --- Dirty tracking ---
// Snapshot baseline after nextTick to let TipTap normalize markdown→HTML roundtrip
const baseline = ref<string>('')

function buildSnapshot(): string {
  const sortedTags = [...selectedTagIds.value].sort((a, b) => a - b)
  return JSON.stringify({
    title: title.value,
    slug: slug.value,
    content: content.value,
    excerpt: excerpt.value,
    metaTitle: metaTitle.value,
    metaDescription: metaDescription.value,
    categoryId: categoryId.value,
    selectedTagIds: sortedTags,
    featuredImageId: featuredImageId.value,
    featuredImageSize: featuredImageSize.value,
    status: status.value,
    commentsEnabled: commentsEnabled.value,
    requiresSignIn: requiresSignIn.value,
    pageStyle: pageStyle.value,
    useLayoutMode: useLayoutMode.value,
    layoutData: layoutData.value,
    authorName: authorName.value,
    publishedAt: publishedAt.value
  })
}

const isDirty = computed(() => baseline.value !== '' && baseline.value !== buildSnapshot())

onMounted(() => {
  nextTick(() => {
    baseline.value = buildSnapshot()
  })
})

function markClean() {
  baseline.value = buildSnapshot()
}

function getFormData(): ArticleCreateDto | ArticleUpdateDto {
  return {
    title: title.value.trim(),
    slug: slug.value.trim(),
    content: useLayoutMode.value ? undefined : (content.value || undefined),
    layout: useLayoutMode.value && layoutData.value.sections.length > 0
      ? layoutData.value
      : undefined,
    excerpt: excerpt.value.trim() || undefined,
    metaTitle: metaTitle.value.trim() || undefined,
    metaDescription: metaDescription.value.trim() || undefined,
    featuredImageId: featuredImageId.value || undefined,
    categoryId: categoryId.value || undefined,
    tagIds: selectedTagIds.value.length > 0 ? selectedTagIds.value : undefined,
    status: status.value,
    commentsEnabled: commentsEnabled.value,
    requiresSignIn: requiresSignIn.value,
    pageStyle: pageStyle.value || undefined,
    featuredImageSize: featuredImageSize.value,
    authorName: authorName.value.trim() || undefined,
    publishedAt: publishedAtIso.value
  }
}

// Emit change on any field mutation
watch(
  [title, slug, content, excerpt, metaTitle, metaDescription, categoryId, selectedTagIds, featuredImageId, featuredImageSize, status, commentsEnabled, requiresSignIn, pageStyle, useLayoutMode, layoutData, authorName, publishedAt],
  () => { emit('change') },
  { deep: true }
)

defineExpose({ isDirty, getFormData, markClean })

// Generate slug from title
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

// Watch title for auto-slug
watch(title, (newTitle) => {
  if (autoSlug.value) {
    slug.value = generateSlug(newTitle)
  }
})

// Validate layout blocks (mirrors backend IsValidLayout)
function validateLayout(): string | null {
  if (!useLayoutMode.value || layoutData.value.sections.length === 0) return null
  for (const section of layoutData.value.sections) {
    if (section.blocks.length === 0) return 'A section has no blocks'
    for (const block of section.blocks) {
      if (block.type === 'text' && !block.content?.trim()) return 'A text block has no content'
      if (block.type === 'image' && !block.mediaId) return 'An image block has no image selected'
      if (block.type === 'link' && !block.url) return 'A link block has no URL'
      if (block.type === 'code' && !block.content?.trim()) return 'A code block has no content'
      if (block.type === 'tweet' && !block.url) return 'A tweet block has no URL'
      if ((block.type === 'cards' || block.type === 'steps') && (!block.items || block.items.length === 0))
        return `A ${block.type} block has no items`
      if (block.type === 'cta' && !block.title?.trim()) return 'A CTA block requires a title'
    }
  }
  return null
}

// Validate form
function validate(): boolean {
  errors.value = {}

  if (!title.value.trim()) {
    errors.value.title = 'Title is required'
  } else if (title.value.length > 200) {
    errors.value.title = 'Title must be less than 200 characters'
  }

  if (!slug.value.trim()) {
    errors.value.slug = 'Slug is required'
  } else if (slug.value.length > 200) {
    errors.value.slug = 'Slug must be less than 200 characters'
  } else if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(slug.value)) {
    errors.value.slug = 'Slug must contain only lowercase letters, numbers, and hyphens'
  }

  if (excerpt.value && excerpt.value.length > 500) {
    errors.value.excerpt = 'Excerpt must be less than 500 characters'
  }

  if (metaTitle.value && metaTitle.value.length > 70) {
    errors.value.metaTitle = 'Meta title must be less than 70 characters'
  }

  if (metaDescription.value && metaDescription.value.length > 160) {
    errors.value.metaDescription = 'Meta description must be less than 160 characters'
  }

  const layoutError = validateLayout()
  if (layoutError) {
    errors.value.layout = layoutError
  }

  return Object.keys(errors.value).length === 0
}

// Handle form submission
function handleSubmit() {
  if (!validate()) return
  emit('submit', getFormData())
}

// Toggle tag selection
function toggleTag(tagId: number) {
  const index = selectedTagIds.value.indexOf(tagId)
  if (index === -1) {
    selectedTagIds.value.push(tagId)
  } else {
    selectedTagIds.value.splice(index, 1)
  }
}

// Inline tag creation
const localTags = ref<TagDto[]>([...(props.tags ?? [])])
const createTagModalOpen = ref(false)

watch(() => props.tags, (newTags) => {
  if (newTags) {
    // Merge in any tags not already in localTags
    const existingIds = new Set(localTags.value.map(t => t.id))
    for (const tag of newTags) {
      if (!existingIds.has(tag.id)) {
        localTags.value.push(tag)
      }
    }
  }
})

function handleTagCreated(tag: TagDto) {
  localTags.value.push(tag)
  selectedTagIds.value.push(tag.id)
  createTagModalOpen.value = false
}

// Category options for select
const categoryOptions = computed(() =>
  (props.categories ?? []).map(c => ({ value: c.id, label: c.name }))
)

// Status options
const statusOptions = [
  { value: ArticleStatus.Draft, label: 'Draft' },
  { value: ArticleStatus.Published, label: 'Published' },
  { value: ArticleStatus.Archived, label: 'Archived' }
]

// Page hero style options
const pageStyleOptions = [
  {
    value: '',
    label: 'Default',
    description: 'Header text, then featured image below',
    preview: 'bg-white border-2 border-gray-200'
  },
  {
    value: 'hero-overlay',
    label: 'Hero Overlay',
    description: 'Featured image fills the header with title text overlaid',
    preview: 'bg-gradient-to-b from-gray-600 to-gray-900'
  },
  {
    value: 'blurred-hero',
    label: 'Blurred Hero',
    description: 'Featured image blurred as background behind the header',
    preview: 'bg-gray-300'
  },
  {
    value: 'gradient-header',
    label: 'Gradient Header',
    description: 'Bold theme-color gradient fills the header area',
    preview: 'bg-gradient-to-br from-primary-500 to-primary-700'
  }
]

const featuredImageSizeOptions: { value: FeaturedImageSize; label: string; description: string }[] = [
  {
    value: 'extra-small',
    label: 'Extra small',
    description: 'Compact (about 224px wide), e.g. a portrait or logo.'
  },
  {
    value: 'small',
    label: 'Small',
    description: 'Use thumbnail variant for compact display.'
  },
  {
    value: 'medium',
    label: 'Medium',
    description: 'Use medium variant for balanced quality and size.'
  },
  {
    value: 'original',
    label: 'Original',
    description: 'Use the full-size original image.'
  }
]
</script>

<template>
  <form @submit.prevent="handleSubmit" class="space-y-6">
    <div class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-6">
      <h2 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-6">
        {{ isEdit ? `Edit ${entityLabel}` : `Create ${entityLabel}` }}
      </h2>

      <div class="grid grid-cols-1 gap-6">
        <!-- Title -->
        <UiInput
          v-model="title"
          label="Title"
          :placeholder="`Enter ${entityLabel.toLowerCase()} title`"
          :error="errors.title"
          :maxlength="200"
          required
        />

        <!-- Slug -->
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
            placeholder="article-slug"
            :error="errors.slug"
            :maxlength="200"
            :disabled="autoSlug"
            required
          />
        </div>

        <!-- Content Mode Toggle -->
        <div class="flex items-center gap-3">
          <label class="relative inline-flex items-center cursor-pointer">
            <input
              v-model="useLayoutMode"
              type="checkbox"
              class="sr-only peer"
            />
            <div class="w-9 h-5 bg-gray-200 dark:bg-gray-600 peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-primary-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 dark:after:border-gray-600 after:border after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:bg-primary-600"></div>
          </label>
          <span class="text-sm font-medium text-gray-700 dark:text-gray-300">
            {{ useLayoutMode ? 'Layout Mode' : 'Simple Editor' }}
          </span>
          <span class="text-xs text-gray-500 dark:text-gray-400">
            {{ useLayoutMode ? 'Build structured layouts with sections and blocks' : 'Single content editor' }}
          </span>
        </div>

        <!-- Simple Content Editor -->
        <EditorTipTapEditor
          v-if="!useLayoutMode"
          v-model="content"
          label="Content"
          :placeholder="`Write your ${entityLabel.toLowerCase()} content...`"
          :error="errors.content"
        />

        <!-- Layout Editor -->
        <div v-else>
          <ArticleLayoutEditor v-model="layoutData" />
          <p v-if="errors.layout" class="mt-2 text-sm text-red-600 dark:text-red-400">{{ errors.layout }}</p>
        </div>

        <!-- Featured Image -->
        <MediaPickerField
          v-model="featuredImageId"
          :image-url="featuredImageUrl"
          label="Featured Image"
          @update:image-url="featuredImageUrl = $event"
        />

        <UiSelect
          v-model="featuredImageSize"
          :options="featuredImageSizeOptions"
          label="Featured Image Size"
        />

        <!-- Excerpt -->
        <UiTextarea
          v-model="excerpt"
          label="Excerpt"
          placeholder="Brief summary of the article"
          :error="errors.excerpt"
          :maxlength="500"
          :rows="3"
        />

        <!-- Page Hero Style -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">Page Style</label>
          <div class="grid grid-cols-2 md:grid-cols-4 gap-3">
            <button
              v-for="option in pageStyleOptions"
              :key="option.value"
              type="button"
              class="flex flex-col items-start gap-2 p-3 rounded-lg border-2 text-left transition-all"
              :class="pageStyle === option.value
                ? 'border-primary-500 bg-primary-50 dark:bg-primary-950/30'
                : 'border-gray-200 dark:border-gray-600 hover:border-gray-300 dark:hover:border-gray-500'"
              @click="pageStyle = option.value"
            >
              <div class="w-full h-10 rounded-md" :class="option.preview" />
              <span class="text-xs font-medium text-gray-900 dark:text-gray-100">{{ option.label }}</span>
              <span class="text-xs text-gray-500 dark:text-gray-400 leading-tight">{{ option.description }}</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- SEO Section -->
    <div class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-6">
      <h3 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-4">SEO</h3>

      <div class="grid grid-cols-1 gap-6">
        <UiInput
          v-model="metaTitle"
          label="Meta Title"
          placeholder="SEO title (optional)"
          :error="errors.metaTitle"
          :maxlength="70"
        />

        <UiTextarea
          v-model="metaDescription"
          label="Meta Description"
          placeholder="SEO description (optional)"
          :error="errors.metaDescription"
          :maxlength="160"
          :rows="2"
        />
      </div>
    </div>

    <!-- Organization Section (articles only) -->
    <div v-if="!isPage" class="bg-white shadow rounded-lg p-6">
      <h3 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-4">Organization</h3>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <!-- Category -->
        <UiSelect
          v-model="categoryId"
          :options="categoryOptions"
          label="Category"
          placeholder="Select a category"
        />

        <!-- Status -->
        <UiSelect
          v-model="status"
          :options="statusOptions"
          label="Status"
          required
        />

        <!-- Author Name -->
        <UiInput
          v-model="authorName"
          label="Author Name"
          placeholder="Display name for the author"
          :maxlength="200"
        />

        <!-- Publish Date -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Publish Date</label>
          <input
            v-model="publishedAt"
            type="datetime-local"
            class="block w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-sm text-gray-900 dark:text-gray-100 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
          />
          <p v-if="scheduled" class="mt-1 text-xs font-medium text-blue-700 dark:text-blue-300" data-testid="scheduled-hint">
            Scheduled: goes live {{ formatDateTime(publishedAtIso!) }} (your local time). Hidden on the public site until then.
          </p>
          <p v-else class="mt-1 text-xs text-gray-500 dark:text-gray-400">Leave empty to use current date when publishing. A future date with status Published schedules the article.</p>
        </div>
      </div>

      <!-- Tags -->
      <div class="mt-6">
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Tags</label>
        <div class="flex flex-wrap gap-2">
          <button
            v-for="tag in localTags"
            :key="tag.id"
            type="button"
            class="inline-flex items-center px-3 py-1.5 rounded-full text-sm font-medium transition-colors"
            :class="selectedTagIds.includes(tag.id)
              ? 'bg-primary-100 text-primary-800 ring-2 ring-primary-500'
              : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'"
            @click="toggleTag(tag.id)"
          >
            {{ tag.name }}
          </button>
          <button
            type="button"
            class="inline-flex items-center px-3 py-1.5 rounded-full text-sm font-medium border-2 border-dashed border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 hover:border-primary-400 hover:text-primary-600 dark:hover:border-primary-500 dark:hover:text-primary-400 transition-colors"
            @click="createTagModalOpen = true"
          >
            <UiIcon name="plus" class="w-3.5 h-3.5 mr-1" />
            New Tag
          </button>
        </div>
      </div>

      <ArticlesCreateTagModal
        :open="createTagModalOpen"
        @close="createTagModalOpen = false"
        @created="handleTagCreated"
      />

      <!-- Comments Toggle -->
      <div class="mt-6">
        <label class="flex items-center gap-3 cursor-pointer">
          <input
            v-model="commentsEnabled"
            type="checkbox"
            class="h-4 w-4 rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500"
          />
          <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Enable comments</span>
        </label>
        <p class="mt-1 text-xs text-gray-500 dark:text-gray-400 ml-7">Allow visitors to post comments on this article</p>
      </div>

      <!-- Requires Sign-In Toggle -->
      <div class="mt-6">
        <label class="flex items-center gap-3 cursor-pointer">
          <input
            v-model="requiresSignIn"
            type="checkbox"
            class="h-4 w-4 rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500"
          />
          <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Requires sign-in</span>
        </label>
        <p class="mt-1 text-xs text-gray-500 dark:text-gray-400 ml-7">Only signed-in website users can view this article</p>
      </div>
    </div>

    <!-- Status Section (pages only) -->
    <div v-if="isPage" class="bg-white shadow rounded-lg p-6">
      <h3 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-4">Settings</h3>
      <div class="max-w-xs">
        <UiSelect
          v-model="status"
          :options="statusOptions"
          label="Status"
          required
        />
      </div>
    </div>

    <!-- Actions -->
    <div class="flex justify-end gap-3">
      <UiButton type="button" variant="secondary" @click="$emit('cancel')">
        Cancel
      </UiButton>
      <UiButton type="submit" :loading="loading">
        {{ isEdit ? `Update ${entityLabel}` : `Create ${entityLabel}` }}
      </UiButton>
    </div>
  </form>
</template>
