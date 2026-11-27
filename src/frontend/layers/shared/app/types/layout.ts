// Article/page layouts (docs/frontend-uis.md, "Layouts and blocks"). The block `type` and the embed `componentName`
// are open strings: the registries in blocks/ and embeds/ define which values exist, and the API rejects others.

export type SectionType = 'full' | 'two-column' | 'three-column'

// --- Admin API input/output: raw Markdown and media ids, as stored ---

export interface BlockItemDto {
  icon?: string
  number?: string
  title?: string
  description?: string
}

export interface BlockInputDto {
  type: string
  content?: string
  mediaId?: number
  alt?: string
  caption?: string
  sections?: SectionInputDto[]
  url?: string
  title?: string
  description?: string
  imageUrl?: string
  siteName?: string
  language?: string
  tweetDate?: string
  heading?: string
  buttonText?: string
  buttonUrl?: string
  items?: BlockItemDto[]
  componentName?: string
}

export interface SectionInputDto {
  type: SectionType
  blocks: BlockInputDto[]
  style?: string
}

export interface LayoutInputDto {
  sections: SectionInputDto[]
}

// --- Rendered by the API (public API and admin preview): sanitised HTML and resolved media URLs ---

export interface RenderedLayoutDto {
  sections: RenderedSectionDto[]
}

export interface RenderedSectionDto {
  type: SectionType
  blocks: RenderedBlockDto[]
  style?: string
}

export interface RenderedBlockItemDto {
  icon?: string
  number?: string
  title?: string
  description?: string
  descriptionHtml?: string
  titleHtml?: string
}

export interface RenderedBlockDto {
  type: string
  html?: string
  imageUrl?: string
  alt?: string
  caption?: string
  sections?: RenderedSectionDto[]
  url?: string
  title?: string
  description?: string
  siteName?: string
  language?: string
  tweetDate?: string
  heading?: string
  buttonText?: string
  buttonUrl?: string
  items?: RenderedBlockItemDto[]
  descriptionHtml?: string
  headingHtml?: string
  titleHtml?: string
  captionHtml?: string
  componentName?: string
}
