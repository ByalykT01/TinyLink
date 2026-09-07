import { apiBaseUrl } from '../config'
import { ApiError, type FieldErrorMap } from './errors'

function asRecord(value: unknown): Record<string, unknown> | null {
  return typeof value === 'object' && value !== null
    ? (value as Record<string, unknown>)
    : null
}

function toStringArray(value: unknown): string[] | null {
  if (Array.isArray(value)) {
    const items = value.filter((v): v is string => typeof v === 'string')
    return items.length > 0 ? items : null
  }
  return typeof value === 'string' ? [value] : null
}

function parseRetryAfter(value: string | null): number | undefined {
  if (value === null) return undefined
  const seconds = Number.parseInt(value, 10)
  return Number.isFinite(seconds) && seconds >= 0 ? seconds : undefined
}

function parseProblem(status: number, body: unknown, retryAfter: string | null): ApiError {
  const doc = asRecord(body) ?? {}
  const title =
    typeof doc['title'] === 'string' && doc['title'].length > 0
      ? doc['title']
      : `Request failed (${status}).`
  const detail = typeof doc['detail'] === 'string' ? doc['detail'] : undefined

  let fields: FieldErrorMap | undefined
  const errors = asRecord(doc['errors'])
  if (errors !== null) {
    const parsed: FieldErrorMap = {}
    for (const [key, value] of Object.entries(errors)) {
      const messages = toStringArray(value)
      if (messages !== null) parsed[key] = messages
    }
    if (Object.keys(parsed).length > 0) fields = parsed
  }

  // Trace id may arrive as a top-level member (custom) or inside `extensions`
  // (ASP.NET ProblemDetails extensions). Accept either.
  const extensions = asRecord(doc['extensions'])
  const traceId =
    (typeof doc['traceId'] === 'string' ? doc['traceId'] : undefined) ??
    (extensions !== null && typeof extensions['traceId'] === 'string'
      ? (extensions['traceId'] as string)
      : undefined)

  return new ApiError({
    status,
    title,
    detail,
    fields,
    retryAfterSeconds: parseRetryAfter(retryAfter),
    traceId,
  })
}

/**
 * JSON fetch against the API. Throws ApiError on non-2xx, mapping
 * application/problem+json (including ValidationProblem field errors and the
 * 429 Retry-After header) into a typed error.
 */
export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      ...init,
      headers: { 'Content-Type': 'application/json', ...(init.headers ?? {}) },
    })
  } catch (error) {
    throw new ApiError({
      status: 0,
      title: 'Cannot reach the API.',
      detail:
        error instanceof Error
          ? error.message
          : 'Check that the backend is running and reachable.',
    })
  }

  if (response.status === 204) return undefined as T

  const contentType = response.headers.get('content-type') ?? ''
  const body: unknown = contentType.includes('json')
    ? await response.json().catch(() => null)
    : null

  if (!response.ok) {
    throw parseProblem(response.status, body, response.headers.get('retry-after'))
  }
  return body as T
}

/** Liveness probe for the AppBar indicator. Never throws. */
export async function checkHealth(signal?: AbortSignal): Promise<boolean> {
  try {
    const response = await fetch(`${apiBaseUrl}/healthz`, { signal })
    return response.ok
  } catch {
    return false
  }
}
