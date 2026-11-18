import type {
  Asset,
  AssetDetail,
  AssetInput,
  AuthResponse,
  Loan,
  LoanStatus,
  Maintenance,
  PagedResult,
  SummaryReport,
} from './types'

/**
 * Small wrapper around fetch for talking to the C# API.
 * - adds the login token to every request
 * - turns API errors (RFC 7807 problem details) into readable messages
 */

// In development Vite forwards /api to the API. In production set VITE_API_URL.
const BASE_URL = import.meta.env.VITE_API_URL ?? ''
const TOKEN_KEY = 'assetflow.auth'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

export function getStoredAuth(): AuthResponse | null {
  try {
    const raw = localStorage.getItem(TOKEN_KEY)
    if (!raw) return null
    const auth = JSON.parse(raw) as AuthResponse
    return new Date(auth.expiresAt) > new Date() ? auth : null
  } catch {
    return null
  }
}

export function storeAuth(auth: AuthResponse | null) {
  try {
    if (auth) localStorage.setItem(TOKEN_KEY, JSON.stringify(auth))
    else localStorage.removeItem(TOKEN_KEY)
  } catch {
    // storage can be unavailable (private mode); the session just won't survive a reload
  }
}

/** Called when the API says the token is no longer valid. Set by the auth provider. */
let onUnauthorized: () => void = () => {}
export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler
}

/** Build a readable message from an API error body. */
export function errorMessage(status: number, body: unknown): string {
  if (body && typeof body === 'object') {
    const problem = body as { detail?: string; title?: string; errors?: Record<string, string[]> }
    if (problem.errors) {
      // Validation errors: { errors: { Purpose: ["The Purpose field is required."] } }
      const first = Object.values(problem.errors).flat()[0]
      if (first) return first
    }
    if (problem.detail) return problem.detail
    if (problem.title) return problem.title
  }
  if (status === 0) return 'Cannot reach the server. Is the API running?'
  return `Request failed (HTTP ${status})`
}

export async function request<T>(path: string, options: { method?: string; body?: unknown } = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  const auth = getStoredAuth()
  if (auth) headers.Authorization = `Bearer ${auth.token}`
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'

  let response: Response
  try {
    response = await fetch(`${BASE_URL}${path}`, {
      method: options.method ?? 'GET',
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
    })
  } catch {
    throw new ApiError(0, errorMessage(0, null))
  }

  if (response.status === 401 && auth) onUnauthorized()

  const text = await response.text()
  const data = text ? JSON.parse(text) : undefined
  if (!response.ok) throw new ApiError(response.status, errorMessage(response.status, data))
  return data as T
}

const post = <T>(path: string, body: unknown = {}) => request<T>(path, { method: 'POST', body })

function query(params: Record<string, string | number | boolean | undefined | null>) {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '' && value !== false) search.set(key, String(value))
  }
  const s = search.toString()
  return s ? `?${s}` : ''
}

// ── Endpoints ─────────────────────────────────────────────────────────────
export const api = {
  login: (email: string, password: string) => post<AuthResponse>('/api/auth/login', { email, password }),
  register: (name: string, email: string, password: string, department: string | null) =>
    post<AuthResponse>('/api/auth/register', { name, email, password, department }),

  searchAssets: (p: { search?: string; status?: string; category?: string; page?: number; pageSize?: number }) =>
    request<PagedResult<Asset>>(`/api/assets${query(p)}`),
  getAsset: (id: number) => request<AssetDetail>(`/api/assets/${id}`),
  createAsset: (input: AssetInput) => post<Asset>('/api/assets', input),
  updateAsset: (id: number, input: AssetInput) => request<Asset>(`/api/assets/${id}`, { method: 'PUT', body: input }),
  retireAsset: (id: number) => post<void>(`/api/assets/${id}/retire`),
  sendToRepair: (id: number, issue: string) => post<Maintenance>(`/api/assets/${id}/repairs`, { issue }),
  completeRepair: (maintenanceId: number, resolution: string | null, cost: number | null) =>
    post<Maintenance>(`/api/assets/repairs/${maintenanceId}/complete`, { resolution, cost }),

  listLoans: (p: { status?: LoanStatus; overdue?: boolean }) => request<Loan[]>(`/api/loans${query(p)}`),
  requestLoan: (assetId: number, startDate: string, dueDate: string, purpose: string) =>
    post<Loan>('/api/loans', { assetId, startDate, dueDate, purpose }),
  approveLoan: (id: number, note: string | null) => post<Loan>(`/api/loans/${id}/approve`, { note }),
  rejectLoan: (id: number, note: string) => post<Loan>(`/api/loans/${id}/reject`, { note }),
  cancelLoan: (id: number) => post<Loan>(`/api/loans/${id}/cancel`),
  checkOutLoan: (id: number) => post<Loan>(`/api/loans/${id}/checkout`),
  returnLoan: (id: number, condition: string | null) => post<Loan>(`/api/loans/${id}/return`, { condition }),

  summary: () => request<SummaryReport>('/api/reports/summary'),
}
