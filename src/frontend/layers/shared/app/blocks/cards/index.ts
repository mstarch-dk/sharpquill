import { defineBlock } from '../types'

export default defineBlock({
  type: 'cards',
  label: 'Cards',
  order: 30,
  create: () => ({ type: 'cards', heading: '', items: [{ icon: '', title: '', description: '' }] })
})
