import { apiFetch } from '../../shared/api/client'

// GET /api/links/{code} — see TinyLink.Api/Features/Links/GetLinkStatus.cs.
// Read-only counterpart to the redirect: 200 when active, 410 when deleted or
// expired, 404 when unknown. Lets the redirect page render real gone/unknown
// states instead of navigating blind into a bare API status.
export interface LinkStatus {
  targetUrl: string
  expiresAt: string | null
}

export function getLinkStatus(code: string, signal?: AbortSignal): Promise<LinkStatus> {
  return apiFetch<LinkStatus>(`/api/links/${encodeURIComponent(code)}`, { signal })
}
