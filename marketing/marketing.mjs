#!/usr/bin/env node
// Marketing material for the CMS: a seeded demo site, screenshots, the brochure PDF and its checks.
// Usage: npm run marketing -- <command> (see README.md). Everything runs against the demo stack
// (docker compose project "cms-demo", ports from demo/demo.env), never the dev stack.
import { spawnSync } from 'node:child_process'
import { existsSync, mkdirSync, readFileSync, readdirSync, statSync, writeFileSync, copyFileSync } from 'node:fs'
import { dirname, join, relative } from 'node:path'
import { fileURLToPath, pathToFileURL } from 'node:url'
import { randomUUID } from 'node:crypto'
import YAML from 'yaml'

const MK = dirname(fileURLToPath(import.meta.url))
const ROOT = join(MK, '..')
const DEMO_ENV = join(MK, 'demo', 'demo.env')
const SEED = join(MK, 'demo', 'seed.yaml')
const IMAGES = join(MK, 'demo', 'images')
const SHOTS = join(MK, 'shots.yaml')
const FEATURES = join(MK, 'features.yaml')
const SITE = join(MK, 'site')
const IMG = join(SITE, 'img')
const PDF = join(MK, 'sharpquill-brochure.pdf')
const EDITION = join(MK, 'edition.json')
const EXPLORE = join(MK, '.explore')
const PROJECT = 'cms-demo'
const DEV_PORTS = new Set(['3000', '3001', '5000', '5432'])

const env = Object.fromEntries(readFileSync(DEMO_ENV, 'utf8').split('\n')
  .filter(l => /^[A-Z_]+=/.test(l)).map(l => [l.slice(0, l.indexOf('=')), l.slice(l.indexOf('=') + 1)]))
for (const k of ['API_PORT', 'WEB_PORT', 'ADMIN_PORT']) {
  if (DEV_PORTS.has(env[k])) die(`demo.env: ${k}=${env[k]} is a dev stack port; the demo must use its own ports`)
}
const API = `http://localhost:${env.API_PORT}`
const BASE = { web: `http://localhost:${env.WEB_PORT}`, admin: `http://localhost:${env.ADMIN_PORT}` }
const VIEWPORTS = {
  desktop: { viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1.5 },
  mobile: { viewport: { width: 390, height: 844 }, deviceScaleFactor: 2, isMobile: true, hasTouch: true },
}

function die(msg) { console.error(msg); process.exit(1) }
const load = p => YAML.parse(readFileSync(p, 'utf8'))
const sleep = ms => new Promise(r => setTimeout(r, ms))
const slugify = t => t.toLowerCase().replaceAll('æ', 'ae').replaceAll('ø', 'oe').replaceAll('å', 'aa')
  .normalize('NFKD').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
const rel = p => relative(ROOT, p).replaceAll('\\', '/')

function compose(args, opts = {}) {
  const r = spawnSync('docker', ['compose', '-p', PROJECT, '--env-file', DEMO_ENV, ...args],
    { cwd: ROOT, stdio: opts.capture ? 'pipe' : 'inherit', encoding: 'utf8' })
  if (r.status !== 0 && !opts.allowFail) die(`docker compose ${args.join(' ')} failed`)
  return r.stdout ?? ''
}

async function browser() {
  const { chromium } = await import('playwright')
  return chromium.launch()
}

// ---------------- demo stack ----------------

function demoUp() {
  compose(['up', '-d', '--build', '--wait', 'db', 'db-init', 'api', 'web', 'admin'])
  console.log(`demo up: web ${BASE.web}  admin ${BASE.admin}  api ${API}`)
}

function demoDown() {
  compose(['down', '-v', '--remove-orphans'])
}

// ---------------- generated images ----------------

function rng(seed) { // mulberry32
  return () => {
    seed |= 0; seed = (seed + 0x6D2B79F5) | 0
    let t = Math.imul(seed ^ (seed >>> 15), 1 | seed)
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296
  }
}

function coverSvg(spec, w = 1600, h = 900) {
  const r = rng(spec.seed)
  const hills = spec.hills.map((fill, i) => {
    const base = h * (0.52 + i * 0.14)
    const pts = Array.from({ length: 6 }, (_, k) => [k * w / 5, base - r() * h * 0.16])
    let d = `M0 ${h} L${pts[0][0]} ${pts[0][1]}`
    for (let k = 1; k < pts.length; k++) {
      const [x0, y0] = pts[k - 1], [x1, y1] = pts[k]
      d += ` C${x0 + w / 10} ${y0} ${x1 - w / 10} ${y1} ${x1} ${y1}`
    }
    return `<path d="${d} L${w} ${h} Z" fill="${fill}"/>`
  }).join('')
  const rows = Array.from({ length: 5 }, (_, i) => {
    const y = h * 0.86 + i * 22
    return `<line x1="${w * 0.08}" y1="${y}" x2="${w * 0.92}" y2="${y - 30}" stroke="rgba(255,255,255,.16)" stroke-width="6" stroke-dasharray="2 26" stroke-linecap="round"/>`
  }).join('')
  const sx = w * (0.18 + r() * 0.64), sy = h * (0.2 + r() * 0.12)
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">
    <defs><linearGradient id="s" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="${spec.sky[0]}"/><stop offset="1" stop-color="${spec.sky[1]}"/></linearGradient></defs>
    <rect width="${w}" height="${h}" fill="url(#s)"/>
    <circle cx="${sx}" cy="${sy}" r="${h * 0.09}" fill="${spec.sun}" opacity=".92"/>
    ${hills}${rows}</svg>`
}

async function images() {
  mkdirSync(IMAGES, { recursive: true })
  const b = await browser()
  const pg = await b.newPage({ viewport: { width: 1600, height: 900 } })
  for (const spec of load(SEED).images) {
    await pg.setContent(`<body style="margin:0">${coverSvg(spec)}</body>`)
    await pg.locator('svg').screenshot({ path: join(IMAGES, `${spec.name}.png`) })
  }
  await b.close()
  console.log(`${rel(IMAGES)}: ${load(SEED).images.length} images`)
}

// ---------------- seed ----------------

class Api {
  constructor() { this.token = null }
  async req(method, path, body, { form, retry429 = true, auth = true } = {}) {
    for (;;) {
      const headers = {}
      if (auth && this.token) headers.Authorization = `Bearer ${this.token}`
      if (body !== undefined && !form) headers['Content-Type'] = 'application/json'
      const res = await fetch(API + path, { method, headers, body: form ?? (body === undefined ? undefined : JSON.stringify(body)) })
      if (res.status === 429 && retry429) { // the public endpoints are rate limited per IP; wait for the window
        console.log(`  rate limited on ${path}, waiting 61 s`)
        await sleep(61000)
        continue
      }
      const text = await res.text()
      if (!res.ok) throw new Error(`${method} ${path}: HTTP ${res.status} ${text.slice(0, 400)}`)
      return text ? JSON.parse(text).data : null
    }
  }
  get = (p) => this.req('GET', p)
  post = (p, b, o) => this.req('POST', p, b, o)
  put = (p, b) => this.req('PUT', p, b)
}

async function seed() {
  const s = load(SEED)
  const api = new Api()
  const login = await api.req('POST', '/api/admin/auth/login', { email: env.ADMIN_EMAIL, password: env.ADMIN_PASSWORD })
  api.token = login.accessToken
  const existing = await api.get('/api/admin/articles?pageSize=1')
  if (existing.length) die('The demo database already has content. Start fresh: npm run marketing -- demo-down && npm run marketing -- demo-up')

  console.log('settings'); await api.put('/api/admin/settings', s.site)
  await api.put(`/api/admin/authors/${login.author.id}`, { email: env.ADMIN_EMAIL, role: 'admin', ...s.admin })

  if (!existsSync(IMAGES) || readdirSync(IMAGES).length < s.images.length) await images()
  const media = {}
  for (const im of s.images) {
    const form = new FormData()
    form.append('file', new Blob([readFileSync(join(IMAGES, `${im.name}.png`))], { type: 'image/png' }), `${im.name}.png`)
    media[im.name] = await api.post('/api/admin/media', undefined, { form })
  }
  console.log(`media: ${Object.keys(media).length}`)

  for (const a of s.authors) await api.post('/api/admin/authors', { bio: null, avatarUrl: null, certificateThumbprint: null, ...a })
  const cats = {}
  for (const c of s.categories) cats[c.slug] = (await api.post('/api/admin/categories', c)).id
  const tags = {}
  for (const name of s.tags) tags[name] = (await api.post('/api/admin/tags', { name, slug: slugify(name) })).id

  const block = (b) => {
    const { image, ...rest } = b
    if (b.type === 'image') return { ...rest, mediaId: media[image].id }
    if (b.type === 'link' && image) return { ...rest, imageUrl: media[image].url }
    return rest
  }
  const day = (n) => new Date(Date.now() - n * 86400000).toISOString()
  const body = (a, contentType) => ({
    title: a.title, slug: a.slug, content: a.body ?? null, excerpt: a.excerpt ?? null,
    layout: a.layout ? { sections: a.layout.map(sec => ({ type: sec.type, style: sec.style ?? null, blocks: sec.blocks.map(block) })) } : null,
    metaTitle: null, metaDescription: a.excerpt ?? null,
    categoryId: a.category ? cats[a.category] : null, featuredImageId: a.image ? media[a.image].id : null,
    tagIds: (a.tags ?? []).map(t => tags[t]), status: a.status === 'draft' ? 0 : 1, contentType, commentsEnabled: contentType === 0,
    requiresSignIn: !!a.members_only, pageStyle: a.style ?? null, featuredImageSize: a.image_size ?? 'medium',
    authorName: a.author ?? null, publishedAt: day(a.days_ago ?? 0),
  })
  const arts = {}
  for (const a of s.articles) {
    const [first, ...later] = [...(a.revisions ?? []).map(r => ({ ...a, ...r })), a]
    arts[a.slug] = await api.post('/api/admin/articles', body(first, 0))
    for (const r of later) await api.put(`/api/admin/articles/${arts[a.slug].id}`, body(r, 0))
  }
  const pages = {}
  for (const p of s.pages) pages[p.slug] = await api.post('/api/admin/pages', body(p, 1))
  console.log(`articles: ${Object.keys(arts).length}, pages: ${Object.keys(pages).length}`)

  for (const sr of s.series) {
    const created = await api.post('/api/admin/series', { name: sr.name, slug: sr.slug, description: sr.description, themeSlug: null })
    const members = s.articles.filter(a => a.series?.[0] === sr.slug).sort((x, y) => x.series[1] - y.series[1])
    for (const [i, a] of members.entries()) await api.post(`/api/admin/series/${created.id}/articles`, { articleId: arts[a.slug].id, position: i + 1 })
  }

  const navs = await api.get('/api/admin/navigations')
  for (const [slug, items] of Object.entries(s.navigation)) {
    const nav = navs.find(n => n.slug === slug) ?? die(`no navigation ${slug}`)
    await api.put(`/api/admin/navigations/${nav.id}/items`, {
      items: items.map((it, i) => ({
        label: it.label, position: i + 1, parentId: null, openInNewTab: !!it.url,
        itemType: it.page ? 0 : it.route ? 1 : 2,
        articleId: it.page ? pages[it.page].id : null, systemRoute: it.route ?? null, url: it.url ?? null,
      })),
    })
  }

  for (const r of s.readers) await api.post('/api/v1/auth/signup', { name: r.name, email: r.email }, { auth: false })
  const users = await api.get('/api/admin/website-users?pageSize=100')
  for (const r of s.readers.filter(r => r.status !== 'pending')) {
    const u = users.find(u => u.email === r.email) ?? die(`reader ${r.email} not found after signup`)
    await api.put(`/api/admin/website-users/${u.id}/moderate`, { status: r.status === 'approved' ? 1 : 2 })
  }
  console.log(`readers: ${s.readers.length}`)

  const commentIds = []
  for (const c of s.comments) {
    const art = arts[c.article]
    await api.post(`/api/v1/articles/${art.id}/comments`, {
      authorName: c.name, authorEmail: c.email, content: c.text, honeypot: '',
      parentCommentId: c.reply_to !== undefined ? commentIds[c.reply_to] : null,
    }, { auth: false })
    const list = await api.get('/api/admin/comments?pageSize=100')
    const mine = list.find(x => x.content === c.text) ?? die(`comment not found: ${c.text}`)
    commentIds.push(mine.id)
    if (c.status !== 'pending') await api.put(`/api/admin/comments/${mine.id}/moderate`, { status: c.status === 'approved' ? 1 : 2 })
  }
  console.log(`comments: ${commentIds.length}`)

  for (const [slug, n] of Object.entries(s.likes)) {
    for (let i = 0; i < n; i++) await api.post(`/api/v1/articles/${arts[slug].id}/likes`, { visitorId: randomUUID() }, { auth: false })
  }
  console.log('seed done')
}

// ---------------- screenshots ----------------

async function adminLogin(ctx) {
  const pg = await ctx.newPage()
  await pg.goto(`${BASE.admin}/login`)
  await pg.locator('input[type=email]').fill(env.ADMIN_EMAIL)
  await pg.locator('input[type=password]').fill(env.ADMIN_PASSWORD)
  await pg.locator('button[type=submit]').click()
  await pg.waitForURL(u => !u.pathname.startsWith('/login'), { timeout: 20000 })
  await pg.close()
}

function contexts(b) {
  const cache = new Map()
  return async (app, vp, scheme) => {
    const key = `${app}/${vp}/${scheme}`
    if (!cache.has(key)) {
      const ctx = await b.newContext({ ...VIEWPORTS[vp], colorScheme: scheme })
      if (app === 'admin') await adminLogin(ctx)
      cache.set(key, ctx)
    }
    return cache.get(key)
  }
}

function locate(pg, loc) {
  let l = pg.locator(loc.css)
  if (loc.has_text) l = l.filter({ hasText: new RegExp(loc.has_text) })
  return loc.nth !== undefined ? l.nth(loc.nth) : l.first()
}

async function act(pg, a) {
  if (a.wait) return pg.waitForTimeout(a.wait)
  if (a.goto) { await pg.goto(BASE[a.app ?? 'admin'] + a.goto); return pg.waitForLoadState('networkidle') }
  const el = locate(pg, a)
  if (a.do === 'click') await el.click()
  else if (a.do === 'hover') await el.hover()
  else if (a.do === 'fill') await el.fill(a.text)
  else if (a.do === 'scroll') await el.evaluate((e, off) => window.scrollTo(0, e.getBoundingClientRect().top + window.scrollY - off), a.offset ?? 80)
  await pg.waitForTimeout(a.settle ?? 500)
}

const BAD_TEXT = ['Something went wrong', 'Internal Server Error', 'Failed to load', 'Not Found', 'Page not found']

// {article:SLUG}, {page:SLUG}, {series:SLUG}, {nav:SLUG} in a shot's path become the demo ids
let ids = null
async function resolve(path) {
  if (!path.includes('{')) return path
  if (!ids) {
    const api = new Api()
    api.token = (await api.req('POST', '/api/admin/auth/login', { email: env.ADMIN_EMAIL, password: env.ADMIN_PASSWORD })).accessToken
    const map = async (p) => Object.fromEntries((await api.get(p)).map(x => [x.slug, x.id]))
    ids = { article: await map('/api/admin/articles?pageSize=100'), page: await map('/api/admin/pages?pageSize=100'),
      series: await map('/api/admin/series'), nav: await map('/api/admin/navigations') }
  }
  return path.replace(/\{(\w+):([\w-]+)\}/g, (_, kind, slug) => ids[kind]?.[slug] ?? die(`no ${kind} ${slug} in the demo`))
}

async function shoot(ctxFor, s) {
  const ctx = await ctxFor(s.app, s.viewport ?? 'desktop', s.scheme ?? 'light')
  const pg = await ctx.newPage()
  await pg.goto(BASE[s.app] + await resolve(s.path))
  await pg.waitForLoadState('networkidle')
  if (s.wait_for) await pg.locator(s.wait_for).first().waitFor({ state: 'visible', timeout: 30000 })
  await pg.waitForTimeout(s.settle ?? 1200)
  for (const a of s.actions ?? []) await act(pg, a)
  const text = await pg.locator('body').innerText()
  for (const bad of BAD_TEXT) if (text.includes(bad) && s.allow_text !== bad) throw new Error(`the page shows "${bad}" (${s.path})`)
  if (pg.url().includes('/login') && s.app === 'admin') throw new Error('not logged in')
  const out = join(IMG, `${s.id}.png`)
  if (s.clip) {
    const c = s.clip
    const els = [locate(pg, c), ...(c.to ? [locate(pg, c.to)] : [])]
    for (const e of els) await e.waitFor({ state: 'visible', timeout: 15000 })
    await els[0].scrollIntoViewIfNeeded()
    const top = await pg.evaluate(() => window.scrollY)
    const pad = c.pad ?? 12
    const boxes = []
    for (const e of els) { const bb = await e.boundingBox(); boxes.push({ ...bb, y: bb.y + top }) }
    if (c.to?.edge === 'top') boxes[boxes.length - 1] = { ...boxes.at(-1), y: boxes.at(-1).y - pad - 4, height: 0 }
    const x0 = Math.max(0, Math.min(...boxes.map(b => b.x)) - pad), y0 = Math.max(0, Math.min(...boxes.map(b => b.y)) - pad)
    let box = { x: x0, y: y0, width: Math.max(...boxes.map(b => b.x + b.width)) + pad - x0, height: Math.max(...boxes.map(b => b.y + b.height)) + pad - y0 }
    if (c.full_width) box = { ...box, x: 0, width: pg.viewportSize().width }
    if (c.span) { const sb = await locate(pg, c.span).boundingBox(); box = { ...box, x: Math.max(0, sb.x - pad), width: sb.width + 2 * pad } }
    if (c.max_height) box.height = Math.min(box.height, c.max_height)
    await pg.screenshot({ path: out, fullPage: true, clip: box })
  } else {
    await pg.screenshot({ path: out, fullPage: !!s.full_page })
  }
  await pg.close()
  if (s.also) copyFileSync(out, join(ROOT, s.also))
  return out
}

async function shots(only) {
  const spec = load(SHOTS).shots
  const unknown = only.filter(id => !spec.some(s => s.id === id))
  if (unknown.length) die(`Unknown shot ids: ${unknown.join(', ')}`)
  mkdirSync(IMG, { recursive: true })
  const b = await browser()
  const ctxFor = contexts(b)
  const failed = []
  for (const s of spec) {
    if (only.length && !only.includes(s.id)) continue
    try { console.log(`ok   ${rel(await shoot(ctxFor, s))}${s.also ? ` (+ ${s.also})` : ''}`) } catch (e) { failed.push(s.id); console.log(`FAIL ${s.id}: ${e.message.split('\n')[0]}`) }
  }
  await b.close()
  if (failed.length) die(`${failed.length} shot(s) failed: ${failed.join(', ')}`)
}

async function explore(paths, scheme = 'light') {
  // paths: web:/blog or admin:/articles; full-page captures to look at before writing shots.yaml
  mkdirSync(EXPLORE, { recursive: true })
  const b = await browser()
  const ctxFor = contexts(b)
  for (const p of paths) {
    const [app, path] = p.includes(':/') ? [p.slice(0, p.indexOf(':')), p.slice(p.indexOf(':') + 1)] : ['web', p]
    const pg = await (await ctxFor(app, 'desktop', scheme)).newPage()
    await pg.goto(BASE[app] + path)
    await pg.waitForLoadState('networkidle')
    await pg.waitForTimeout(1500)
    const f = join(EXPLORE, `${app}_${path.replace(/[^A-Za-z0-9]+/g, '_').replace(/^_|_$/g, '') || 'home'}.png`)
    await pg.screenshot({ path: f, fullPage: true })
    console.log(`${f}  (${await pg.evaluate(() => document.documentElement.scrollHeight)} px)`)
    await pg.close()
  }
  await b.close()
}

// ---------------- pdf ----------------

const FOOTER = '<div style="font-size:8px;width:100%;text-align:center;color:#888">Sharpquill · page <span class="pageNumber"></span> of <span class="totalPages"></span></div>'

async function pdf() {
  const b = await browser()
  const pg = await b.newPage({ viewport: { width: 1120, height: 900 }, colorScheme: 'light' })
  await pg.goto(pathToFileURL(join(SITE, 'index.html')).href)
  await pg.evaluate(() => document.querySelectorAll('img[loading=lazy]').forEach(i => { i.loading = 'eager' }))
  await pg.waitForLoadState('networkidle')
  await pg.evaluate(() => Promise.all([...document.images].map(i => i.complete ? 1 : new Promise(r => { i.onload = i.onerror = r }))))
  await pg.evaluate(() => document.fonts.ready)
  await pg.emulateMedia({ media: 'print' })
  await pg.pdf({ path: PDF, format: 'A4', printBackground: true, scale: 0.68,
    margin: { top: '14mm', bottom: '14mm', left: '12mm', right: '12mm' },
    displayHeaderFooter: true, headerTemplate: '<span></span>', footerTemplate: FOOTER })
  await b.close()
  console.log(`${rel(PDF)}: ${pdfPages()} pages`)
}

const pdfPages = () => (readFileSync(PDF, 'latin1').match(/\/Type\s*\/Page[^s]/g) ?? []).length

// ---------------- check ----------------

const ALLOWED_HOST = /(^|\.)example(\.(com|org|net))?$|^localhost$|^(fonts\.googleapis\.com|fonts\.gstatic\.com|github\.com|umami\.is|caddyserver\.com|www\.w3\.org|claude\.com)$/

async function check() {
  const problems = []
  const html = readFileSync(join(SITE, 'index.html'), 'utf8')
  const shotsSpec = load(SHOTS).shots
  const features = load(FEATURES).features
  const ids = new Set([...html.matchAll(/\bid="([^"]+)"/g)].map(m => m[1]))
  const shotIds = new Set(shotsSpec.map(s => s.id))
  for (const f of features) {
    if (!ids.has(f.section)) problems.push(`features.yaml ${f.id}: section #${f.section} not in index.html`)
    for (const sh of f.shots ?? []) if (!shotIds.has(sh)) problems.push(`features.yaml ${f.id}: unknown shot ${sh}`)
  }
  const featureIds = new Set(features.map(f => f.id))
  for (const s of shotsSpec) {
    if (!featureIds.has(s.feature)) problems.push(`shots.yaml ${s.id}: unknown feature ${s.feature}`)
    if (!features.some(f => f.shots?.includes(s.id))) problems.push(`shots.yaml ${s.id}: not listed under its feature in features.yaml`)
    const uses = html.split(`img/${s.id}.png`).length - 1
    if (uses !== 1) problems.push(`index.html uses img/${s.id}.png ${uses} times (want 1)`)
    const f = join(IMG, `${s.id}.png`)
    if (!existsSync(f)) problems.push(`missing ${rel(f)} (run shots)`)
    else if (statSync(f).size < 8000) problems.push(`${rel(f)} is suspiciously small (${statSync(f).size} bytes)`)
  }
  for (const m of html.matchAll(/img\/([\w-]+)\.png/g)) if (!shotIds.has(m[1])) problems.push(`index.html references img/${m[1]}.png, not in shots.yaml`)

  // Leak check: only reserved demo domains, no addresses outside .example, no private paths
  for (const file of [join(SITE, 'index.html'), SEED, SHOTS, FEATURES, join(MK, 'README.md')]) {
    const text = readFileSync(file, 'utf8')
    for (const m of text.matchAll(/https?:\/\/([a-z0-9.-]+)/gi)) if (!ALLOWED_HOST.test(m[1].toLowerCase())) problems.push(`${rel(file)}: host ${m[1]} is not a demo/allowed host`)
    for (const m of text.matchAll(/[\w.+-]+@([\w-]+\.)+[a-z]{2,}/gi)) if (!/\.example$/i.test(m[0].split('@')[1]) && !/^noreply@anthropic\.com$/.test(m[0])) problems.push(`${rel(file)}: email ${m[0]} is not on a .example domain`)
    if (/\/home\/|C:\\\\|\/mnt\//.test(text)) problems.push(`${rel(file)}: contains a local file path`)
  }

  // The demo database holds only the seed's writers and articles
  try {
    const s = load(SEED)
    const api = new Api()
    api.token = (await api.req('POST', '/api/admin/auth/login', { email: env.ADMIN_EMAIL, password: env.ADMIN_PASSWORD })).accessToken
    const authors = (await api.get('/api/admin/authors')).map(a => a.email).sort()
    const want = [env.ADMIN_EMAIL, ...s.authors.map(a => a.email)].sort()
    if (JSON.stringify(authors) !== JSON.stringify(want)) problems.push(`demo authors ${authors} differ from the seed ${want}`)
    const arts = await api.get('/api/admin/articles?pageSize=100')
    for (const a of arts) if (!s.articles.some(x => x.slug === a.slug)) problems.push(`demo article ${a.slug} is not in the seed`)
  } catch (e) { problems.push(`demo API not reachable for the database check (${e.message.split('\n')[0]})`) }

  // The brochure works at phone width
  const b = await browser()
  const pg = await b.newPage({ viewport: { width: 390, height: 844 } })
  await pg.goto(pathToFileURL(join(SITE, 'index.html')).href)
  const overflow = await pg.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
  if (overflow > 1) problems.push(`index.html scrolls sideways by ${overflow}px at 390px width`)
  await b.close()

  if (problems.length) { problems.forEach(p => console.log(`- ${p}`)); die(`${problems.length} problem(s)`) }
  console.log(`check ok: ${features.length} features, ${shotsSpec.length} shots${existsSync(PDF) ? `, ${pdfPages()} PDF pages` : ''}`)
}

// ---------------- edition ----------------

function edition(date) {
  const commit = spawnSync('git', ['rev-parse', 'HEAD'], { cwd: ROOT, encoding: 'utf8' }).stdout.trim()
  const ed = {
    date: date ?? new Date().toISOString().slice(0, 10), commit,
    features: load(FEATURES).features.map(f => f.id), shots: load(SHOTS).shots.map(s => s.id), pdf_pages: pdfPages(),
  }
  writeFileSync(EDITION, JSON.stringify(ed, null, 2) + '\n')
  console.log(`${rel(EDITION)}: ${ed.date} at ${commit.slice(0, 7)}`)
}

// ---------------- main ----------------

const [cmd, ...args] = process.argv.slice(2)
const commands = {
  'demo-up': demoUp,
  'demo-down': demoDown,
  images,
  seed,
  explore: () => explore(args.filter(a => !a.startsWith('--')), args.includes('--dark') ? 'dark' : 'light'),
  shots: () => shots(args),
  pdf,
  check,
  edition: () => edition(args[args.indexOf('--date') + 1] && args.includes('--date') ? args[args.indexOf('--date') + 1] : undefined),
  all: async () => { demoDown(); demoUp(); await images(); await seed(); await shots([]); await pdf(); await check() },
}
if (!commands[cmd]) die(`Usage: npm run marketing -- <${Object.keys(commands).join('|')}> [args]`)
await commands[cmd]()
