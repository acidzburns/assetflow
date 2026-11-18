import { Link, useSearchParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { api } from '../lib/api'
import { useAuth } from '../lib/useAuth'
import { STATUS_LABEL } from '../lib/format'
import type { LoanStatus } from '../lib/types'
import { LoanTable } from '../components/LoanTable'
import { EmptyState, ErrorMessage, Loading, PageHeader } from '../components/ui'

const TABS: { key: string; label: string; status?: LoanStatus; overdue?: boolean }[] = [
  { key: 'all', label: 'All' },
  { key: 'Requested', label: STATUS_LABEL.Requested, status: 'Requested' },
  { key: 'Approved', label: STATUS_LABEL.Approved, status: 'Approved' },
  { key: 'CheckedOut', label: STATUS_LABEL.CheckedOut, status: 'CheckedOut' },
  { key: 'overdue', label: 'Overdue', overdue: true },
  { key: 'Returned', label: STATUS_LABEL.Returned, status: 'Returned' },
]

export function LoansPage() {
  const { isAdmin } = useAuth()
  const [params, setParams] = useSearchParams()
  const tab = TABS.find((t) => t.key === params.get('tab')) ?? TABS[0]

  const { data, isLoading, error } = useQuery({
    queryKey: ['loans', { status: tab.status, overdue: tab.overdue }],
    queryFn: () => api.listLoans({ status: tab.status, overdue: tab.overdue }),
  })

  return (
    <>
      <PageHeader
        title={isAdmin ? 'Loans' : 'My loans'}
        subtitle={isAdmin ? 'Review requests, hand out equipment and receive returns.' : 'Equipment you have requested or borrowed.'}
      />

      <nav className="mb-5 flex flex-wrap gap-1 border-b border-line">
        {TABS.map((t) => (
          <button
            key={t.key}
            onClick={() => setParams(t.key === 'all' ? {} : { tab: t.key }, { replace: true })}
            className={`-mb-px border-b-2 px-3 py-2 text-sm font-medium transition ${
              t.key === tab.key ? 'border-accent text-accent' : 'border-transparent text-muted hover:text-ink'
            }`}
          >
            {t.label}
          </button>
        ))}
      </nav>

      <ErrorMessage error={error} />
      {isLoading && <Loading />}
      {data && data.length === 0 && (
        <EmptyState title="Nothing here">
          {isAdmin ? (
            'No loans in this list.'
          ) : (
            <>
              Find something to borrow on the <Link to="/assets" className="text-accent underline">Assets</Link> page.
            </>
          )}
        </EmptyState>
      )}
      {data && data.length > 0 && <LoanTable loans={data} showBorrower={isAdmin} />}
    </>
  )
}
