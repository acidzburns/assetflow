import { useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api } from '../lib/api'
import { useAuth } from '../lib/useAuth'
import { STATUS_LABEL } from '../lib/format'
import { ASSET_CATEGORIES, ASSET_STATUSES } from '../lib/types'
import { AssetForm } from '../components/AssetForm'
import { Button, EmptyState, ErrorMessage, Input, Loading, PageHeader, Select, StatusBadge } from '../components/ui'

const PAGE_SIZE = 12

export function AssetsPage() {
  const { isAdmin } = useAuth()
  const [params, setParams] = useSearchParams() // filters live in the URL, so they survive a refresh
  const [adding, setAdding] = useState(false)
  const search = params.get('search') ?? ''
  const status = params.get('status') ?? ''
  const category = params.get('category') ?? ''
  const page = Number(params.get('page') ?? 1)

  const { data, isLoading, error } = useQuery({
    queryKey: ['assets', { search, status, category, page }],
    queryFn: () => api.searchAssets({ search, status, category, page, pageSize: PAGE_SIZE }),
    placeholderData: keepPreviousData,
  })

  const update = (key: string, value: string) => {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next, { replace: true })
  }

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE)) : 1

  return (
    <>
      <PageHeader
        title="Assets"
        subtitle={data ? `${data.totalCount} item${data.totalCount === 1 ? '' : 's'}` : undefined}
        actions={isAdmin && <Button onClick={() => setAdding(true)}>+ Add asset</Button>}
      />

      <div className="mb-5 grid gap-3 sm:grid-cols-[1fr_180px_180px]">
        <Input
          type="search"
          placeholder="Search by tag, name, serial or category…"
          value={search}
          onChange={(e) => update('search', e.target.value)}
          aria-label="Search assets"
        />
        <Select value={status} onChange={(e) => update('status', e.target.value)} aria-label="Status">
          <option value="">All statuses</option>
          {ASSET_STATUSES.map((s) => (
            <option key={s} value={s}>
              {STATUS_LABEL[s]}
            </option>
          ))}
        </Select>
        <Select value={category} onChange={(e) => update('category', e.target.value)} aria-label="Category">
          <option value="">All categories</option>
          {ASSET_CATEGORIES.map((c) => (
            <option key={c}>{c}</option>
          ))}
        </Select>
      </div>

      <ErrorMessage error={error} />
      {isLoading && <Loading />}

      {data && data.items.length === 0 && (
        <EmptyState title="No assets match">Try a different search or clear the filters.</EmptyState>
      )}

      {data && data.items.length > 0 && (
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          {data.items.map((asset) => (
            <Link
              key={asset.id}
              to={`/assets/${asset.id}`}
              className="group rounded-xl border border-card-line bg-card p-5 transition hover:border-ink/20 hover:bg-white"
            >
              <div className="flex items-start justify-between gap-2">
                <span className="font-mono text-xs font-medium text-muted">{asset.tag}</span>
                <StatusBadge status={asset.status} />
              </div>
              <p className="mt-2 font-medium group-hover:text-accent">{asset.name}</p>
              <p className="mt-1 text-sm text-muted">
                {asset.category}
                {asset.location ? ` · ${asset.location}` : ''}
              </p>
            </Link>
          ))}
        </div>
      )}

      {data && totalPages > 1 && (
        <div className="mt-6 flex items-center justify-center gap-3 text-sm">
          <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => update('page', String(page - 1))}>
            ← Previous
          </Button>
          <span className="text-muted">
            Page {page} of {totalPages}
          </span>
          <Button variant="secondary" size="sm" disabled={page >= totalPages} onClick={() => update('page', String(page + 1))}>
            Next →
          </Button>
        </div>
      )}

      {adding && <AssetForm onClose={() => setAdding(false)} />}
    </>
  )
}
