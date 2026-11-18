import { Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { api } from '../lib/api'
import { formatMoney, STATUS_LABEL } from '../lib/format'
import { ASSET_STATUSES } from '../lib/types'
import { LoanTable } from '../components/LoanTable'
import { Card, EmptyState, ErrorMessage, Loading, PageHeader } from '../components/ui'

const BAR_COLOUR = {
  Available: 'bg-emerald-500',
  OnLoan: 'bg-sky-500',
  InRepair: 'bg-amber-400',
  Retired: 'bg-zinc-300',
} as const

/** Admin overview: key numbers, what needs attention, and asset usage. */
export function DashboardPage() {
  const summary = useQuery({ queryKey: ['summary'], queryFn: api.summary })
  const pending = useQuery({ queryKey: ['loans', { status: 'Requested' }], queryFn: () => api.listLoans({ status: 'Requested' }) })
  const overdue = useQuery({ queryKey: ['loans', { overdue: true }], queryFn: () => api.listLoans({ overdue: true }) })

  if (summary.isLoading) return <Loading />
  if (summary.error || !summary.data) return <ErrorMessage error={summary.error} />
  const s = summary.data

  const counts = Object.fromEntries(s.assetsByStatus.map((x) => [x.status, x.count])) as Record<string, number>
  const totalAssets = s.assetsByStatus.reduce((sum, x) => sum + x.count, 0)

  const tiles = [
    { label: 'Pending requests', value: s.pendingRequests, to: '/loans?tab=Requested', alert: s.pendingRequests > 0 },
    { label: 'On loan now', value: s.activeLoans, to: '/loans?tab=CheckedOut' },
    { label: 'Overdue', value: s.overdueLoans, to: '/loans?tab=overdue', alert: s.overdueLoans > 0 },
    { label: 'Repair costs this year', value: formatMoney(s.repairCostThisYear) },
  ]

  return (
    <>
      <PageHeader title="Dashboard" subtitle="What needs your attention today." />

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        {tiles.map((t) => {
          const body = (
            <Card className={`h-full p-5 ${t.to ? 'transition hover:border-ink/20 hover:bg-white' : ''}`}>
              <p className="text-xs font-medium uppercase tracking-wide text-muted">{t.label}</p>
              <p className={`mt-2 text-3xl font-semibold tabular-nums ${t.alert ? 'text-rose-600' : ''}`}>{t.value}</p>
            </Card>
          )
          return t.to ? (
            <Link key={t.label} to={t.to}>
              {body}
            </Link>
          ) : (
            <div key={t.label}>{body}</div>
          )
        })}
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <div className="space-y-6">
          <section>
            <h2 className="mb-3 font-semibold">Waiting for approval</h2>
            {pending.data && pending.data.length === 0 && <EmptyState title="All caught up" />}
            {pending.data && pending.data.length > 0 && <LoanTable loans={pending.data} />}
          </section>
          <section>
            <h2 className="mb-3 font-semibold">Overdue</h2>
            {overdue.data && overdue.data.length === 0 && <EmptyState title="Nothing overdue" />}
            {overdue.data && overdue.data.length > 0 && <LoanTable loans={overdue.data} />}
          </section>
        </div>

        <div className="space-y-6">
          <Card className="p-5">
            <h2 className="font-semibold">Assets by status</h2>
            <p className="text-sm text-muted">{totalAssets} assets in total</p>
            <div className="mt-4 flex h-3 overflow-hidden rounded-full bg-line" aria-hidden>
              {ASSET_STATUSES.map((status) =>
                counts[status] ? (
                  <div key={status} className={BAR_COLOUR[status]} style={{ width: `${(counts[status] / totalAssets) * 100}%` }} />
                ) : null,
              )}
            </div>
            <ul className="mt-4 space-y-2 text-sm">
              {ASSET_STATUSES.map((status) => (
                <li key={status} className="flex items-center justify-between">
                  <span className="flex items-center gap-2">
                    <span className={`h-2.5 w-2.5 rounded-sm ${BAR_COLOUR[status]}`} />
                    <Link to={`/assets?status=${status}`} className="hover:underline">
                      {STATUS_LABEL[status]}
                    </Link>
                  </span>
                  <span className="font-mono tabular-nums">{counts[status] ?? 0}</span>
                </li>
              ))}
            </ul>
          </Card>

          <Card className="p-5">
            <h2 className="font-semibold">Most borrowed</h2>
            {s.mostBorrowed.length === 0 ? (
              <p className="mt-2 text-sm text-muted">No completed loans yet.</p>
            ) : (
              <ol className="mt-3 space-y-2 text-sm">
                {s.mostBorrowed.map((a, i) => (
                  <li key={a.tag} className="flex items-center justify-between gap-2">
                    <span className="truncate">
                      <span className="mr-2 text-muted">{i + 1}.</span>
                      <span className="font-mono text-xs">{a.tag}</span> {a.name}
                    </span>
                    <span className="shrink-0 font-mono text-xs text-muted">{a.loanCount}×</span>
                  </li>
                ))}
              </ol>
            )}
          </Card>
        </div>
      </div>
    </>
  )
}
