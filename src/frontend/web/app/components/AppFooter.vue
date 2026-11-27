<script setup lang="ts">
import { featureIcons } from '#layer/utils/featureIcons'

const { data: navResponse } = await useNavigation().getNavigationBySlug('footer-links')
const footerItems = computed(() => navResponse.value?.data?.items || [])
const { settings, footerText } = useSiteSettings()
const { t } = useI18n()

const email = computed(() => settings.value.publicEmail.trim())
const phone = computed(() => settings.value.publicPhone.trim())
const companyInfo = computed(() => settings.value.companyInfo.trim())
const hasContact = computed(() => !!(email.value || phone.value || companyInfo.value))

// tel: links take digits and a leading + only
const phoneHref = computed(() => `tel:${phone.value.replace(/[^0-9+]/g, '')}`)

const socialLinks = computed(() => [
  { label: 'LinkedIn', url: settings.value.socialLinkedIn },
  { label: 'X', url: settings.value.socialTwitter },
  { label: 'Facebook', url: settings.value.socialFacebook },
  { label: 'Instagram', url: settings.value.socialInstagram }
].filter(link => link.url.trim()))

const envelopePaths = featureIcons.envelope!.paths
const phonePaths = featureIcons['device-phone-mobile']!.paths
</script>

<template>
  <footer class="bg-gray-800 dark:bg-gray-900 text-white">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-12">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-8">
        <div>
          <h3 class="text-lg font-semibold mb-4">{{ t('footer.quickLinks') }}</h3>
          <ul class="space-y-2">
            <li v-for="item in footerItems" :key="item.id">
              <NuxtLink
                :to="safeHref(item.url)"
                :target="item.openInNewTab ? '_blank' : undefined"
                class="text-gray-400 dark:text-gray-500 hover:text-white transition-colors"
              >
                {{ item.label }}
              </NuxtLink>
            </li>
          </ul>
        </div>
        <div>
          <h3 class="text-lg font-semibold mb-4">{{ t('footer.contact') }}</h3>
          <ul v-if="hasContact" class="space-y-2 text-gray-400 dark:text-gray-500">
            <li v-if="email">
              <a :href="`mailto:${email}`" class="inline-flex items-center gap-2 hover:text-white transition-colors">
                <svg class="h-5 w-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true">
                  <path v-for="(d, i) in envelopePaths" :key="i" :d="d" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
                {{ email }}
              </a>
            </li>
            <li v-if="phone">
              <a :href="phoneHref" class="inline-flex items-center gap-2 hover:text-white transition-colors">
                <svg class="h-5 w-5 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" aria-hidden="true">
                  <path v-for="(d, i) in phonePaths" :key="i" :d="d" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
                {{ phone }}
              </a>
            </li>
            <li v-if="companyInfo">{{ companyInfo }}</li>
          </ul>
          <p v-else class="text-gray-400 dark:text-gray-500">
            {{ t('footer.followUs') }}
          </p>
        </div>
        <div v-if="socialLinks.length">
          <h3 class="text-lg font-semibold mb-4">{{ t('footer.follow') }}</h3>
          <ul class="space-y-2">
            <li v-for="link in socialLinks" :key="link.label">
              <a
                :href="safeHref(link.url)"
                target="_blank"
                rel="noopener noreferrer"
                class="text-gray-400 dark:text-gray-500 hover:text-white transition-colors"
              >
                {{ link.label }}
              </a>
            </li>
          </ul>
        </div>
      </div>
      <div class="mt-8 pt-8 border-t border-gray-700 dark:border-gray-600 text-center text-gray-400 dark:text-gray-500">
        <p>{{ footerText }}</p>
      </div>
    </div>
  </footer>
</template>
