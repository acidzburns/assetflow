import { describe, expect, it } from 'vitest'
import { availableLoanActions } from '../lib/loanActions'
import { errorMessage } from '../lib/api'
import { formatRange, loanLengthDays } from '../lib/format'
import type { Loan, LoanStatus, User } from '../lib/types'

const staff: User = { id: 2, name: 'Daniel Lim', email: 'staff@assetflow.dev', role: 'Staff', department: null }
const otherStaff: User = { ...staff, id: 3, name: 'Someone Else' }
const admin: User = { id: 1, name: 'Nadia Rahman', email: 'admin@assetflow.dev', role: 'Admin', department: null }

const loan = (status: LoanStatus): Loan => ({
  id: 10, assetId: 1, assetTag: 'LAP-0001', assetName: 'Laptop', borrowerId: staff.id, borrowerName: staff.name,
  purpose: 'x', startDate: '2026-11-18', dueDate: '2026-11-20', status, isOverdue: false,
  requestedAt: '', reviewedBy: null, reviewNote: null, checkedOutAt: null, returnedAt: null,
})

describe('availableLoanActions', () => {
  it.each<[LoanStatus, string[]]>([
    ['Requested', ['approve', 'reject', 'cancel']],
    ['Approved', ['checkout', 'cancel']],
    ['CheckedOut', ['return']],
    ['Returned', []],
    ['Rejected', []],
  ])('admin, %s loan → %j', (status, expected) => {
    expect(availableLoanActions(loan(status), admin)).toEqual(expected)
  })

  it('the borrower can only cancel, and only before check-out', () => {
    expect(availableLoanActions(loan('Requested'), staff)).toEqual(['cancel'])
    expect(availableLoanActions(loan('Approved'), staff)).toEqual(['cancel'])
    expect(availableLoanActions(loan('CheckedOut'), staff)).toEqual([])
  })

  it("other staff get no actions on someone else's loan", () => {
    expect(availableLoanActions(loan('Requested'), otherStaff)).toEqual([])
  })
})

describe('errorMessage', () => {
  it('uses the problem "detail" from the API', () => {
    expect(errorMessage(409, { title: 'Conflict', detail: 'LAP-0001 is already booked.' })).toBe('LAP-0001 is already booked.')
  })

  it('shows the first validation error', () => {
    expect(errorMessage(400, { title: 'Validation', errors: { Purpose: ['The Purpose field is required.'] } })).toBe(
      'The Purpose field is required.',
    )
  })

  it('explains when the server cannot be reached', () => {
    expect(errorMessage(0, null)).toMatch(/Cannot reach the server/)
  })
})

describe('loanLengthDays', () => {
  it('counts both the first and last day', () => {
    expect(loanLengthDays('2026-11-18', '2026-11-18')).toBe(1)
    expect(loanLengthDays('2026-11-18', '2026-12-17')).toBe(30)
  })
})

describe('formatRange', () => {
  it('leaves out the year for dates in the current year', () => {
    const year = new Date().getFullYear()
    expect(formatRange(`${year}-11-18`, `${year}-11-20`)).toBe('18 Nov → 20 Nov')
    expect(formatRange('2020-01-05', '2020-01-06')).toBe('5 Jan 2020 → 6 Jan 2020')
  })
})
