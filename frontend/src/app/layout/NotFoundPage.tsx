import { Link as RouterLink } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <section className="section" aria-labelledby="not-found-heading">
      <h1 id="not-found-heading" className="section-title">
        Page not found.
      </h1>
      <p>
        Short links look like <code className="mono">/aB3dE9x</code> — seven
        letters and digits.
      </p>
      <p>
        <RouterLink to="/">Shorten a URL</RouterLink>
      </p>
    </section>
  )
}
