// Shared frontend configuration. Mirrors the backend's UrlPolicy limits
// (TinyLink.Api/Features/Links/UrlPolicy.cs) so fast client feedback matches
// server validation; the server remains the source of truth.

export const SHORT_CODE_PATTERN = /^[0-9A-Za-z]{7}$/
export const MAX_URL_LENGTH = 2000

function stripTrailingSlashes(value: string): string {
  return value.replace(/\/+$/, '')
}

function readEnv(name: string): string {
  return (import.meta.env[name] as string | undefined ?? '').trim()
}

/**
 * Base URL for JSON API calls. Empty string means same-origin, which the Vite
 * dev proxy forwards to the API. Set VITE_API_BASE_URL for split-origin deploys
 * (requires the API CORS policy).
 */
export const apiBaseUrl = stripTrailingSlashes(readEnv('VITE_API_BASE_URL'))

/**
 * Base URL used for full-page navigation to short links. Browsers transparently
 * follow the 302, so this must be an origin that serves the API redirects:
 * the same origin when the SPA is served by the API itself (production Docker
 * image, no env set), or the explicit API origin in split-origin dev.
 */
export const redirectBaseUrl = stripTrailingSlashes(
  readEnv('VITE_REDIRECT_BASE_URL') ||
    readEnv('VITE_API_BASE_URL') ||
    window.location.origin,
)

export function shortUrlFor(code: string): string {
  return `${redirectBaseUrl}/${code}`
}
/**
 * If the input already carries a scheme (http://, https://, or anything else
 * the validator will then reject), it is left alone. Otherwise https:// is
 * prepended, so pasting `example.com/article` just works.
 */
export function normalizeUrl(raw: string): string {
  const value = raw.trim()
  if (value === '') return value
  if (/^[a-zA-Z][a-zA-Z0-9+.-]*:\/\//.test(value)) return value
  return `https://${value}`
}

/** Client-side mirror of UrlPolicy. Returns an error message, or null when OK. */
export function validateUrl(raw: string): string | null {
  const value = raw.trim()
  if (value.length === 0) return 'URL is required.'
  if (value.length > MAX_URL_LENGTH) {
    return `Must be at most ${MAX_URL_LENGTH} characters in length.`
  }
  let parsed: URL
  try {
    parsed = new URL(value)
  } catch {
    return 'Must be an absolute URL.'
  }
  if (parsed.protocol !== 'http:' && parsed.protocol !== 'https:') {
    return 'Must use the http or https scheme.'
  }
  if (parsed.username !== '' || parsed.password !== '') {
    return 'Must not bear embedded credentials.'
  }
  return null
}
