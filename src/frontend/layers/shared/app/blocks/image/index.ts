import { defineBlock } from '../types'

export default defineBlock({
  type: 'image',
  label: 'Image',
  order: 20,
  create: () => ({ type: 'image' })
})
