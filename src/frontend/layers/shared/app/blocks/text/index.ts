import { defineBlock } from '../types'

export default defineBlock({
  type: 'text',
  label: 'Text',
  order: 10,
  create: () => ({ type: 'text', content: '' })
})
