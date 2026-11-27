import { defineBlock } from '../types'

export default defineBlock({
  type: 'cta',
  label: 'CTA',
  order: 50,
  create: () => ({ type: 'cta', title: '', description: '', buttonText: '', buttonUrl: '' })
})
