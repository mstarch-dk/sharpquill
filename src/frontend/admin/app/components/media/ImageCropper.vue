<script setup lang="ts">
import { Cropper } from 'vue-advanced-cropper'
import 'vue-advanced-cropper/dist/style.css'

interface Props {
  imageFile: File
  aspectRatio?: number
  maxWidth?: number
  maxHeight?: number
}

const props = withDefaults(defineProps<Props>(), {
  maxWidth: 1920,
  maxHeight: 1080
})

const emit = defineEmits<{
  crop: [file: File]
  cancel: []
}>()

const cropperRef = ref()
const imageUrl = ref('')
const selectedAspect = ref<number | undefined>(props.aspectRatio)

const aspectPresets = [
  { label: 'Free', value: undefined },
  { label: '16:9', value: 16 / 9 },
  { label: '4:3', value: 4 / 3 },
  { label: '1:1', value: 1 }
]

onMounted(() => {
  imageUrl.value = URL.createObjectURL(props.imageFile)
})

onBeforeUnmount(() => {
  if (imageUrl.value) {
    URL.revokeObjectURL(imageUrl.value)
  }
})

function handleCrop() {
  const { canvas } = cropperRef.value.getResult()
  if (!canvas) return

  // Resize if needed
  let targetCanvas = canvas
  if (canvas.width > props.maxWidth || canvas.height > props.maxHeight) {
    const scale = Math.min(props.maxWidth / canvas.width, props.maxHeight / canvas.height)
    const resized = document.createElement('canvas')
    resized.width = Math.round(canvas.width * scale)
    resized.height = Math.round(canvas.height * scale)
    const ctx = resized.getContext('2d')!
    ctx.drawImage(canvas, 0, 0, resized.width, resized.height)
    targetCanvas = resized
  }

  targetCanvas.toBlob((blob: Blob | null) => {
    if (!blob) return
    const ext = props.imageFile.name.split('.').pop() || 'jpg'
    const filename = props.imageFile.name.replace(/\.[^.]+$/, '') + `-cropped.${ext}`
    const file = new File([blob], filename, { type: blob.type || 'image/jpeg' })
    emit('crop', file)
  }, props.imageFile.type || 'image/jpeg', 0.9)
}

function selectAspect(value: number | undefined) {
  selectedAspect.value = value
}
</script>

<template>
  <div class="space-y-3">
    <div class="flex items-center gap-2">
      <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Aspect ratio:</span>
      <button
        v-for="preset in aspectPresets"
        :key="preset.label"
        type="button"
        class="px-2 py-1 text-xs rounded-md transition-colors"
        :class="selectedAspect === preset.value
          ? 'bg-primary-100 text-primary-800 ring-1 ring-primary-500'
          : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'"
        @click="selectAspect(preset.value)"
      >
        {{ preset.label }}
      </button>
    </div>

    <div class="max-h-[350px] overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
      <Cropper
        ref="cropperRef"
        :src="imageUrl"
        :stencil-props="{ aspectRatio: selectedAspect }"
        class="max-h-[350px]"
      />
    </div>

    <div class="flex justify-end gap-2">
      <button
        type="button"
        class="px-3 py-1.5 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700"
        @click="$emit('cancel')"
      >
        Skip Crop
      </button>
      <button
        type="button"
        class="px-3 py-1.5 text-sm font-medium text-white bg-primary-600 rounded-md hover:bg-primary-700"
        @click="handleCrop"
      >
        Apply & Upload
      </button>
    </div>
  </div>
</template>
