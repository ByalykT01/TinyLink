import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiFetch } from './client'
import { ApiError } from './errors'

function jsonResponse(status: number, body: unknown, headers: Record<string, string> = {}) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/problem+json', ...headers },
  })
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('apiFetch', () => {
  it('returns parsed JSON on success', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      jsonResponse(201, { shortCode: 'FKVY4mF' }),
    ))
    await expect(apiFetch('/api/links', { method: 'POST' })).resolves.toEqual({
      shortCode: 'FKVY4mF',
    })
  })

  it('maps ValidationProblem field errors', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      jsonResponse(400, {
        title: 'One or more validation errors occurred.',
        errors: { url: ['Must use the http or https scheme.'] },
      }),
    ))
    const failure = await apiFetch('/api/links', { method: 'POST' }).catch(
      (error: unknown) => error,
    )
    expect(failure).toBeInstanceOf(ApiError)
    const apiError = failure as ApiError
    expect(apiError.status).toBe(400)
    expect(apiError.fields).toEqual({ url: ['Must use the http or https scheme.'] })
    expect(apiError.isValidation).toBe(true)
  })

  it('maps 429 with Retry-After and traceId', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(
      jsonResponse(
        429,
        {
          title: 'Too many requests.',
          detail: 'Try again shortly.',
          extensions: { traceId: 'abc123' },
        },
        { 'retry-after': '42' },
      ),
    ))
    const failure = await apiFetch('/api/links', { method: 'POST' }).catch(
      (error: unknown) => error,
    )
    const apiError = failure as ApiError
    expect(apiError.isRateLimited).toBe(true)
    expect(apiError.retryAfterSeconds).toBe(42)
    expect(apiError.traceId).toBe('abc123')
  })

  it('maps network failure to status 0', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))
    const failure = await apiFetch('/api/links').catch((error: unknown) => error)
    expect(failure).toBeInstanceOf(ApiError)
    expect((failure as ApiError).status).toBe(0)
  })
})
