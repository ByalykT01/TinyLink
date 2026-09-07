import type { ReactNode } from 'react'
import { ApiError } from '../api/errors'

function formatRetryDelay(seconds: number): string {
  return seconds <= 1 ? '1 second' : `${seconds} seconds`
}

/** Small gray inline note for informational text (expiry hints, confirmations). */
export function Note({ children }: { children: ReactNode }) {
  return <p className="note">{children}</p>
}

/**
 * API errors as small red text. Field errors list beneath; rate-limit delay
 * and trace id render as small gray inline companions to what they describe.
 */
export function ErrorNotice({ error }: { error: ApiError | null }) {
  if (error === null) return null

  const fieldEntries =
    error.fields !== undefined ? Object.entries(error.fields) : []

  return (
    <div role="alert">
      <p className="error-text">
        {error.title}
        {error.detail !== undefined ? ` ${error.detail}` : ''}
      </p>
      {fieldEntries.length > 0 && (
        <ul className="error-list">
          {fieldEntries.map(([field, messages]) =>
            messages.map((message) => (
              <li key={`${field}:${message}`}>
                {field}: {message}
              </li>
            )),
          )}
        </ul>
      )}
      {error.isRateLimited && (
        <p className="note">
          {error.retryAfterSeconds !== undefined
            ? `Try again in ${formatRetryDelay(error.retryAfterSeconds)}.`
            : 'Try again shortly.'}
        </p>
      )}
      {error.traceId !== undefined && (
        <p className="trace">traceId: {error.traceId}</p>
      )}
    </div>
  )
}
