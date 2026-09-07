import { describe, expect, it } from 'vitest'
import { normalizeUrl, validateUrl } from './config'

describe('normalizeUrl', () => {
  it('prepends https:// when no scheme is present', () => {
    expect(normalizeUrl('example.com/article')).toBe('https://example.com/article')
    expect(normalizeUrl('  example.com  ')).toBe('https://example.com')
  })

  it('leaves input with a scheme alone', () => {
    expect(normalizeUrl('https://example.com')).toBe('https://example.com')
    expect(normalizeUrl('http://example.com')).toBe('http://example.com')
    expect(normalizeUrl('ftp://example.com/file')).toBe('ftp://example.com/file')
  })

  it('leaves empty input alone', () => {
    expect(normalizeUrl('')).toBe('')
  })

  it('pairs with validateUrl for schemeless input', () => {
    expect(validateUrl(normalizeUrl('example.com/article'))).toBeNull()
  })
})

describe('validateUrl', () => {
  it('accepts https and http URLs', () => {
    expect(validateUrl('https://example.com/hello')).toBeNull()
    expect(validateUrl('http://api.localhost/FKVY4mF')).toBeNull()
  })

  it('rejects empty input', () => {
    expect(validateUrl('')).toBe('URL is required.')
    expect(validateUrl('   ')).toBe('URL is required.')
  })

  it('rejects non-absolute URLs', () => {
    expect(validateUrl('not-a-url')).toBe('Must be an absolute URL.')
    expect(validateUrl('/relative/path')).toBe('Must be an absolute URL.')
  })

  it('rejects non-http schemes', () => {
    expect(validateUrl('ftp://example.com/file')).toBe(
      'Must use the http or https scheme.',
    )
  })

  it('rejects embedded credentials', () => {
    expect(validateUrl('https://user:pass@example.com/')).toBe(
      'Must not bear embedded credentials.',
    )
  })

  it('rejects over-long URLs', () => {
    expect(validateUrl(`https://example.com/${'a'.repeat(2000)}`)).toBe(
      'Must be at most 2000 characters in length.',
    )
  })
})
