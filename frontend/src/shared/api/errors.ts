// Typed wrapper around the API's RFC 7807 (application/problem+json) errors.

export type FieldErrorMap = Record<string, string[]>

/** Backend error contract, covering ValidationProblem, Problem, and 429s. */
export class ApiError extends Error {
  status: number
  title: string
  detail: string | undefined
  fields: FieldErrorMap | undefined
  retryAfterSeconds: number | undefined
  traceId: string | undefined

  constructor(init: {
    status: number
    title: string
    detail?: string
    fields?: FieldErrorMap
    retryAfterSeconds?: number
    traceId?: string
  }) {
    super(init.detail ? `${init.title} ${init.detail}` : init.title)
    this.name = 'ApiError'
    this.status = init.status
    this.title = init.title
    this.detail = init.detail
    this.fields = init.fields
    this.retryAfterSeconds = init.retryAfterSeconds
    this.traceId = init.traceId
  }

  get isRateLimited(): boolean {
    return this.status === 429
  }

  get isValidation(): boolean {
    return this.status === 400 && this.fields !== undefined
  }
}
