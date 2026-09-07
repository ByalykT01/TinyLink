import { useState } from 'react'
import { ApiError } from '../../shared/api/errors'
import { shortUrlFor } from '../../shared/config'
import { DeleteCard } from '../delete-link/DeleteCard'
import { postLink } from './api'
import { ShortenForm } from './ShortenForm'
import { ShortenResult, type CreatedLink } from './ShortenResult'

/** Home page: shorten, result, and delete sections divided by hairlines. */
export function ShortenPage() {
  const [pending, setPending] = useState(false)
  const [created, setCreated] = useState<CreatedLink | null>(null)
  const [error, setError] = useState<ApiError | null>(null)

  const handleSubmit = async (url: string) => {
    setPending(true)
    setError(null)
    try {
      const response = await postLink(url)
      setCreated({
        shortCode: response.shortCode,
        shortUrl: shortUrlFor(response.shortCode),
        expiresAt: response.expiresAt,
        deleteToken: response.deleteToken,
      })
    } catch (failure) {
      setError(failure instanceof ApiError ? failure : new ApiError({
        status: 0,
        title: 'Something went wrong.',
        detail: failure instanceof Error ? failure.message : undefined,
      }))
    } finally {
      setPending(false)
    }
  }

  return (
    <div className="stack">
      <ShortenForm pending={pending} serverError={error} onSubmit={handleSubmit} />
      {created !== null && (
        <ShortenResult key={created.shortCode} created={created} />
      )}
      {/* Remount on each new link so the form picks up fresh initial values. */}
      <DeleteCard
        key={created?.shortCode ?? 'empty'}
        initialCode={created?.shortCode}
        initialToken={created?.deleteToken}
      />
    </div>
  )
}
