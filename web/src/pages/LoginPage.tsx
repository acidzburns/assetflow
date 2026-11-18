import { useState } from 'react'
import { Navigate, useLocation } from 'react-router'
import { useAuth } from '../lib/useAuth'
import { Button, ErrorMessage, Field, Input } from '../components/ui'

const DEMO_ACCOUNTS = [
  { label: 'Admin', email: 'admin@assetflow.dev', password: 'Admin@12345' },
  { label: 'Staff', email: 'staff@assetflow.dev', password: 'Staff@12345' },
]

export function LoginPage() {
  const { user, login, register } = useAuth()
  const location = useLocation()
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [form, setForm] = useState({ name: '', email: '', password: '', department: '' })
  const [error, setError] = useState<unknown>(null)
  const [busy, setBusy] = useState(false)

  // Once logged in, go back to the page that asked for a login (or home)
  if (user) return <Navigate to={(location.state as { from?: string } | null)?.from ?? '/'} replace />

  const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm({ ...form, [key]: e.target.value })

  async function submit(email = form.email, password = form.password) {
    setBusy(true)
    setError(null)
    try {
      if (mode === 'register') await register(form.name, email, password, form.department || null)
      else await login(email, password)
    } catch (err) {
      setError(err)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="grid min-h-screen place-items-center bg-card px-4">
      <div className="w-full max-w-sm">
        <div className="mb-6 flex items-center justify-center gap-2">
          <img src="/favicon.svg" alt="" className="h-8 w-8" />
          <span className="text-2xl font-semibold tracking-tight">AssetFlow</span>
        </div>

        <div className="rounded-xl border border-card-line bg-white p-6 shadow-sm">
          <h1 className="text-lg font-semibold">{mode === 'login' ? 'Log in' : 'Create a staff account'}</h1>
          <p className="mt-1 text-sm text-muted">IT equipment, booked in a few clicks.</p>

          <form
            className="mt-5 space-y-4"
            onSubmit={(e) => {
              e.preventDefault()
              submit()
            }}
          >
            {mode === 'register' && (
              <>
                <Field label="Full name">
                  <Input value={form.name} onChange={set('name')} required maxLength={100} autoComplete="name" />
                </Field>
                <Field label="Department">
                  <Input value={form.department} onChange={set('department')} maxLength={100} placeholder="Optional" />
                </Field>
              </>
            )}
            <Field label="Email">
              <Input type="email" value={form.email} onChange={set('email')} required autoComplete="email" />
            </Field>
            <Field label="Password" hint={mode === 'register' ? 'At least 8 characters.' : undefined}>
              <Input
                type="password"
                value={form.password}
                onChange={set('password')}
                required
                minLength={mode === 'register' ? 8 : undefined}
                autoComplete={mode === 'register' ? 'new-password' : 'current-password'}
              />
            </Field>
            <ErrorMessage error={error} />
            <Button type="submit" className="w-full" disabled={busy}>
              {busy ? 'Please wait…' : mode === 'login' ? 'Log in' : 'Create account'}
            </Button>
          </form>

          <p className="mt-4 text-center text-sm text-muted">
            {mode === 'login' ? 'New here?' : 'Already have an account?'}{' '}
            <button
              className="font-medium text-accent hover:underline"
              onClick={() => {
                setMode(mode === 'login' ? 'register' : 'login')
                setError(null)
              }}
            >
              {mode === 'login' ? 'Create an account' : 'Log in'}
            </button>
          </p>
        </div>

        {mode === 'login' && (
          <div className="mt-4 rounded-xl border border-dashed border-card-line p-4">
            <p className="text-xs font-medium uppercase tracking-wide text-muted">Try a demo account</p>
            <div className="mt-2 flex gap-2">
              {DEMO_ACCOUNTS.map((account) => (
                <Button
                  key={account.label}
                  variant="secondary"
                  size="sm"
                  className="flex-1"
                  disabled={busy}
                  onClick={() => {
                    setForm({ ...form, email: account.email, password: account.password })
                    submit(account.email, account.password)
                  }}
                >
                  Log in as {account.label}
                </Button>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
