import type { ApiResponse, ArticleListDto, CategoryDto, TagDto } from '~/types/api'

export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig()
  const apiBase = config.apiBaseInternal || config.public.apiBase
  const siteUrl = getRequestURL(event).origin

  const urls: { loc: string; lastmod?: string; changefreq: string; priority: number }[] = []

  // Static pages
  urls.push(
    { loc: '/', changefreq: 'daily', priority: 1.0 },
    { loc: '/blog', changefreq: 'daily', priority: 0.9 },
    { loc: '/about', changefreq: 'monthly', priority: 0.5 }
  )

  try {
    // Fetch all articles (paginated, get first 100 for sitemap)
    const articlesResponse = await $fetch<ApiResponse<ArticleListDto[]>>(
      `${apiBase}/api/v1/articles`,
      { query: { page: 1, pageSize: 100 } }
    )

    if (articlesResponse?.data) {
      for (const article of articlesResponse.data) {
        urls.push({
          loc: `/blog/${article.slug}`,
          lastmod: article.publishedAt?.split('T')[0],
          changefreq: 'weekly',
          priority: 0.8
        })
      }
    }

    // Fetch categories
    const categoriesResponse = await $fetch<ApiResponse<CategoryDto[]>>(
      `${apiBase}/api/v1/categories`
    )

    if (categoriesResponse?.data) {
      for (const category of categoriesResponse.data) {
        urls.push({
          loc: `/category/${category.slug}`,
          changefreq: 'weekly',
          priority: 0.6
        })
      }
    }

    // Fetch tags
    const tagsResponse = await $fetch<ApiResponse<TagDto[]>>(
      `${apiBase}/api/v1/tags`
    )

    if (tagsResponse?.data) {
      for (const tag of tagsResponse.data) {
        urls.push({
          loc: `/tag/${tag.slug}`,
          changefreq: 'weekly',
          priority: 0.5
        })
      }
    }
  } catch (error) {
    console.error('Failed to fetch data for sitemap:', error)
  }

  // Generate XML
  const xml = `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
${urls.map(url => `  <url>
    <loc>${siteUrl}${url.loc}</loc>${url.lastmod ? `
    <lastmod>${url.lastmod}</lastmod>` : ''}
    <changefreq>${url.changefreq}</changefreq>
    <priority>${url.priority}</priority>
  </url>`).join('\n')}
</urlset>`

  setHeader(event, 'Content-Type', 'application/xml')
  return xml
})
