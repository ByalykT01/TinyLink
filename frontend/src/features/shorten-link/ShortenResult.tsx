import { CopyIconButton } from '../../shared/ui/CopyIconButton'
import { formatExpiry } from '../../shared/format'
import { Note } from '../../shared/ui/Notice'

export interface CreatedLink {
  shortCode: string
  shortUrl: string
  expiresAt: string | null
  deleteToken: string
}

/**
 * The shortened link as labeled rows — link, code, token — each with a copy
 * icon. No heading: appearing right under the form is context enough.
 * The token is shown once; there is no history.
 */
export function ShortenResult({ created }: { created: CreatedLink }) {
  return (
    <section className="section" aria-label="Shortened link" aria-live="polite">
      <div className="result-item">
        <p className="result-label">Short link</p>
        <div className="result-line">
          <span className="result-value">
            <a href={created.shortUrl} target="_blank" rel="noreferrer">
              {created.shortUrl}
            </a>
          </span>
          <CopyIconButton text={created.shortUrl} label="Copy short link" />
        </div>
      </div>
      <div className="result-item">
        <p className="result-label">Code</p>
        <div className="result-line">
          <span className="mono result-value">{created.shortCode}</span>
          <CopyIconButton text={created.shortCode} label="Copy short code" />
        </div>
      </div>
      <div className="result-item">
        <p className="result-label">Deletion token</p>
        <div className="result-line">
          <span className="mono result-value">{created.deleteToken}</span>
          <CopyIconButton text={created.deleteToken} label="Copy deletion token" />
        </div>
      </div>
      <Note>
        {formatExpiry(created.expiresAt)} Save the token — it&rsquo;s shown
        once and can&rsquo;t be recovered.
      </Note>
    </section>
  )
}
