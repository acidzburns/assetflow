import type { Loan, User } from './types'

export type LoanAction = 'approve' | 'reject' | 'checkout' | 'return' | 'cancel'

/**
 * Which buttons to show for a loan. This mirrors the rules in the C# Loan class,
 * so users only see actions the API will accept. The API still checks everything.
 */
export function availableLoanActions(loan: Loan, user: User): LoanAction[] {
  const isAdmin = user.role === 'Admin'
  const isBorrower = loan.borrowerId === user.id
  const actions: LoanAction[] = []

  if (isAdmin && loan.status === 'Requested') actions.push('approve', 'reject')
  if (isAdmin && loan.status === 'Approved') actions.push('checkout')
  if (isAdmin && loan.status === 'CheckedOut') actions.push('return')
  if ((isBorrower || isAdmin) && (loan.status === 'Requested' || loan.status === 'Approved')) actions.push('cancel')

  return actions
}

export const ACTION_LABEL: Record<LoanAction, string> = {
  approve: 'Approve',
  reject: 'Reject',
  checkout: 'Check out',
  return: 'Mark returned',
  cancel: 'Cancel',
}
