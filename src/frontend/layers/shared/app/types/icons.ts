// Shape of an icon in an app's icon set. The shared <UiIcon> renders it; each app keeps its own set in
// ~/utils/icons (the website uses Tailwind-style 2px strokes, the admin Heroicons outline), so only the type is shared.
export interface IconPath {
  d?: string
  cx?: number
  cy?: number
  r?: number
  fillRule?: 'evenodd' | 'nonzero'
  clipRule?: 'evenodd' | 'nonzero'
  transform?: string
}

export interface IconDef {
  paths: IconPath[]
  viewBox?: string
  style?: 'stroke' | 'fill'
  strokeWidth?: number
}
