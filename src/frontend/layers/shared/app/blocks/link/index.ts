import { defineBlock } from '../types'

export default defineBlock({
  type: 'link',
  label: 'Link',
  order: 60,
  create: () => ({ type: 'link', url: '' })
})
