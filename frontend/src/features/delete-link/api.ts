import { apiFetch } from '../../shared/api/client'
import { ApiError } from '../../shared/api/errors'

// DELETE /api/links/{code} — see TinyLink.Api/Features/Links/DeleteLink.cs.
// The API answers 404 for unknown codes AND wrong tokens alike (idempotent,
// no token oracle), so we translate it into a single user-facing message.
export async function deleteLink(code: string, token: string): Promise<void> {
  try {
    await apiFetch<void>(`/api/links/${encodeURIComponent(code)}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` },
    })
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      throw new ApiError({
        status: 404,
        title: 'Link not found.',
        detail:
          'The code is unknown, or the deletion token is incorrect. ' +
          'Deletion is idempotent — an already-deleted link also reports this.',
      })
    }
    throw error
  }
}
