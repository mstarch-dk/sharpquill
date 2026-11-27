<script setup lang="ts">
const { data: categoriesResponse } = await useCategories().getCategories()
const categories = computed(() => categoriesResponse.value?.data || [])

const { data: navResponse } = await useNavigation().getNavigationBySlug('main-menu')
const navItems = computed(() => navResponse.value?.data?.items || [])

const mobileMenuOpen = ref(false)
const loginEnabled = ref(true)

const { isAuthenticated, initAuth, logout: doLogout, user, fetchProfile } = useWebsiteAuth()
const { isDark, toggle: toggleDark } = useDarkMode()
const { fetchSiteSettings } = useTheme()
const { t } = useI18n()
// Wordmark only for a configured site name: the translated fallback ("Forside") is not a brand
const { settings: siteSettings } = useSiteSettings()
const wordmark = computed(() => siteSettings.value.siteName.trim())

onMounted(async () => {
  await initAuth()
  if (isAuthenticated.value) {
    await fetchProfile()
  }
  try {
    const settings = await fetchSiteSettings()
    loginEnabled.value = settings.loginEnabled
  } catch {
    loginEnabled.value = true
  }
})

async function handleLogout() {
  await doLogout()
  navigateTo('/')
}
</script>

<template>
  <header class="bg-white dark:bg-gray-800 shadow-sm dark:shadow-gray-900/30">
    <nav class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
      <div class="flex h-16 items-center justify-between">
        <div class="flex items-center">
          <NuxtLink to="/" class="flex items-center gap-2 text-primary-600">
            <UiIcon name="book-open" class="w-8 h-8 shrink-0" />
            <span v-if="wordmark" class="text-lg font-semibold tracking-tight text-gray-900 dark:text-gray-100">{{ wordmark }}</span>
          </NuxtLink>
        </div>

        <!-- Desktop navigation -->
        <div class="hidden md:flex md:items-center md:space-x-6">
          <template v-for="item in navItems" :key="item.id">
            <!-- Categories dropdown -->
            <div v-if="item.url === '/categories' && categories.length" class="relative group">
              <button class="text-gray-600 dark:text-gray-400 hover:text-primary-600 transition-colors flex items-center gap-1">
                {{ item.label }}
                <UiIcon name="chevron-down" class="w-4 h-4" />
              </button>
              <div class="absolute left-0 mt-2 w-48 bg-white dark:bg-gray-800 rounded-md shadow-lg dark:shadow-gray-900/40 opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-50">
                <div class="py-1">
                  <NuxtLink
                    v-for="category in categories"
                    :key="category.id"
                    :to="`/category/${category.slug}`"
                    class="block px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
                  >
                    {{ category.name }}
                  </NuxtLink>
                </div>
              </div>
            </div>
            <!-- Regular nav item -->
            <NuxtLink
              v-else
              :to="safeHref(item.url)"
              :target="item.openInNewTab ? '_blank' : undefined"
              class="text-gray-600 dark:text-gray-400 hover:text-primary-600 transition-colors"
            >
              {{ item.label }}
            </NuxtLink>
          </template>

          <SearchBox />

          <!-- Dark mode toggle -->
          <ClientOnly>
            <button
              class="p-1.5 rounded-md text-gray-600 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 transition-colors"
              :aria-label="isDark ? t('header.switchToLight') : t('header.switchToDark')"
              @click="toggleDark"
            >
              <UiIcon :name="isDark ? 'sun' : 'moon'" class="w-5 h-5" />
            </button>
          </ClientOnly>

          <!-- Auth links (client-side only) -->
          <ClientOnly>
            <template v-if="isAuthenticated">
              <span class="text-sm text-gray-500 dark:text-gray-400">{{ user?.name }}</span>
              <button
                class="text-gray-600 dark:text-gray-400 hover:text-primary-600 transition-colors text-sm"
                @click="handleLogout"
              >
                {{ t('common.logOut') }}
              </button>
            </template>
            <template v-else>
              <NuxtLink v-if="loginEnabled" to="/login" class="text-gray-600 dark:text-gray-400 hover:text-primary-600 transition-colors text-sm">
                {{ t('common.logIn') }}
              </NuxtLink>
              <NuxtLink v-if="loginEnabled" to="/signup" class="bg-primary-600 text-white px-3 py-1.5 rounded-md text-sm hover:bg-primary-700 transition-colors">
                {{ t('common.signUp') }}
              </NuxtLink>
            </template>
          </ClientOnly>
        </div>

        <!-- Mobile menu button -->
        <button
          class="md:hidden p-2 rounded-md text-gray-600 dark:text-gray-400 hover:text-primary-600"
          @click="mobileMenuOpen = !mobileMenuOpen"
        >
          <UiIcon v-if="!mobileMenuOpen" name="bars" class="w-6 h-6" />
          <UiIcon v-else name="x-mark" class="w-6 h-6" />
        </button>
      </div>

      <!-- Mobile menu -->
      <div v-show="mobileMenuOpen" class="md:hidden pb-4">
        <div class="flex flex-col space-y-2">
          <SearchBox block class="px-2 pb-2" @submitted="mobileMenuOpen = false" />

          <template v-for="item in navItems" :key="item.id">
            <!-- Categories section in mobile -->
            <template v-if="item.url === '/categories' && categories.length">
              <div class="px-2 py-2 text-gray-400 dark:text-gray-500 text-sm uppercase tracking-wider">
                {{ item.label }}
              </div>
              <NuxtLink
                v-for="category in categories"
                :key="category.id"
                :to="`/category/${category.slug}`"
                class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:text-primary-600"
                @click="mobileMenuOpen = false"
              >
                {{ category.name }}
              </NuxtLink>
            </template>
            <!-- Regular mobile nav item -->
            <NuxtLink
              v-else
              :to="safeHref(item.url)"
              :target="item.openInNewTab ? '_blank' : undefined"
              class="px-2 py-2 text-gray-600 dark:text-gray-400 hover:text-primary-600"
              @click="mobileMenuOpen = false"
            >
              {{ item.label }}
            </NuxtLink>
          </template>

          <!-- Mobile dark mode toggle -->
          <ClientOnly>
            <button
              class="flex items-center gap-2 px-2 py-2 text-gray-600 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 transition-colors"
              :aria-label="isDark ? t('header.switchToLight') : t('header.switchToDark')"
              @click="toggleDark(); mobileMenuOpen = false"
            >
              <UiIcon :name="isDark ? 'sun' : 'moon'" class="w-5 h-5" />
              <span class="text-sm">{{ isDark ? t('header.lightMode') : t('header.darkMode') }}</span>
            </button>
          </ClientOnly>

          <!-- Mobile auth links -->
          <ClientOnly>
            <div class="border-t border-gray-200 dark:border-gray-700 pt-2 mt-2">
              <template v-if="isAuthenticated">
                <div class="px-2 py-2 text-sm text-gray-500 dark:text-gray-400">{{ user?.name }}</div>
                <button
                  class="px-2 py-2 text-gray-600 dark:text-gray-400 hover:text-primary-600 text-left w-full"
                  @click="handleLogout(); mobileMenuOpen = false"
                >
                  {{ t('common.logOut') }}
                </button>
              </template>
              <template v-else>
                <NuxtLink
                  v-if="loginEnabled"
                  to="/login"
                  class="px-2 py-2 text-gray-600 dark:text-gray-400 hover:text-primary-600 block"
                  @click="mobileMenuOpen = false"
                >
                  {{ t('common.logIn') }}
                </NuxtLink>
                <NuxtLink
                  v-if="loginEnabled"
                  to="/signup"
                  class="px-2 py-2 text-primary-600 hover:text-primary-700 font-medium block"
                  @click="mobileMenuOpen = false"
                >
                  {{ t('common.signUp') }}
                </NuxtLink>
              </template>
            </div>
          </ClientOnly>
        </div>
      </div>
    </nav>
  </header>
</template>
