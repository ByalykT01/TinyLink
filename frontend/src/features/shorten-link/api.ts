import { apiFetch } from '../../shared/api/client'

// POST /api/links — see TinyLink.Api/Features/Links/CreateLink.cs.
// `expiresAt` is omitted: the server applies its 7-day default.
export interface CreateLinkResponse {
  shortCode: string
  expiresAt: string | null
  deleteToken: string
}

export function postLink(url: string, signal?: AbortSignal): Promise<CreateLinkResponse> {
  return apiFetch<CreateLinkResponse>('/api/links', {
    method: 'POST',
    body: JSON.stringify({ url }),
    signal,
  })
}
