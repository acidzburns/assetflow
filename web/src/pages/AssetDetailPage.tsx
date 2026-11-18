import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../lib/api'
import { useAuth } from '../lib/useAuth'
import { formatDate, formatDateTime, formatMoney, loanLengthDays, todayIso } from '../lib/format'
import type { Asset, Maintenance } from '../lib/types'
import { AssetForm } from '../components/AssetForm'
import { LoanTable } from '../components/LoanTable'
import { Button, Card, EmptyState, ErrorMessage, Field, Input, Loading, Modal, StatusBadge, Textarea } from '../components/ui'

const MAX_LOAN_DAYS = 30

export function AssetDetailPage() {
  const id = Number(useParams().id)
  const { isAdmin } = useAuth()
  const [editing, setEditing] = useState(false)
  const { data, isLoading, error } = useQuery({ queryKey: ['asset', id], queryFn: () => api.getAsset(id) })

  if (isLoading) return <Loading />
  if (error || !data) return <ErrorMessage error={error ?? 'Asset not found'} />

  const { asset, recentLoans, maintenance } = data
  const openRepair = maintenance.find((m) => !m.closedAt)
  const canBorrow = asset.status === 'Available' || asset.status === 'OnLoan'

  return (
    <>
      <Link to="/assets" className="text-sm text-muted hover:text-ink">
        ← All assets
      </Link>

      <div className="mt-3 mb-6 flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="flex items-center gap-3">
            <span className="font-mono text-sm text-muted">{asset.tag}</span>
            <StatusBadge status={asset.status} />
          </div>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">{asset.name}</h1>
        </div>
        {isAdmin && <AdminActions asset={asset} openRepair={openRepair} onEdit={() => setEditing(true)} />}
      </div>

      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
        <div className="space-y-6">
          <Card className="grid grid-cols-2 gap-x-6 gap-y-4 p-5 text-sm sm:grid-cols-4">
            <Detail label="Category" value={asset.category} />
            <Detail label="Serial number" value={asset.serialNumber} mono />
            <Detail label="Location" value={asset.location} />
            <Detail label="Purchased" value={formatDate(asset.purchasedOn)} />
          </Card>

          <section>
            <h2 className="mb-3 font-semibold">Loan history</h2>
            {recentLoans.length === 0 ? (
              <EmptyState title="Not borrowed yet" />
            ) : (
              <LoanTable loans={recentLoans} showAsset={false} />
            )}
          </section>

          <section>
            <h2 className="mb-3 font-semibold">Maintenance</h2>
            {maintenance.length === 0 ? (
              <EmptyState title="No repairs recorded" />
            ) : (
              <ul className="divide-y divide-line rounded-xl border border-line">
                {maintenance.map((m) => (
                  <li key={m.id} className="flex flex-wrap items-start justify-between gap-2 px-4 py-3 text-sm">
                    <div>
                      <p className="font-medium">{m.issue}</p>
                      <p className="text-muted">
                        {m.closedAt ? `Fixed: ${m.resolution ?? 'No notes'}` : 'In progress'}
                      </p>
                    </div>
                    <div className="text-right text-xs text-muted">
                      <p>Opened {formatDateTime(m.openedAt)}</p>
                      {m.closedAt && <p>Closed {formatDateTime(m.closedAt)} · {formatMoney(m.cost)}</p>}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </div>

        <aside>
          {canBorrow ? (
            <LoanRequestForm asset={asset} />
          ) : (
            <Card className="p-5 text-sm text-muted">
              This asset is {asset.status === 'InRepair' ? 'being repaired' : 'retired'} and can't be borrowed right now.
            </Card>
          )}
        </aside>
      </div>

      {editing && <AssetForm asset={asset} onClose={() => setEditing(false)} />}
    </>
  )
}

function Detail({ label, value, mono }: { label: string; value: string | null; mono?: boolean }) {
  return (
    <div>
      <p className="text-xs text-muted">{label}</p>
      <p className={`mt-0.5 font-medium ${mono ? 'font-mono text-xs' : ''}`}>{value || '—'}</p>
    </div>
  )
}

function LoanRequestForm({ asset }: { asset: Asset }) {
  const queryClient = useQueryClient()
  const [form, setForm] = useState({ startDate: todayIso(), dueDate: todayIso(2), purpose: '' })
  const days = form.startDate && form.dueDate ? loanLengthDays(form.startDate, form.dueDate) : 0
  const tooLong = days > MAX_LOAN_DAYS

  const request = useMutation({
    mutationFn: () => api.requestLoan(asset.id, form.startDate, form.dueDate, form.purpose.trim()),
    onSuccess: () => {
      setForm({ ...form, purpose: '' })
      queryClient.invalidateQueries()
    },
  })

  return (
    <Card className="p-5">
      <h2 className="font-semibold">Request this {asset.category.toLowerCase()}</h2>
      <p className="mt-1 text-sm text-muted">An admin will approve it before you collect it.</p>

      {request.isSuccess ? (
        <div className="mt-4 rounded-md bg-accent-soft p-4 text-sm text-accent">
          <p className="font-medium">Request sent.</p>
          <p className="mt-1">
            Track it in <Link to="/loans" className="underline">My loans</Link>.
          </p>
          <button className="mt-2 text-xs underline" onClick={() => request.reset()}>
            Make another request
          </button>
        </div>
      ) : (
        <form
          className="mt-4 space-y-4"
          onSubmit={(e) => {
            e.preventDefault()
            request.mutate()
          }}
        >
          <div className="grid grid-cols-2 gap-3">
            <Field label="From">
              <Input
                type="date"
                min={todayIso()}
                value={form.startDate}
                onChange={(e) => setForm({ ...form, startDate: e.target.value })}
                required
              />
            </Field>
            <Field label="Until">
              <Input
                type="date"
                min={form.startDate}
                value={form.dueDate}
                onChange={(e) => setForm({ ...form, dueDate: e.target.value })}
                required
              />
            </Field>
          </div>
          <p className={`text-xs ${tooLong ? 'text-rose-600' : 'text-muted'}`}>
            {days > 0 ? `${days} day${days === 1 ? '' : 's'}` : ''} · maximum {MAX_LOAN_DAYS} days
          </p>
          <Field label="What do you need it for?">
            <Textarea
              value={form.purpose}
              onChange={(e) => setForm({ ...form, purpose: e.target.value })}
              required
              maxLength={300}
              placeholder="e.g. Client presentation in KL"
            />
          </Field>
          <ErrorMessage error={request.error} />
          <Button type="submit" className="w-full" disabled={request.isPending || tooLong || days < 1}>
            {request.isPending ? 'Sending…' : 'Send request'}
          </Button>
        </form>
      )}
    </Card>
  )
}

function AdminActions({ asset, openRepair, onEdit }: { asset: Asset; openRepair?: Maintenance; onEdit: () => void }) {
  const queryClient = useQueryClient()
  const [dialog, setDialog] = useState<'repair' | 'complete' | 'retire' | null>(null)
  const [text, setText] = useState('')
  const [cost, setCost] = useState('')

  const action = useMutation({
    mutationFn: async () => {
      if (dialog === 'repair') await api.sendToRepair(asset.id, text.trim())
      if (dialog === 'complete' && openRepair) await api.completeRepair(openRepair.id, text.trim() || null, cost ? Number(cost) : null)
      if (dialog === 'retire') await api.retireAsset(asset.id)
    },
    onSuccess: () => {
      setDialog(null)
      queryClient.invalidateQueries()
    },
  })

  const open = (d: typeof dialog) => {
    setText('')
    setCost('')
    action.reset()
    setDialog(d)
  }

  const canRepair = asset.status === 'Available'
  const canRetire = asset.status !== 'OnLoan' && asset.status !== 'Retired'

  return (
    <div className="flex flex-wrap gap-2">
      <Button variant="secondary" onClick={onEdit}>
        Edit
      </Button>
      {canRepair && (
        <Button variant="secondary" onClick={() => open('repair')}>
          Send to repair
        </Button>
      )}
      {openRepair && <Button onClick={() => open('complete')}>Complete repair</Button>}
      {canRetire && (
        <Button variant="danger" onClick={() => open('retire')}>
          Retire
        </Button>
      )}

      {dialog && (
        <Modal
          title={dialog === 'repair' ? `Send ${asset.tag} to repair` : dialog === 'complete' ? 'Complete repair' : `Retire ${asset.tag}?`}
          onClose={() => setDialog(null)}
        >
          <form
            className="space-y-4"
            onSubmit={(e) => {
              e.preventDefault()
              action.mutate()
            }}
          >
            {dialog === 'repair' && (
              <Field label="What's wrong?">
                <Textarea value={text} onChange={(e) => setText(e.target.value)} required maxLength={300} />
              </Field>
            )}
            {dialog === 'complete' && (
              <>
                <p className="text-sm text-muted">Issue: {openRepair?.issue}</p>
                <Field label="What was done?">
                  <Textarea value={text} onChange={(e) => setText(e.target.value)} maxLength={300} />
                </Field>
                <Field label="Cost (RM)">
                  <Input type="number" min="0" step="0.01" value={cost} onChange={(e) => setCost(e.target.value)} />
                </Field>
              </>
            )}
            {dialog === 'retire' && (
              <p className="text-sm text-muted">
                It will no longer be available to borrow. Its loan and repair history is kept.
              </p>
            )}
            <ErrorMessage error={action.error} />
            <div className="flex justify-end gap-2">
              <Button type="button" variant="secondary" onClick={() => setDialog(null)}>
                Cancel
              </Button>
              <Button type="submit" variant={dialog === 'retire' ? 'danger' : 'primary'} disabled={action.isPending}>
                {dialog === 'repair' ? 'Send to repair' : dialog === 'complete' ? 'Mark as fixed' : 'Retire asset'}
              </Button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}
