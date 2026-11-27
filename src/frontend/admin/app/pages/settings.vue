<script setup lang="ts">
import { themeList } from '../../../themes'
import { fontOptions, getFontStackById, defaultFontId } from '../../../themes/fonts'
import type { SiteSettingsUpdateDto } from '~/composables/useAdminSettings'

definePageMeta({ middleware: 'auth' })
useHead({ title: 'Settings' })

const { getSettings, updateSettings } = useAdminSettings()

const loading = ref(true)
const saving = ref(false)
const error = ref('')
const success = ref('')

interface SiteSettingsForm {
  siteName: string
  siteDescription: string
  siteUrl: string
  contactEmail: string
  socialTwitter: string
  socialFacebook: string
  socialInstagram: string
  socialLinkedIn: string
  metaKeywords: string
  theme: string
  globalFont: string
  language: string
  footerText: string
  darkModeAdmin: boolean
  darkModeWeb: boolean
  loginEnabled: boolean
  publicEmail: string
  publicPhone: string
  companyInfo: string
}

const form = reactive<SiteSettingsForm>({
  siteName: '',
  siteDescription: '',
  siteUrl: '',
  contactEmail: '',
  socialTwitter: '',
  socialFacebook: '',
  socialInstagram: '',
  socialLinkedIn: '',
  metaKeywords: '',
  theme: 'ocean-blue',
  globalFont: defaultFontId,
  language: '',
  footerText: '',
  darkModeAdmin: false,
  darkModeWeb: false,
  loginEnabled: true,
  publicEmail: '',
  publicPhone: '',
  companyInfo: ''
})

onMounted(async () => {
  try {
    const response = await getSettings()
    const s = response.data
    form.siteName = s.siteName || ''
    form.siteDescription = s.siteDescription || ''
    form.siteUrl = s.siteUrl || ''
    form.contactEmail = s.contactEmail || ''
    form.socialTwitter = s.socialTwitter || ''
    form.socialFacebook = s.socialFacebook || ''
    form.socialInstagram = s.socialInstagram || ''
    form.socialLinkedIn = s.socialLinkedIn || ''
    form.metaKeywords = s.metaKeywords || ''
    form.theme = s.theme || 'ocean-blue'
    form.globalFont = s.globalFont || defaultFontId
    form.language = s.language || ''
    form.footerText = s.footerText || ''
    form.darkModeAdmin = s.darkModeAdmin || false
    form.darkModeWeb = s.darkModeWeb || false
    form.loginEnabled = s.loginEnabled !== false
    form.publicEmail = s.publicEmail || ''
    form.publicPhone = s.publicPhone || ''
    form.companyInfo = s.companyInfo || ''
  } catch {
    error.value = 'Failed to load settings'
  } finally {
    loading.value = false
  }
})

async function handleSave() {
  saving.value = true
  error.value = ''
  success.value = ''
  try {
    const payload: SiteSettingsUpdateDto = { ...form }
    await updateSettings(payload)
    // Apply admin dark mode immediately
    if (import.meta.client) {
      const selectedFontStack = getFontStackById(form.globalFont)
      document.documentElement.style.setProperty('--font-heading', selectedFontStack)
      document.documentElement.style.setProperty('--font-body', selectedFontStack)

      if (form.darkModeAdmin) {
        document.documentElement.classList.add('dark')
      } else {
        document.documentElement.classList.remove('dark')
      }
    }
    success.value = 'Settings saved successfully'
    setTimeout(() => success.value = '', 3000)
  } catch {
    error.value = 'Failed to save settings'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div>
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Settings</h1>
      <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">Manage your site settings and appearance</p>
    </div>

    <div v-if="loading" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600" />
    </div>

    <form v-else @submit.prevent="handleSave" class="space-y-8">
      <!-- Success / Error messages -->
      <div v-if="success" class="rounded-md bg-green-50 dark:bg-green-900/20 p-4">
        <p class="text-sm text-green-800 dark:text-green-300">{{ success }}</p>
      </div>
      <div v-if="error" class="rounded-md bg-red-50 dark:bg-red-900/20 p-4">
        <p class="text-sm text-red-800 dark:text-red-300">{{ error }}</p>
      </div>

      <!-- Dark Mode -->
      <div class="bg-white dark:bg-gray-800 shadow rounded-lg p-6">
        <h2 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-4">Dark Mode</h2>
        <p class="text-sm text-gray-500 dark:text-gray-400 mb-4">Enable dark mode for the admin panel and/or public website</p>

        <div class="space-y-4">
          <label class="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              v-model="form.darkModeAdmin"
              class="h-4 w-4 rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500"
            />
            <div>
              <span class="text-sm font-medium text-gray-900 dark:text-gray-100">Admin Panel</span>
              <p class="text-xs text-gray-500 dark:text-gray-400">Apply dark mode to this admin interface</p>
            </div>
          </label>

          <label class="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              v-model="form.darkModeWeb"
              class="h-4 w-4 rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500"
            />
            <div>
              <span class="text-sm font-medium text-gray-900 dark:text-gray-100">Public Website</span>
              <p class="text-xs text-gray-500 dark:text-gray-400">Apply dark mode to the public-facing website</p>
            </div>
          </label>
        </div>
      </div>

      <!-- Typography -->
      <div class="bg-white dark:bg-gray-800 shadow rounded-lg p-6">
        <h2 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-4">Typography</h2>
        <p class="text-sm text-gray-500 dark:text-gray-400 mb-4">
          Pick one global font used across both admin and public website rendering.
        </p>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <label
            v-for="font in fontOptions"
            :key="font.id"
            class="relative rounded-lg border-2 p-4 cursor-pointer transition-colors"
            :class="form.globalFont === font.id ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20' : 'border-gray-200 dark:border-gray-600 hover:border-gray-300 dark:hover:border-gray-500'"
          >
            <input
              type="radio"
              :value="font.id"
              v-model="form.globalFont"
              class="sr-only"
            />

            <p
              class="text-lg text-gray-900 dark:text-gray-100"
              :style="{ fontFamily: getFontStackById(font.id) }"
            >
              {{ font.name }}
            </p>
            <p class="mt-1 text-xs text-gray-600 dark:text-gray-400">{{ font.rationale }}</p>
            <p
              class="mt-3 text-sm text-gray-700 dark:text-gray-300"
              :style="{ fontFamily: getFontStackById(font.id) }"
            >
              Lorem ipsum dolor sit amet, consectetur adipiscing elit.
            </p>

            <div
              v-if="form.globalFont === font.id"
              class="absolute top-2 right-2"
            >
              <UiIcon name="check-circle" class="w-5 h-5 text-primary-600" />
            </div>
          </label>
        </div>
      </div>

      <!-- Theme Selection -->
      <div class="bg-white dark:bg-gray-800 shadow rounded-lg p-6">
        <h2 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-4">Theme</h2>
        <p class="text-sm text-gray-500 dark:text-gray-400 mb-4">Choose the visual theme for your public website</p>

        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <label
            v-for="theme in themeList"
            :key="theme.slug"
            class="relative flex flex-col rounded-lg border-2 p-4 cursor-pointer transition-colors"
            :class="form.theme === theme.slug ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20' : 'border-gray-200 dark:border-gray-600 hover:border-gray-300 dark:hover:border-gray-500'"
          >
            <input
              type="radio"
              :value="theme.slug"
              v-model="form.theme"
              class="sr-only"
            />
            <!-- Color swatches -->
            <div class="flex gap-1 mb-3">
              <div
                class="w-6 h-6 rounded-full"
                :style="{ backgroundColor: theme.colors.primary[500] }"
              />
              <div
                class="w-6 h-6 rounded-full"
                :style="{ backgroundColor: theme.colors.primary[700] }"
              />
              <div
                class="w-6 h-6 rounded-full"
                :style="{ backgroundColor: theme.colors.primary[300] }"
              />
              <div
                class="w-6 h-6 rounded-full border border-gray-200 dark:border-gray-600"
                :style="{ backgroundColor: theme.colors.background }"
              />
            </div>
            <span class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ theme.name }}</span>
            <span class="text-xs text-gray-500 dark:text-gray-400 mt-1">{{ theme.description }}</span>
            <!-- Checkmark for selected -->
            <div
              v-if="form.theme === theme.slug"
              class="absolute top-2 right-2"
            >
              <UiIcon name="check-circle" class="w-5 h-5 text-primary-600" />
            </div>
          </label>
        </div>
      </div>

      <!-- General Settings -->
      <div class="bg-white dark:bg-gray-800 shadow rounded-lg p-6">
        <h2 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-4">General</h2>

        <div class="grid grid-cols-1 gap-6">
          <UiInput
            v-model="form.siteName"
            label="Site Name"
            placeholder="My Website"
          />
          <UiTextarea
            v-model="form.siteDescription"
            label="Site Description"
            placeholder="A brief description of your website"
            :rows="2"
          />
          <UiInput
            v-model="form.siteUrl"
            label="Site URL"
            placeholder="https://example.com"
          />
          <UiInput
            v-model="form.contactEmail"
            label="Contact Email"
            placeholder="contact@example.com"
          />
          <UiInput
            v-model="form.language"
            label="Website language"
            placeholder="da"
          />
          <p class="-mt-4 text-xs text-gray-500 dark:text-gray-400">
            Language tag for the public website, e.g. da, en or en-GB. Empty means da. Sets the &lt;html lang&gt;
            attribute and the language of the website's built-in texts (Danish and English ship; others fall back to
            Danish).
          </p>
          <UiInput
            v-model="form.footerText"
            label="Footer text"
            placeholder="© {year} Your Name"
          />
          <p class="-mt-4 text-xs text-gray-500 dark:text-gray-400">
            Shown at the bottom of every page. {year} is replaced by the current year. Empty shows
            "© year" followed by the site name.
          </p>

          <label class="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              v-model="form.loginEnabled"
              class="h-4 w-4 rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500"
            />
            <div>
              <span class="text-sm font-medium text-gray-900 dark:text-gray-100">Vis login-mulighed</span>
              <p class="text-xs text-gray-500 dark:text-gray-400">Show login option on public website</p>
            </div>
          </label>
        </div>
      </div>

      <!-- Footer contact details -->
      <div class="bg-white dark:bg-gray-800 shadow rounded-lg p-6">
        <h2 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-1">Footer Contact</h2>
        <p class="text-sm text-gray-500 dark:text-gray-400 mb-4">
          Shown publicly in the website footer. Leave empty to hide. The contact email above stays private: it only
          receives contact form messages.
        </p>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
          <UiInput
            v-model="form.publicEmail"
            label="Public email"
            placeholder="hello@example.com"
          />
          <UiInput
            v-model="form.publicPhone"
            label="Phone"
            placeholder="+45 12 34 56 78"
          />
          <UiInput
            v-model="form.companyInfo"
            label="Company info"
            placeholder="CVR 12345678"
            class="sm:col-span-2"
          />
        </div>
      </div>

      <!-- Social Links -->
      <div class="bg-white dark:bg-gray-800 shadow rounded-lg p-6">
        <h2 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-4">Social Links</h2>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
          <UiInput
            v-model="form.socialTwitter"
            label="Twitter / X"
            placeholder="https://twitter.com/yourhandle"
          />
          <UiInput
            v-model="form.socialFacebook"
            label="Facebook"
            placeholder="https://facebook.com/yourpage"
          />
          <UiInput
            v-model="form.socialInstagram"
            label="Instagram"
            placeholder="https://instagram.com/yourhandle"
          />
          <UiInput
            v-model="form.socialLinkedIn"
            label="LinkedIn"
            placeholder="https://linkedin.com/in/yourprofile"
          />
        </div>
      </div>

      <!-- SEO -->
      <div class="bg-white dark:bg-gray-800 shadow rounded-lg p-6">
        <h2 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-4">SEO</h2>

        <div class="grid grid-cols-1 gap-6">
          <UiInput
            v-model="form.metaKeywords"
            label="Meta Keywords"
            placeholder="keyword1, keyword2, keyword3"
          />
        </div>
      </div>

      <!-- Save button -->
      <div class="flex justify-end">
        <UiButton type="submit" :loading="saving">
          Save Settings
        </UiButton>
      </div>
    </form>
  </div>
</template>
