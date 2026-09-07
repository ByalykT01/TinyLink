/**
 * Absolute, unambiguous timestamp — locale formatting is genuinely ambiguous
 * outside the US date-order convention, so data stays in ISO shape.
 */
export function formatExpiry(expiresAt: string | null): string {
  if (expiresAt === null) return 'No expiry set.'
  const date = new Date(expiresAt)
  if (Number.isNaN(date.getTime())) return expiresAt
  const pad = (value: number) => String(value).padStart(2, '0')
  return (
    `Expires ${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
    ` ${pad(date.getHours())}:${pad(date.getMinutes())}`
  )
}
