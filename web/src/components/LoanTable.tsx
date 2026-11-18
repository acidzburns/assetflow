import { useState } from 'react'
import { Link } from 'react-router'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../lib/api'
import { useAuth } from '../lib/useAuth'
import { formatRange } from '../lib/format'
import { ACTION_LABEL, availableLoanActions, type LoanAction } from '../lib/loanActions'
import type { Loan } from '../lib/types'
import { Button, ErrorMessage, Field, Modal, StatusBadge, Textarea } from './ui'

/** A table of loans with the right action buttons for the logged-in user. */
export function LoanTable({
  loans,
  showBorrower = true,
  showAsset = true,
}: {
  loans: Loan[]
  showBorrower?: boolean
  showAsset?: boolean
}) {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [dialog, setDialog] = useState<{ loan: Loan; action: 'reject' | 'return' } | null>(null)
  const [note, setNote] = useState('')

  const mutation = useMutation({
    mutationFn: ({ loan, action, text }: { loan: Loan; action: LoanAction; text?: string }) => {
      switch (action) {
        case 'approve':
          return api.approveLoan(loan.id, null)
        case 'reject':
          return api.rejectLoan(loan.id, text ?? '')
        case 'checkout':
          return api.checkOutLoan(loan.id)
        case 'return':
          return api.returnLoan(loan.id, text || null)
        case 'cancel':
          return api.cancelLoan(loan.id)
      }
    },
    onSuccess: () => {
      setDialog(null)
      setNote('')
      // Loans, assets and the dashboard all change together, so refresh them all
      queryClient.invalidateQueries()
    },
  })

  if (!user) return null

  const run = (loan: Loan, action: LoanAction) => {
    if (action === 'reject' || action === 'return') {
      mutation.reset()
      setNote('')
      setDialog({ loan, action })
    } else {
      mutation.mutate({ loan, action })
    }
  }

  return (
    <>
      {!dialog && <ErrorMessage error={mutation.error} />}
      <div className="overflow-x-auto rounded-xl border border-line">
        <table className="w-full text-sm">
          <thead className="bg-card text-left text-xs font-medium uppercase tracking-wide text-muted">
            <tr>
              {showAsset && <th className="px-4 py-2.5">Asset</th>}
              {showBorrower && <th className="px-4 py-2.5">Borrower</th>}
              <th className="px-4 py-2.5">Dates</th>
              <th className="px-4 py-2.5">Purpose</th>
              <th className="px-4 py-2.5">Status</th>
              <th className="px-4 py-2.5 text-right">
                <span className="sr-only">Actions</span>
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-line">
            {loans.map((loan) => {
              const actions = availableLoanActions(loan, user)
              return (
                <tr key={loan.id} className="align-top">
                  {showAsset && (
                  <td className="px-4 py-3">
                    <Link to={`/assets/${loan.assetId}`} className="font-mono text-xs font-medium text-accent hover:underline">
                      {loan.assetTag}
                    </Link>
                    <p className="text-muted">{loan.assetName}</p>
                  </td>
                  )}
                  {showBorrower && <td className="px-4 py-3">{loan.borrowerName}</td>}
                  <td className="whitespace-nowrap px-4 py-3">
                    {formatRange(loan.startDate, loan.dueDate)}
                  </td>
                  <td className="min-w-40 max-w-64 px-4 py-3 text-muted">
                    {loan.purpose}
                    {loan.reviewNote && <p className="mt-1 text-xs italic">“{loan.reviewNote}” — {loan.reviewedBy}</p>}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex flex-col items-start gap-1">
                      <StatusBadge status={loan.status} />
                      {loan.isOverdue && <StatusBadge status={loan.status} overdue />}
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap justify-end gap-1.5">
                      {actions.map((action) => (
                        <Button
                          key={action}
                          size="sm"
                          variant={action === 'reject' || action === 'cancel' ? 'danger' : action === 'approve' ? 'primary' : 'secondary'}
                          disabled={mutation.isPending}
                          onClick={() => run(loan, action)}
                        >
                          {ACTION_LABEL[action]}
                        </Button>
                      ))}
                    </div>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      {dialog && (
        <Modal
          title={dialog.action === 'reject' ? `Reject loan for ${dialog.loan.assetTag}` : `Return ${dialog.loan.assetTag}`}
          onClose={() => setDialog(null)}
        >
          <form
            className="space-y-4"
            onSubmit={(e) => {
              e.preventDefault()
              mutation.mutate({ loan: dialog.loan, action: dialog.action, text: note.trim() })
            }}
          >
            <Field
              label={dialog.action === 'reject' ? 'Reason' : 'Condition on return'}
              hint={dialog.action === 'reject' ? 'The borrower will see this.' : 'Optional, e.g. "Good" or "Scratch on lid".'}
            >
              <Textarea value={note} onChange={(e) => setNote(e.target.value)} required={dialog.action === 'reject'} maxLength={300} />
            </Field>
            <ErrorMessage error={mutation.error} />
            <div className="flex justify-end gap-2">
              <Button type="button" variant="secondary" onClick={() => setDialog(null)}>
                Cancel
              </Button>
              <Button type="submit" variant={dialog.action === 'reject' ? 'danger' : 'primary'} disabled={mutation.isPending}>
                {dialog.action === 'reject' ? 'Reject loan' : 'Confirm return'}
              </Button>
            </div>
          </form>
        </Modal>
      )}
    </>
  )
}
