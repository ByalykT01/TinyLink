import { useState } from 'react'
import type { FormEvent } from 'react'
import { SHORT_CODE_PATTERN } from '../../shared/config'
import { ApiError } from '../../shared/api/errors'
import { ErrorNotice } from '../../shared/ui/Notice'
import { deleteLink } from './api'

interface DeleteCardProps {
  initialCode?: string
  initialToken?: string
}

function validateCode(raw: string): string | null {
  if (raw.trim().length === 0) return 'Short code is required.'
  if (!SHORT_CODE_PATTERN.test(raw.trim())) {
    return 'Must be the 7-character code (letters and digits).'
  }
  return null
}

/** Bearer-token deletion. Prefilled from a just-created link when available. */
export function DeleteCard({ initialCode, initialToken }: DeleteCardProps) {
  const [code, setCode] = useState(initialCode ?? '')
  const [token, setToken] = useState(initialToken ?? '')
  const [showToken, setShowToken] = useState(false)
  const [codeError, setCodeError] = useState<string | null>(null)
  const [tokenError, setTokenError] = useState<string | null>(null)
  const [pending, setPending] = useState(false)
  const [deleted, setDeleted] = useState(false)
  const [error, setError] = useState<ApiError | null>(null)
  // Note: fresh initial values arrive via `key` remount from the parent,
  // so no state syncing in effects is needed here.

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const codeProblem = validateCode(code)
    const tokenProblem = token.trim().length === 0 ? 'Deletion token is required.' : null
    setCodeError(codeProblem)
    setTokenError(tokenProblem)
    if (codeProblem !== null || tokenProblem !== null) return

    setPending(true)
    setError(null)
    setDeleted(false)
    try {
      await deleteLink(code.trim(), token.trim())
      setDeleted(true)
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
    <section className="section" aria-labelledby="delete-heading">
      <h2 id="delete-heading" className="section-title">
        Delete a link
      </h2>
      <p>
        Unknown codes and incorrect tokens give the same error, so check both
        before trying again.
      </p>
      <form onSubmit={handleSubmit} noValidate>
        <div className="field-row">
          <div className="field-code">
            <label className="field-label" htmlFor="delete-code">
              Code
            </label>
            <input
              id="delete-code"
              className="field-input mono"
              placeholder="aB3dE9x"
              value={code}
              onChange={(event) => {
                setCode(event.target.value)
                if (codeError !== null) setCodeError(null)
                if (deleted) setDeleted(false)
              }}
              maxLength={7}
              autoComplete="off"
              aria-invalid={codeError !== null}
            />
            {codeError !== null && (
              <p className="field-error" role="alert">
                {codeError}
              </p>
            )}
          </div>
          <div className="field-grow">
            <label className="field-label" htmlFor="delete-token">
              Token
            </label>
            <input
              id="delete-token"
              className="field-input mono"
              type={showToken ? 'text' : 'password'}
              value={token}
              onChange={(event) => {
                setToken(event.target.value)
                if (tokenError !== null) setTokenError(null)
                if (deleted) setDeleted(false)
              }}
              autoComplete="off"
              aria-invalid={tokenError !== null}
            />
            {tokenError !== null && (
              <p className="field-error" role="alert">
                {tokenError}
              </p>
            )}
          </div>
        </div>
        <div className="btn-row btn-row-spread">
          <button
            type="button"
            className="text-btn"
            onClick={() => setShowToken((visible) => !visible)}
            aria-pressed={showToken}
          >
            {showToken ? 'Hide' : 'Show'}
          </button>
          <button type="submit" className="btn-danger" disabled={pending || deleted}>
            {pending ? 'Deleting…' : deleted ? 'Deleted' : 'Delete link'}
          </button>
        </div>
      </form>
      <ErrorNotice error={error} />
    </section>
  )
}
