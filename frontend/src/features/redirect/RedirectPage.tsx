import { useEffect, useState } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { SHORT_CODE_PATTERN, shortUrlFor } from '../../shared/config'
import { ApiError } from '../../shared/api/errors'
import { formatExpiry } from '../../shared/format'
import { CopyIconButton } from '../../shared/ui/CopyIconButton'
import { ErrorNotice, Note } from '../../shared/ui/Notice'
import { getLinkStatus } from './api'

type PageState =
  | { kind: 'invalid' }
  | { kind: 'checking' }
  | { kind: 'active'; expiresAt: string | null }
  | { kind: 'gone' }
  | { kind: 'unknown' }
  | { kind: 'offline' }

// The redirect itself (GET /{code}) is a 302 browsers follow transparently,
// so this page first asks GET /api/links/{code} whether the code is active,
// gone, or unknown — and renders a real page for each instead of navigating
// blind into a bare API status.
export function RedirectPage() {
  const { code = '' } = useParams()
  const [state, setState] = useState<PageState>(() =>
    SHORT_CODE_PATTERN.test(code) ? { kind: 'checking' } : { kind: 'invalid' },
  )

  useEffect(() => {
    if (!SHORT_CODE_PATTERN.test(code)) return
    const controller = new AbortController()
    let mounted = true
    getLinkStatus(code, controller.signal).then(
      (status) => {
        if (mounted) setState({ kind: 'active', expiresAt: status.expiresAt })
      },
      (failure: unknown) => {
        if (!mounted) return
        if (failure instanceof ApiError && failure.status === 404) {
          setState({ kind: 'unknown' })
        } else if (failure instanceof ApiError && failure.status === 410) {
          setState({ kind: 'gone' })
        } else {
          setState({ kind: 'offline' })
        }
      },
    )
    return () => {
      mounted = false
      controller.abort()
    }
  }, [code])

  if (state.kind === 'invalid') {
    return (
      <section className="section" aria-labelledby="invalid-heading">
        <h1 id="invalid-heading" className="section-title">
          Not a short link.
        </h1>
        <ErrorNotice
          error={
            new ApiError({
              status: 404,
              title: `“${code}” is not a valid short code.`,
              detail: 'Codes are 7 letters and digits.',
            })
          }
        />
        <p>
          <RouterLink to="/">Shorten a URL</RouterLink>
        </p>
      </section>
    )
  }

  if (state.kind === 'gone') {
    return (
      <section className="section" aria-labelledby="gone-heading">
        <h1 id="gone-heading" className="section-title">
          This link is gone.
        </h1>
        <p className="muted">It was deleted or it expired.</p>
        <p>
          <RouterLink to="/">Shorten a URL</RouterLink>
        </p>
      </section>
    )
  }

  if (state.kind === 'unknown') {
    return (
      <section className="section" aria-labelledby="unknown-heading">
        <h1 id="unknown-heading" className="section-title">
          No link with this code.
        </h1>
        <p className="muted">Check the code and try again.</p>
        <p>
          <RouterLink to="/">Shorten a URL</RouterLink>
        </p>
      </section>
    )
  }

  if (state.kind === 'checking') {
    return (
      <section className="section" aria-label="Short link">
        <p className="note" aria-live="polite">
          Checking link…
        </p>
      </section>
    )
  }

  const shortUrl = shortUrlFor(code)
  return (
    <section className="section" aria-label="Short link">
      <div className="result-line">
        <span className="result-value">
          <a href={shortUrl}>{shortUrl}</a>
        </span>
        <CopyIconButton text={shortUrl} label="Copy short link" />
      </div>
      {state.kind === 'active' && (
        <Note>{formatExpiry(state.expiresAt)}</Note>
      )}
      <p>
        <RouterLink className="text-btn" to="/">
          Home
        </RouterLink>
      </p>
    </section>
  )
}
