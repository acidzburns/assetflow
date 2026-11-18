import { useEffect, useRef, type ButtonHTMLAttributes, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes, type TextareaHTMLAttributes } from 'react'
import { STATUS_LABEL } from '../lib/format'
import type { AssetStatus, LoanStatus } from '../lib/types'

/* Small, reusable building blocks so every screen looks the same. */

type Variant = 'primary' | 'secondary' | 'danger' | 'ghost'

const VARIANTS: Record<Variant, string> = {
  primary: 'bg-ink text-white hover:bg-ink/85',
  secondary: 'border border-line bg-white hover:border-ink/30',
  danger: 'border border-rose-200 bg-white text-rose-700 hover:bg-rose-50',
  ghost: 'text-muted hover:text-ink',
}

export function Button({
  variant = 'primary',
  size = 'md',
  className = '',
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: Variant; size?: 'sm' | 'md' }) {
  const sizing = size === 'sm' ? 'px-2.5 py-1.5 text-xs' : 'px-4 py-2 text-sm'
  return (
    <button
      className={`inline-flex items-center justify-center gap-1.5 rounded-md font-medium transition disabled:cursor-not-allowed disabled:opacity-50 ${sizing} ${VARIANTS[variant]} ${className}`}
      {...props}
    />
  )
}

export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <div className={`rounded-xl border border-card-line bg-card ${className}`}>{children}</div>
}

export function PageHeader({ title, subtitle, actions }: { title: string; subtitle?: string; actions?: ReactNode }) {
  return (
    <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
        {subtitle && <p className="mt-1 text-sm text-muted">{subtitle}</p>}
      </div>
      {actions && <div className="flex gap-2">{actions}</div>}
    </div>
  )
}

export function Field({ label, hint, children }: { label: string; hint?: string; children: ReactNode }) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium">{label}</span>
      {children}
      {hint && <span className="mt-1 block text-xs text-muted">{hint}</span>}
    </label>
  )
}

const inputClass =
  'w-full rounded-md border border-line bg-white px-3 py-2 text-sm outline-none transition focus:border-accent focus:ring-2 focus:ring-accent/15'

export const Input = (props: InputHTMLAttributes<HTMLInputElement>) => <input className={inputClass} {...props} />
export const Select = (props: SelectHTMLAttributes<HTMLSelectElement>) => <select className={inputClass} {...props} />
export const Textarea = (props: TextareaHTMLAttributes<HTMLTextAreaElement>) => (
  <textarea className={inputClass} rows={3} {...props} />
)

export function ErrorMessage({ error }: { error: unknown }) {
  if (!error) return null
  const message = error instanceof Error ? error.message : String(error)
  return (
    <p role="alert" className="rounded-md border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">
      {message}
    </p>
  )
}

export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="rounded-xl border border-dashed border-line px-6 py-12 text-center">
      <p className="font-medium">{title}</p>
      {children && <div className="mt-1 text-sm text-muted">{children}</div>}
    </div>
  )
}

export function Loading() {
  return <p className="py-10 text-center text-sm text-muted">Loading…</p>
}

const STATUS_STYLE: Record<AssetStatus | LoanStatus, string> = {
  Available: 'bg-emerald-50 text-emerald-700 ring-emerald-600/20',
  OnLoan: 'bg-sky-50 text-sky-700 ring-sky-600/20',
  InRepair: 'bg-amber-50 text-amber-800 ring-amber-600/25',
  Retired: 'bg-zinc-100 text-zinc-600 ring-zinc-500/20',
  Requested: 'bg-violet-50 text-violet-700 ring-violet-600/20',
  Approved: 'bg-sky-50 text-sky-700 ring-sky-600/20',
  CheckedOut: 'bg-emerald-50 text-emerald-700 ring-emerald-600/20',
  Returned: 'bg-zinc-100 text-zinc-600 ring-zinc-500/20',
  Rejected: 'bg-rose-50 text-rose-700 ring-rose-600/20',
  Cancelled: 'bg-zinc-100 text-zinc-500 ring-zinc-500/20',
}

export function StatusBadge({ status, overdue }: { status: AssetStatus | LoanStatus; overdue?: boolean }) {
  if (overdue) {
    return (
      <span className="inline-flex rounded px-1.5 py-0.5 font-mono text-[11px] font-medium text-rose-700 ring-1 ring-rose-600/20 ring-inset bg-rose-50">
        Overdue
      </span>
    )
  }
  return (
    <span className={`inline-flex whitespace-nowrap rounded px-1.5 py-0.5 font-mono text-[11px] font-medium ring-1 ring-inset ${STATUS_STYLE[status]}`}>
      {STATUS_LABEL[status]}
    </span>
  )
}

/** A centred dialog box. Closes with Esc or by clicking outside. */
export function Modal({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) {
  const ref = useRef<HTMLDivElement>(null)
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    ref.current?.querySelector<HTMLElement>('input, textarea, select')?.focus()
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-ink/30 p-4" onMouseDown={onClose}>
      <div
        ref={ref}
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className="w-full max-w-md rounded-xl bg-white p-6 shadow-xl"
        onMouseDown={(e) => e.stopPropagation()}
      >
        <h2 className="text-lg font-semibold">{title}</h2>
        <div className="mt-4">{children}</div>
      </div>
    </div>
  )
}
