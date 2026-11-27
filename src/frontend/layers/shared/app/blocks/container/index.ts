import { defineBlock } from '../types'

export default defineBlock({
  type: 'container',
  label: 'Container',
  order: 100,
  nestsSections: true,
  create: () => ({ type: 'container', sections: [{ type: 'full', blocks: [{ type: 'text', content: '' }] }] })
})
