<script setup lang="ts">
const route = useRoute()
const router = useRouter()
const { loginWithToken, isAuthenticated, initAuth } = useWebsiteAuth()
const { t } = useI18n()

const token = route.query.token as string | undefined
const isLoggingIn = ref(false)
const loginError = ref('')
const loginSuccess = ref(false)

useSeoMeta({
  title: () => t('common.logIn'),
  description: () => t('login.description')
})

onMounted(async () => {
  await initAuth()

  if (token) {
    isLoggingIn.value = true
    const result = await loginWithToken(token)
    isLoggingIn.value = false

    if (result.success) {
      loginSuccess.value = true
      setTimeout(() => {
        router.push('/')
      }, 1500)
    } else {
      loginError.value = result.error || t('login.failed')
    }
  }
})
</script>

<template>
  <div class="py-16">
    <div class="mx-auto max-w-md px-4 sm:px-6 lg:px-8">
      <div class="bg-white dark:bg-gray-800 shadow dark:shadow-gray-900/30 rounded-lg p-8">
        <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-6 text-center">{{ t('common.logIn') }}</h1>

        <!-- Auto-login with token -->
        <div v-if="token">
          <div v-if="isLoggingIn" class="text-center">
            <UiLoadingSpinner class="h-8 w-8 mx-auto text-primary-600 mb-4" />
            <p class="text-gray-600 dark:text-gray-400">{{ t('login.loggingIn') }}</p>
          </div>

          <div v-else-if="loginSuccess" class="text-center">
            <UiIcon name="check" class="w-12 h-12 mx-auto text-green-500 mb-4" />
            <p class="text-green-600 dark:text-green-400 font-medium">{{ t('login.success') }}</p>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">{{ t('login.redirecting') }}</p>
          </div>

          <div v-else-if="loginError" class="text-center">
            <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg mb-4">
              {{ loginError }}
            </div>
            <i18n-t keypath="login.linkExpired" tag="p" scope="global" class="text-sm text-gray-500 dark:text-gray-400">
              <template #signUp>
                <NuxtLink to="/signup" class="text-primary-600 hover:text-primary-700">{{ t('common.signUp') }}</NuxtLink>
              </template>
            </i18n-t>
          </div>
        </div>

        <!-- No token - info page -->
        <div v-else>
          <div v-if="isAuthenticated" class="text-center">
            <p class="text-gray-600 dark:text-gray-400 mb-4">{{ t('login.alreadyLoggedIn') }}</p>
            <NuxtLink to="/" class="text-primary-600 hover:text-primary-700 font-medium">
              {{ t('common.goHome') }}
            </NuxtLink>
          </div>
          <div v-else>
            <p class="text-gray-600 dark:text-gray-400 mb-6">
              {{ t('login.info') }}
            </p>
            <NuxtLink
              to="/signup"
              class="block w-full text-center bg-primary-600 text-white px-4 py-2 rounded-md hover:bg-primary-700 transition-colors"
            >
              {{ t('common.signUp') }}
            </NuxtLink>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
