<script setup lang="ts">
import { useEditor, EditorContent } from '@tiptap/vue-3'
import { Placeholder } from '@tiptap/extensions'
import type { MediaDto } from '~/types/media'

interface Props {
  modelValue: string
  placeholder?: string
  error?: string
  label?: string
  showFullscreenButton?: boolean
  fullscreen?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  placeholder: 'Start writing...',
  showFullscreenButton: true,
  fullscreen: false
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const isFullscreen = ref(false)

const editorClass = computed(() => [
  // dark:prose-invert: without it headings, bold and inline code keep the light theme's dark colors in dark mode.
  // Links carry text-primary-600 (markdownConversion.ts), too dark on gray-800, so dark mode lightens them here.
  'max-w-none focus:outline-none px-4 py-3 dark:prose-invert dark:prose-a:text-primary-400',
  props.fullscreen
    ? 'prose prose-base md:prose-lg min-h-[60vh] leading-8'
    : 'prose prose-sm min-h-[200px]'
].join(' '))

// Markdown <-> HTML conversion and the document schema live in utils/markdownConversion.ts (unit-tested there)
const initialHtml = computed(() => markdownToHtml(props.modelValue))

// Image size toolbar state — shown contextually in the main toolbar when an image is selected
const isImageSelected = ref(false)
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const selectedImageAttrs = ref<Record<string, any>>({})

const IMAGE_SIZES = [
  { key: 'small', label: 'S' },
  { key: 'medium', label: 'M' },
  { key: 'large', label: 'L' },
  { key: 'full', label: 'Full' },
] as const

function updateImageToolbar() {
  if (!editor.value) return
  const active = editor.value.isActive('image')
  isImageSelected.value = active
  if (active) {
    selectedImageAttrs.value = editor.value.getAttributes('image')
  }
}

function setImageSize(size: string) {
  if (!editor.value) return
  const a = editor.value.getAttributes('image')
  const newSrc: string =
    size === 'small'  ? (a.srcThumb  || a.srcOriginal || a.src || '') :
    size === 'medium' ? (a.srcMedium || a.srcOriginal || a.src || '') :
                        (a.srcOriginal || a.src || '')
  editor.value.chain().focus().updateAttributes('image', { size, src: newSrc }).run()
  selectedImageAttrs.value = { ...a, size, src: newSrc }
}

const editor = useEditor({
  content: initialHtml.value,
  extensions: [
    ...editorContentExtensions(),
    Placeholder.configure({
      placeholder: props.placeholder
    })
  ],
  editorProps: {
    attributes: {
      class: editorClass.value,
      spellcheck: 'false',
      autocorrect: 'off',
      autocapitalize: 'off'
    }
  },
  onUpdate: ({ editor }) => {
    const html = editor.getHTML()
    const markdown = htmlToMarkdown(html)
    emit('update:modelValue', markdown)
    updateImageToolbar()
  },
  onSelectionUpdate: () => {
    updateImageToolbar()
  },
})

// Watch for external changes to modelValue
watch(() => props.modelValue, (newValue) => {
  if (!editor.value) return

  const currentMarkdown = htmlToMarkdown(editor.value.getHTML())
  if (newValue !== currentMarkdown) {
    const html = markdownToHtml(newValue)
    editor.value.commands.setContent(html, { emitUpdate: false })
  }
})

onBeforeUnmount(() => {
  editor.value?.destroy()
})


const showImagePicker = ref(false)

function handleImageSelect(media: MediaDto) {
  showImagePicker.value = false
  editor.value?.chain().focus().setImage({
    src: media.url,
    alt: media.altText || '',
    size: 'full',
    srcOriginal: media.url,
    srcMedium: media.mediumUrl ?? null,
    srcThumb: media.thumbnailUrl ?? null,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any -- custom image attrs are not in tiptap's types
  } as any).run()
}

const showLinkPicker = ref(false)
const linkPickerInitialUrl = ref<string | undefined>()
const linkPickerInitialText = ref<string | undefined>()

function openLinkPicker() {
  if (!editor.value) return
  const existingHref = editor.value.getAttributes('link').href as string | undefined
  const { from, to } = editor.value.state.selection
  const selectedText = editor.value.state.doc.textBetween(from, to, '')
  linkPickerInitialUrl.value = existingHref
  linkPickerInitialText.value = selectedText || undefined
  showLinkPicker.value = true
}

function handleLinkConfirm({ url, text }: { url: string; text?: string }) {
  showLinkPicker.value = false
  if (!editor.value) return
  const chain = editor.value.chain().focus()
  if (text) {
    const { from, to } = editor.value.state.selection
    if (from === to) {
      // No selection — insert new text with link
      chain.insertContent(`<a href="${url}">${text}</a>`).run()
      return
    }
  }
  chain.setLink({ href: url }).run()
}
</script>

<template>
  <div>
    <label v-if="label" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
      {{ label }}
    </label>
    <div
      class="border rounded-lg overflow-hidden"
      :class="error ? 'border-red-300 dark:border-red-700' : 'border-gray-300 dark:border-gray-600'"
    >
      <!-- Toolbar -->
      <div v-if="editor" class="flex flex-wrap items-center gap-1 px-2 py-1.5 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-700">
        <button
          type="button"
          class="p-1.5 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          :class="{ 'bg-gray-200 dark:bg-gray-600': editor.isActive('bold') }"
          title="Bold"
          @click="editor.chain().focus().toggleBold().run()"
        >
          <UiIcon name="bold" class="w-4 h-4" />
        </button>

        <button
          type="button"
          class="p-1.5 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          :class="{ 'bg-gray-200 dark:bg-gray-600': editor.isActive('italic') }"
          title="Italic"
          @click="editor.chain().focus().toggleItalic().run()"
        >
          <UiIcon name="italic" class="w-4 h-4" />
        </button>

        <div class="w-px h-5 bg-gray-300 dark:bg-gray-600 mx-1" />

        <button
          type="button"
          class="p-1.5 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          :class="{ 'bg-gray-200 dark:bg-gray-600': editor.isActive('heading', { level: 1 }) }"
          title="Heading 1"
          @click="editor.chain().focus().toggleHeading({ level: 1 }).run()"
        >
          <span class="text-xs font-bold">H1</span>
        </button>

        <button
          type="button"
          class="p-1.5 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          :class="{ 'bg-gray-200 dark:bg-gray-600': editor.isActive('heading', { level: 2 }) }"
          title="Heading 2"
          @click="editor.chain().focus().toggleHeading({ level: 2 }).run()"
        >
          <span class="text-xs font-bold">H2</span>
        </button>

        <button
          type="button"
          class="p-1.5 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          :class="{ 'bg-gray-200 dark:bg-gray-600': editor.isActive('heading', { level: 3 }) }"
          title="Heading 3"
          @click="editor.chain().focus().toggleHeading({ level: 3 }).run()"
        >
          <span class="text-xs font-bold">H3</span>
        </button>

        <div class="w-px h-5 bg-gray-300 dark:bg-gray-600 mx-1" />

        <button
          type="button"
          class="p-1.5 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          :class="{ 'bg-gray-200 dark:bg-gray-600': editor.isActive('bulletList') }"
          title="Bullet List"
          @click="editor.chain().focus().toggleBulletList().run()"
        >
          <UiIcon name="bullet-list" class="w-4 h-4" />
        </button>

        <button
          type="button"
          class="p-1.5 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          :class="{ 'bg-gray-200 dark:bg-gray-600': editor.isActive('orderedList') }"
          title="Numbered List"
          @click="editor.chain().focus().toggleOrderedList().run()"
        >
          <UiIcon name="ordered-list" class="w-4 h-4" />
        </button>

        <div class="w-px h-5 bg-gray-300 dark:bg-gray-600 mx-1" />

        <button
          type="button"
          class="p-1.5 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          :class="{ 'bg-gray-200 dark:bg-gray-600': editor.isActive('blockquote') }"
          title="Quote"
          @click="editor.chain().focus().toggleBlockquote().run()"
        >
          <UiIcon name="blockquote" class="w-4 h-4" />
        </button>

        <button
          type="button"
          class="p-1.5 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          :class="{ 'bg-gray-200 dark:bg-gray-600': editor.isActive('codeBlock') }"
          title="Code Block"
          @click="editor.chain().focus().toggleCodeBlock().run()"
        >
          <UiIcon name="code-block" class="w-4 h-4" />
        </button>

        <div class="w-px h-5 bg-gray-300 dark:bg-gray-600 mx-1" />

        <button
          type="button"
          class="p-1.5 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          :class="{ 'bg-gray-200 dark:bg-gray-600': editor.isActive('link') }"
          title="Link"
          @click="openLinkPicker"
        >
          <UiIcon name="link" class="w-4 h-4" />
        </button>

        <button
          type="button"
          class="p-1.5 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          title="Insert Image"
          @click="showImagePicker = true"
        >
          <UiIcon name="photo" class="w-4 h-4" />
        </button>

        <!-- Image size controls — shown when an image is selected -->
        <template v-if="isImageSelected">
          <div class="w-px h-5 bg-gray-300 dark:bg-gray-600 mx-1" />
          <button
            v-for="size in IMAGE_SIZES"
            :key="size.key"
            type="button"
            class="px-2 py-0.5 text-xs rounded transition-colors"
            :class="selectedImageAttrs.size === size.key
              ? 'bg-primary-100 text-primary-700 dark:bg-primary-900 dark:text-primary-300 font-medium'
              : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'"
            :title="`Image size: ${size.key}`"
            @mousedown.prevent="setImageSize(size.key)"
          >
            {{ size.label }}
          </button>
        </template>

        <!-- Fullscreen button (right-aligned) -->
        <button
          v-if="showFullscreenButton"
          type="button"
          class="ml-auto p-1.5 rounded hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200"
          title="Fullscreen editor"
          @click="isFullscreen = true"
        >
          <UiIcon name="fullscreen" class="w-4 h-4" />
        </button>
      </div>

      <!-- Editor content -->
      <EditorContent :editor="editor" class="bg-white dark:bg-gray-800" />
    </div>
    <p v-if="error" class="mt-1 text-sm text-red-600 dark:text-red-400">{{ error }}</p>

    <MediaPicker
      :open="showImagePicker"
      @select="handleImageSelect"
      @close="showImagePicker = false"
    />

    <EditorLinkPickerModal
      :open="showLinkPicker"
      :initial-url="linkPickerInitialUrl"
      :initial-text="linkPickerInitialText"
      @confirm="handleLinkConfirm"
      @close="showLinkPicker = false"
    />

    <!-- Fullscreen overlay -->
    <EditorFullscreenEditor
      v-if="isFullscreen"
      :model-value="modelValue"
      :placeholder="placeholder"
      @update:model-value="$emit('update:modelValue', $event)"
      @close="isFullscreen = false"
    />
  </div>
</template>

<style>
.ProseMirror p.is-editor-empty:first-child::before {
  content: attr(data-placeholder);
  float: left;
  color: #9ca3af;
  pointer-events: none;
  height: 0;
}

.ProseMirror:focus {
  outline: none;
}

.dark .ProseMirror {
  color: #e5e7eb;
}

.ProseMirror img[data-size]          { display: block; margin-left: auto; margin-right: auto; height: auto; }
.ProseMirror img[data-size="small"]  { max-width: 25%; }
.ProseMirror img[data-size="medium"] { max-width: 50%; }
.ProseMirror img[data-size="large"]  { max-width: 75%; }
.ProseMirror img[data-size="full"]   { max-width: 100%; }
</style>
