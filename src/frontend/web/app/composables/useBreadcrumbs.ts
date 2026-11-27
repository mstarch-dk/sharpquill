export interface BreadcrumbItem {
  label: string
  href?: string
}

export const useBreadcrumbs = () => {
  const breadcrumbs = useState<BreadcrumbItem[]>('breadcrumbs', () => [])

  const set = (items: BreadcrumbItem[]) => {
    breadcrumbs.value = items
  }

  return { breadcrumbs, set }
}
