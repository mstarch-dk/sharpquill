import type { MaybeRefOrGetter } from 'vue'
import { feedPath } from '~/utils/feed'
import type { FeedScope } from '~/utils/feed'

// <link rel="alternate"> autodiscovery for the RSS and Atom feeds (served by server/routes, built by utils/feed.ts).
// The site-wide feeds are advertised on every page by plugins/site-head.ts; category, tag and series pages add their own.
export function useFeedLinks(scope: FeedScope, slug: string | undefined, name: MaybeRefOrGetter<string>) {
  useHead({
    link: (['rss', 'atom'] as const).map(format => ({
      rel: 'alternate',
      type: format === 'rss' ? 'application/rss+xml' : 'application/atom+xml',
      href: feedPath(scope, format, slug),
      title: () => `${toValue(name)} (${format === 'rss' ? 'RSS' : 'Atom'})`
    }))
  })
}
