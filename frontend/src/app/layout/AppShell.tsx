import { Outlet } from 'react-router-dom'
import { HealthStatus } from '../../shared/ui/HealthStatus'

/** Wordmark plus status in normal flow; one left-aligned column below. */
export function AppShell() {
  return (
    <div className="shell">
      <div className="shell-inner">
        <header className="masthead">
          <span className="wordmark">tinylink</span>
          <HealthStatus />
        </header>
        <main>
          <Outlet />
        </main>
        <footer className="footer">
          Links expire after 7 days unless deleted first.
        </footer>
      </div>
    </div>
  )
}
