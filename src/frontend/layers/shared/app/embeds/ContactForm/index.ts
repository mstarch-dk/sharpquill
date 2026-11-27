import { defineEmbed } from '../types'

// Posts to the public contact endpoint (/api/v1/contact), which answers 503 until the contact email is configured
export default defineEmbed({
  name: 'ContactForm',
  label: 'Contact Form',
  component: () => import('./ContactForm.vue')
})
