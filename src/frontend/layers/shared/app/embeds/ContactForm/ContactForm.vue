<script setup lang="ts">
const config = useRuntimeConfig()
const { t } = useLayerText()

const form = reactive({
  name: '',
  email: '',
  subject: '',
  message: ''
})

const submitting = ref(false)
const success = ref(false)
const errorMessage = ref<string | null>(null)

async function submit() {
  submitting.value = true
  errorMessage.value = null

  try {
    await $fetch(`${config.public.apiBase}/api/v1/contact`, {
      method: 'POST',
      body: form
    })
    success.value = true
  } catch (err) {
    const msg = (err as { data?: { error?: { message?: string } } })?.data?.error?.message
    errorMessage.value = msg || t('contact.error')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div>
    <div v-if="success" class="rounded-lg bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 p-6 text-green-800 dark:text-green-200">
      <p class="font-semibold">{{ t('contact.thanksTitle') }}</p>
      <p class="mt-1 text-sm">{{ t('contact.thanksText') }}</p>
    </div>

    <form v-else @submit.prevent="submit" class="space-y-6">
      <div v-if="errorMessage" class="rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 p-4 text-red-700 dark:text-red-300 text-sm">
        {{ errorMessage }}
      </div>

      <div>
        <label for="name" class="block text-sm font-medium mb-1">{{ t('contact.name') }} <span class="text-red-500">*</span></label>
        <input
          id="name"
          v-model="form.name"
          type="text"
          required
          maxlength="100"
          class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
        />
      </div>

      <div>
        <label for="email" class="block text-sm font-medium mb-1">{{ t('contact.email') }} <span class="text-red-500">*</span></label>
        <input
          id="email"
          v-model="form.email"
          type="email"
          required
          maxlength="200"
          class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
        />
      </div>

      <div>
        <label for="subject" class="block text-sm font-medium mb-1">{{ t('contact.subject') }}</label>
        <input
          id="subject"
          v-model="form.subject"
          type="text"
          maxlength="200"
          class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
        />
      </div>

      <div>
        <label for="message" class="block text-sm font-medium mb-1">{{ t('contact.message') }} <span class="text-red-500">*</span></label>
        <textarea
          id="message"
          v-model="form.message"
          required
          rows="6"
          maxlength="5000"
          class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 resize-y"
        />
      </div>

      <button
        type="submit"
        :disabled="submitting"
        class="inline-flex items-center gap-2 rounded-lg bg-primary-600 hover:bg-primary-700 disabled:opacity-60 disabled:cursor-not-allowed text-white px-6 py-2.5 text-sm font-medium transition-colors"
      >
        <span v-if="submitting">{{ t('common.sending') }}</span>
        <span v-else>{{ t('contact.send') }}</span>
      </button>
    </form>
  </div>
</template>
