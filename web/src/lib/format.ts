import type { AssetStatus, LoanStatus } from './types'

/** "2026-11-18" → "18 Nov 2026" */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '—'
  const [y, m, d] = value.slice(0, 10).split('-').map(Number)
  return new Date(y, m - 1, d).toLocaleDateString('en-MY', { day: 'numeric', month: 'short', year: 'numeric' })
}

/** Short date range: "18 Nov → 20 Nov" this year, with the year added otherwise. */
export function formatRange(start: string, end: string): string {
  const thisYear = String(new Date().getFullYear())
  const short = (value: string) => {
    const [y, m, d] = value.slice(0, 10).split('-').map(Number)
    const options: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'short' }
    if (value.slice(0, 4) !== thisYear) options.year = 'numeric'
    return new Date(y, m - 1, d).toLocaleDateString('en-MY', options)
  }
  return `${short(start)} → ${short(end)}`
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) return '—'
  return new Date(value).toLocaleString('en-MY', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })
}

export function formatMoney(value: number | null | undefined): string {
  if (value === null || value === undefined) return '—'
  return `RM ${value.toLocaleString('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

/** Today's date in the user's time zone, as yyyy-mm-dd (for <input type="date">). */
export function todayIso(offsetDays = 0): string {
  const d = new Date()
  d.setDate(d.getDate() + offsetDays)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

/** Number of days from start to due, counting both days. */
export function loanLengthDays(start: string, due: string): number {
  const ms = new Date(`${due}T00:00:00`).getTime() - new Date(`${start}T00:00:00`).getTime()
  return Math.round(ms / 86_400_000) + 1
}

export const STATUS_LABEL: Record<AssetStatus | LoanStatus, string> = {
  Available: 'Available',
  OnLoan: 'On loan',
  InRepair: 'In repair',
  Retired: 'Retired',
  Requested: 'Requested',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Cancelled: 'Cancelled',
  CheckedOut: 'Checked out',
  Returned: 'Returned',
}
