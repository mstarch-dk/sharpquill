import { describe, expect, it } from 'vitest'
import { blockDefinitions, blockFolders, getBlockDefinition, getBlockRenderer } from '../../app/blocks/registry'
import { getBlockEditor } from '../../app/blocks/editors'
import { embedDefinitions, embedFolders, getEmbedComponent, getEmbedDefinition } from '../../app/embeds/registry'

// The registries find blocks and embeds by folder; the backend's LayoutBlockTypes must list the same names
// (checked from the backend side by LayoutBlockRegistryTests)

describe('block registry', () => {
  it('finds the built-in blocks', () => {
    expect(blockDefinitions.map(d => d.type).sort()).toEqual(
      ['cards', 'code', 'component', 'container', 'cta', 'image', 'link', 'steps', 'text', 'tweet'])
  })

  it.each(blockFolders)('names the $folder block after its folder', ({ folder, type }) => {
    expect(type).toBe(folder)
  })

  it.each(blockDefinitions)('has a renderer and an editor for $type', definition => {
    expect(getBlockRenderer(definition.type)).toBeDefined()
    expect(getBlockEditor(definition.type)).toBeDefined()
  })

  it.each(blockDefinitions)('creates a $type block of its own type', definition => {
    expect(definition.create().type).toBe(definition.type)
  })

  it('lists the blocks in picker order', () => {
    const orders = blockDefinitions.map(d => d.order)
    expect(orders).toEqual([...orders].sort((a, b) => a - b))
  })

  it.each(['unknown', '', '__proto__', 'constructor', 'toString'])('knows nothing about %j', type => {
    expect(getBlockDefinition(type)).toBeUndefined()
    expect(getBlockRenderer(type)).toBeUndefined()
    expect(getBlockEditor(type)).toBeUndefined()
  })
})

describe('embed registry', () => {
  it('finds the built-in embeds', () => {
    expect(embedDefinitions.map(d => d.name)).toEqual(['ContactForm'])
  })

  it.each(embedFolders)('names the $folder embed after its folder', ({ folder, name }) => {
    expect(name).toBe(folder)
  })

  it('resolves a known embed to one component instance', () => {
    const component = getEmbedComponent('ContactForm')
    expect(component).toBeDefined()
    expect(getEmbedComponent('ContactForm')).toBe(component)
  })

  it.each([undefined, '', 'Unknown', '__proto__', 'contactform'])('knows nothing about %j', name => {
    expect(getEmbedDefinition(name)).toBeUndefined()
    expect(getEmbedComponent(name)).toBeUndefined()
  })

  it('makes the component block default to the first embed', () => {
    expect(getBlockDefinition('component')!.create().componentName).toBe('ContactForm')
  })
})
