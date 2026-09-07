import { useEffect, useState } from 'react'
import { checkHealth } from '../api/client'

type Health = 'unknown' | 'up' | 'down'

/** API reachability as small gray text. Silent on failure by design. */
export function HealthStatus() {
  const [health, setHealth] = useState<Health>('unknown')

  useEffect(() => {
    const controller = new AbortController()
    let mounted = true
    const probe = async () => {
      const ok = await checkHealth(controller.signal)
      if (mounted) setHealth(ok ? 'up' : 'down')
    }
    void probe()
    const timer = window.setInterval(probe, 30_000)
    return () => {
      mounted = false
      controller.abort()
      window.clearInterval(timer)
    }
  }, [])

  const label =
    health === 'up'
      ? 'API connected'
      : health === 'down'
        ? 'API unreachable'
        : 'Checking API…'

  return (
    <p className="status" role="status">
      {label}
    </p>
  )
}
