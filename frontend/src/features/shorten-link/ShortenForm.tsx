import { useState } from 'react'
import type { FormEvent } from 'react'
import { MAX_URL_LENGTH, normalizeUrl, validateUrl } from '../../shared/config'
import type { ApiError } from '../../shared/api/errors'
import { ErrorNotice } from '../../shared/ui/Notice'

interface ShortenFormProps {
  pending: boolean
  serverError: ApiError | null
  onSubmit: (url: string) => void
}

export function ShortenForm({ pending, serverError, onSubmit }: ShortenFormProps) {
  const [url, setUrl] = useState('')
  const [clientError, setClientError] = useState<string | null>(null)

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    const normalized = normalizeUrl(url)
    setUrl(normalized)
    const problem = validateUrl(normalized)
    setClientError(problem)
    if (problem === null) onSubmit(normalized)
  }

  return (
    <section className="section" aria-labelledby="shorten-heading">
      <h1 id="shorten-heading" className="section-title">
        Shorten a link
      </h1>
      <form onSubmit={handleSubmit} noValidate>
        <label className="field-label" htmlFor="long-url">
          Long URL
        </label>
        <input
          id="long-url"
          className="field-input"
          type="url"
          placeholder="https://example.com/article"
          autoFocus
          autoComplete="url"
          value={url}
          onChange={(event) => {
            setUrl(event.target.value)
            if (clientError !== null) setClientError(null)
          }}
          aria-describedby="long-url-help"
          aria-invalid={clientError !== null}
        />
        <div className="field-meta">
          <span id="long-url-help">https:// is added if you leave it out</span>
          <span className="mono small" aria-live="polite">
            {url.length} / {MAX_URL_LENGTH}
          </span>
        </div>
        {clientError !== null && (
          <p className="field-error" role="alert">
            {clientError}
          </p>
        )}
        <div className="btn-row btn-row-end">
          <button
            type="submit"
            className="btn-primary"
            disabled={pending || url.trim().length === 0}
          >
            {pending ? 'Shortening…' : 'Shorten link'}
          </button>
        </div>
      </form>
      <ErrorNotice error={serverError} />
    </section>
  )
}
