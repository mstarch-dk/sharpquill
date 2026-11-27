# Privacy and GDPR guidance for operators

If you run a site with Sharpquill and it is reachable by people in the EU/EEA, you are the **data controller** for
the personal data it collects. This page lists what the software stores, how long, and how to delete it, so you
can write your own privacy policy and handle requests. It is not legal advice.

## What is collected

| Data | Where it comes from | Stored in | Personal data |
| --- | --- | --- | --- |
| **Authors** (admin users) | Created by an admin or by `db-init` | `authors`, `refresh_tokens` | Name, email, bio, avatar, password hash, optional certificate fingerprint |
| **Reader accounts** (only if sign-in is enabled) | Sign-up form on the public site | `website_users`, `website_login_tokens`, `website_refresh_tokens` | Name, email, status, timestamps |
| **Comments** | Comment form under articles | `comments` | Name, email, comment text, **IP address**, timestamps |
| **Likes** | Like button | `article_likes`; a random visitor id in the browser's `localStorage` | Random visitor id, **IP address**, timestamp |
| **Contact form messages** | Contact form | **Not stored.** Sent by email (SMTP) to the site's contact address | Name, email, message, in your mailbox and your email provider's |
| **Analytics** (only if Umami is enabled) | Umami tracker script | Umami's own database | Umami is cookie-less and does not store IP addresses; it stores page views and hashed, rotating session ids |
| **Reverse-proxy and container logs** | Every request | Proxy access logs (if enabled), Docker logs | IP address, URL, user agent; retention depends on your log settings |
| **Backups** | `scripts/backup.sh` | Wherever you keep backups | Everything above |

Browser storage used by the public site:

- `websiteRefreshToken` cookie (httpOnly) and an access token in `localStorage`, only for signed-in readers.
- A random visitor id in `localStorage` so a reader can like an article once. It is set when the like button is used
  or the like status is loaded.

There are no third-party trackers, ad scripts or external fonts in the default setup. Link preview cards and
images that authors embed by URL are loaded from the sites they point to, which then see the reader's IP address.

## Retention

The software does **not** delete personal data automatically, except that expired login and refresh tokens become
unusable (they are not purged). Decide on retention periods yourself and write them into your privacy policy,
for example:

- comment IP addresses: kept for abuse handling for a limited time (say 90 days), then cleared;
- like IP addresses: not needed after rate limiting, clear them regularly;
- rejected or inactive reader accounts: delete after a period;
- backups: rotate (the backup script's retention setting) so deleted data does not live on indefinitely.

Example clean-up (run with `docker compose exec db psql -U <user> -d cms`):

```sql
-- Clear IP addresses older than 90 days
UPDATE comments      SET ip_address = NULL WHERE created_at < now() - interval '90 days';
UPDATE article_likes SET ip_address = NULL WHERE created_at < now() - interval '90 days';

-- Purge expired or used login and refresh tokens
DELETE FROM website_login_tokens   WHERE expires_at < now() OR used_at IS NOT NULL;
DELETE FROM website_refresh_tokens WHERE expires_at < now() OR revoked_at IS NOT NULL;
DELETE FROM refresh_tokens         WHERE expires_at < now() OR revoked_at IS NOT NULL;
```

Check the table and column names against your version (`\d comments`) before running anything, and take a backup
first.

## Handling requests (access, deletion)

**Comments:** delete them in the admin panel under *Comments*, or by email address in SQL:

```sql
SELECT id, article_id, author_name, created_at FROM comments WHERE author_email = 'reader@example.com';
DELETE FROM comments WHERE author_email = 'reader@example.com';
```

**Reader accounts:** the admin panel can reject an account, but has no delete button yet. Delete in SQL; the
reader's login and refresh tokens are removed with it (`ON DELETE CASCADE`):

```sql
SELECT id, name, email, status, created_at FROM website_users WHERE email = 'reader@example.com';
DELETE FROM website_users WHERE email = 'reader@example.com';
```

**Likes** are tied to a random browser id, not to a person, so they can only be matched by IP address, if at all.

**Contact form messages** live in your mailbox; delete them there.

**Authors** are managed in the admin panel. Articles keep a display author name, so check published content too.

For an access request, the `SELECT` statements above give you what is stored about an email address.
Remember that the data stays in backups until they rotate out; say so in your policy.

## What you need to provide

- A **privacy policy** page (create it as a page in the CMS and link it from the navigation or footer) that names
  you as controller and covers the table above, your retention periods, your hosting and email providers, and how
  to contact you.
- A legal basis for each processing purpose (for example legitimate interest for comment moderation and abuse
  prevention, consent or contract for reader accounts).
- If you add anything that sets non-essential cookies or loads third-party content, a consent mechanism. The
  default setup (Umami, the like visitor id, sign-in tokens) is designed not to need a cookie banner, but whether
  that holds for your setup is your call.
- A data processing agreement with your hosting, email (SMTP) and backup storage providers.
