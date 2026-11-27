<script setup lang="ts">
definePageMeta({
  layout: 'auth',
  middleware: 'guest'
})

useHead({
  title: 'Login'
})

const { login, certLogin } = useAuth()
const router = useRouter()

const email = ref('')
const password = ref('')
const error = ref('')
const isLoading = ref(false)
const certCheckDone = ref(false)

// Attempt certificate login client-side before showing the form
onMounted(async () => {
  const result = await certLogin()
  if (result.success) {
    router.push('/articles')
    return
  }
  certCheckDone.value = true
})

async function handleSubmit() {
  error.value = ''
  isLoading.value = true

  try {
    const result = await login({ email: email.value, password: password.value })

    if (result.success) {
      router.push('/articles')
    } else {
      error.value = result.error || 'Login failed'
    }
  } catch {
    error.value = 'An unexpected error occurred'
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="w-full max-w-md px-4">
    <div v-if="!certCheckDone" class="bg-white dark:bg-gray-800 rounded-lg shadow-lg dark:shadow-gray-900/30 p-8 text-center">
      <h1 class="flex flex-col items-center gap-2">
        <BrandLogo class="h-12" />
        <span class="text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Admin</span>
      </h1>
      <p class="text-gray-600 dark:text-gray-400 mt-4">Checking certificate...</p>
    </div>

    <div v-else class="bg-white dark:bg-gray-800 rounded-lg shadow-lg dark:shadow-gray-900/30 p-8">
      <div class="text-center mb-8">
        <h1 class="flex flex-col items-center gap-2">
          <BrandLogo class="h-12" />
          <span class="text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Admin</span>
        </h1>
        <p class="text-gray-600 dark:text-gray-400 mt-2">Sign in with your credentials.</p>
      </div>

      <form @submit.prevent="handleSubmit" class="space-y-6">
        <div v-if="error" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 px-4 py-3 rounded-lg text-sm">
          {{ error }}
        </div>

        <div>
          <label for="email" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Email
          </label>
          <input
            id="email"
            v-model="email"
            type="email"
            required
            autocomplete="email"
            class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg shadow-sm dark:shadow-gray-900/30 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 dark:bg-gray-800 dark:text-gray-100"
            placeholder="you@example.com"
          />
        </div>

        <div>
          <label for="password" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Password
          </label>
          <input
            id="password"
            v-model="password"
            type="password"
            required
            autocomplete="current-password"
            class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg shadow-sm dark:shadow-gray-900/30 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 dark:bg-gray-800 dark:text-gray-100"
            placeholder="Enter your password"
          />
        </div>

        <button
          type="submit"
          :disabled="isLoading"
          class="w-full py-2.5 px-4 bg-primary-600 text-white font-medium rounded-lg hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          {{ isLoading ? 'Signing in...' : 'Sign in' }}
        </button>
      </form>
    </div>
  </div>
</template>
