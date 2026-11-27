// robots.txt needs an absolute Sitemap URL, so it is built per request from the request origin (same as sitemap.xml)
export default defineEventHandler((event) => {
  const siteUrl = getRequestURL(event).origin

  setHeader(event, 'Content-Type', 'text/plain; charset=utf-8')
  return `User-agent: *\nAllow: /\n\nSitemap: ${siteUrl}/sitemap.xml\n`
})
