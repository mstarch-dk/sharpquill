import type { H3Event } from 'h3'
import type { ApiResponse, ArticleListDto, CategoryDto, SeriesDetailDto, TagDto } from '~/types/api'
import { buildFeed, FEED_CONTENT_TYPES, feedPath } from '~/utils/feed'
import type { Feed, FeedFormat, FeedItem, FeedScope } from '~/utils/feed'
import { DEFAULT_LANGUAGE } from '~/utils/siteDefaults'

// Newest articles per feed. Feed readers poll; they only need what is new since the last visit.
const FEED_SIZE = 20

interface FeedSettings {
  siteName?: string | null
  siteDescription?: string | null
  language?: string | null
}

interface ScopeContent {
  title?: string
  description?: string
  items: FeedItem[]
}

function articleItem(article: ArticleListDto, siteUrl: string): FeedItem {
  return {
    title: article.title,
    url: `${siteUrl}/blog/${encodeURIComponent(article.slug)}`,
    summary: article.excerpt,
    published: article.publishedAt,
    author: article.author?.name || article.authorName || undefined,
    categories: [article.category?.name, ...article.tags.map(tag => tag.name)].filter((c): c is string => !!c)
  }
}

async function loadScope(apiBase: string, scope: FeedScope, slug: string, siteUrl: string): Promise<ScopeContent> {
  const query = { page: 1, pageSize: FEED_SIZE }
  const articles = (list: ArticleListDto[]) => list.map(article => articleItem(article, siteUrl))
  const path = encodeURIComponent(slug)

  switch (scope) {
    case 'site': {
      const response = await $fetch<ApiResponse<ArticleListDto[]>>(`${apiBase}/api/v1/articles`, { query })
      return { items: articles(response.data) }
    }
    case 'category':
    case 'tag': {
      const collection = scope === 'category' ? 'categories' : 'tags'
      // The articles endpoint answers 404 for an unknown slug; the list gives the name and description
      const [response, all] = await Promise.all([
        $fetch<ApiResponse<ArticleListDto[]>>(`${apiBase}/api/v1/${collection}/${path}/articles`, { query }),
        $fetch<ApiResponse<(CategoryDto | TagDto)[]>>(`${apiBase}/api/v1/${collection}`)
      ])
      const match = all.data.find(entry => entry.slug === slug)
      return {
        title: match?.name ?? slug,
        description: match && 'description' in match ? match.description : undefined,
        items: articles(response.data)
      }
    }
    case 'series': {
      const response = await $fetch<ApiResponse<SeriesDetailDto>>(`${apiBase}/api/v1/series/${path}`)
      const series = response.data
      // The series lists its parts in reading order; a feed lists the newest first
      const items = [...series.articles]
        .sort((a, b) => (b.publishedAt ?? '').localeCompare(a.publishedAt ?? ''))
        .slice(0, FEED_SIZE)
        .map(article => ({
          title: article.title,
          url: `${siteUrl}/blog/${encodeURIComponent(article.slug)}`,
          summary: article.excerpt,
          published: article.publishedAt,
          categories: [series.name]
        }))
      return { title: series.name, description: series.description, items }
    }
  }
}

/** Route handler for one feed: `export default feedHandler('category', 'atom')` in server/routes/category/[slug]/atom.xml.ts */
export function feedHandler(scope: FeedScope, format: FeedFormat) {
  return defineEventHandler(async (event: H3Event) => {
    const config = useRuntimeConfig(event)
    const apiBase = config.apiBaseInternal || config.public.apiBase
    const siteUrl = getRequestURL(event).origin
    const slug = scope === 'site' ? '' : getRouterParam(event, 'slug', { decode: true }) ?? ''

    let content: ScopeContent
    let settings: FeedSettings
    try {
      const [scoped, settingsResponse] = await Promise.all([
        loadScope(apiBase, scope, slug, siteUrl),
        // Without settings the feed still works, with the host name as its title
        $fetch<ApiResponse<FeedSettings>>(`${apiBase}/api/v1/settings`).catch(() => undefined)
      ])
      content = scoped
      settings = settingsResponse?.data ?? {}
    } catch (error) {
      if ((error as { statusCode?: number } | undefined)?.statusCode === 404) {
        throw createError({ statusCode: 404, statusMessage: 'Not Found' })
      }
      // Never answer with an empty but valid feed: readers would show it as "no articles"
      console.error(`Failed to load the ${scope} feed:`, error)
      throw createError({ statusCode: 502, statusMessage: 'Bad Gateway' })
    }

    const siteName = settings.siteName?.trim() || getRequestURL(event).host
    const pageUrl = scope === 'site' ? `${siteUrl}/` : `${siteUrl}/${scope}/${encodeURIComponent(slug)}`
    const feed: Feed = {
      title: content.title ? `${content.title} | ${siteName}` : siteName,
      description: content.description?.trim() || settings.siteDescription?.trim() || '',
      language: settings.language?.trim() || DEFAULT_LANGUAGE,
      pageUrl,
      selfUrl: `${siteUrl}${feedPath(scope, format, slug)}`,
      siteName,
      items: content.items
    }

    setHeader(event, 'Content-Type', FEED_CONTENT_TYPES[format])
    setHeader(event, 'Cache-Control', 'public, max-age=600')
    return buildFeed(feed, format)
  })
}
