<script setup lang="ts">
const { signup, isAuthenticated } = useWebsiteAuth()
const { t } = useI18n()

const name = ref('')
const email = ref('')
const isSubmitting = ref(false)
const error = ref('')
const success = ref(false)

useSeoMeta({
  title: () => t('common.signUp'),
  description: () => t('signup.description')
})

async function handleSubmit() {
  error.value = ''

  if (!name.value.trim() || !email.value.trim()) {
    error.value = t('signup.fillBoth')
    return
  }

  isSubmitting.value = true
  const result = await signup(name.value.trim(), email.value.trim())
  isSubmitting.value = false

  if (result.success) {
    success.value = true
  } else {
    error.value = result.error || t('signup.failed')
  }
}
</script>

<template>
  <div class="py-16">
    <div class="mx-auto max-w-md px-4 sm:px-6 lg:px-8">
      <div class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8">
        <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-6 text-center">{{ t('common.signUp') }}</h1>

        <div v-if="isAuthenticated" class="text-center">
          <p class="text-gray-600 dark:text-gray-400 mb-4">{{ t('login.alreadyLoggedIn') }}</p>
          <NuxtLink to="/" class="text-primary-600 hover:text-primary-700 font-medium">
            {{ t('common.goHome') }}
          </NuxtLink>
        </div>

        <div v-else-if="success" class="text-center">
          <UiIcon name="check" class="w-12 h-12 mx-auto text-green-500 mb-4" />
          <h2 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">{{ t('signup.receivedTitle') }}</h2>
          <p class="text-gray-600 dark:text-gray-400">
            {{ t('signup.receivedText') }}
          </p>
        </div>

        <form v-else @submit.prevent="handleSubmit" class="space-y-4">
          <p class="text-sm text-gray-600 dark:text-gray-400 mb-4">
            {{ t('signup.intro') }}
          </p>

          <div v-if="error" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg text-sm">
            {{ error }}
          </div>

          <div>
            <label for="name" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">{{ t('signup.name') }}</label>
            <input
              id="name"
              v-model="name"
              type="text"
              required
              maxlength="100"
              class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              :placeholder="t('signup.namePlaceholder')"
            />
          </div>

          <div>
            <label for="email" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">{{ t('signup.email') }}</label>
            <input
              id="email"
              v-model="email"
              type="email"
              required
              maxlength="255"
              class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              :placeholder="t('signup.emailPlaceholder')"
            />
          </div>

          <button
            type="submit"
            :disabled="isSubmitting"
            class="w-full bg-primary-600 text-white px-4 py-2 rounded-md hover:bg-primary-700 transition-colors disabled:opacity-50"
          >
            {{ isSubmitting ? t('signup.submitting') : t('common.signUp') }}
          </button>

          <i18n-t keypath="signup.haveAccount" tag="p" scope="global" class="text-center text-sm text-gray-500 dark:text-gray-400">
            <template #logIn>
              <NuxtLink to="/login" class="text-primary-600 hover:text-primary-700">{{ t('common.logIn') }}</NuxtLink>
            </template>
          </i18n-t>
        </form>
      </div>
    </div>
  </div>
</template>
