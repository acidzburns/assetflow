import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { AuthProvider } from '../components/AuthProvider'
import { Layout } from '../components/Layout'
import { LoginPage } from '../pages/LoginPage'
import { LoansPage } from '../pages/LoansPage'
import type { AuthResponse, Loan } from '../lib/types'

/** Render the app's routes in memory, with a fake API behind fetch. */
function renderApp(path: string) {
  const router = createMemoryRouter(
    [
      { path: '/login', element: <LoginPage /> },
      { element: <Layout />, children: [{ path: 'loans', element: <LoansPage /> }, { path: 'assets', element: <p>Assets page</p> }] },
    ],
    { initialEntries: [path] },
  )
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={client}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </QueryClientProvider>,
  )
  return router
}

const json = (status: number, body: unknown) => new Response(JSON.stringify(body), { status })

const authAs = (role: 'Admin' | 'Staff'): AuthResponse => ({
  token: 'test-token',
  expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
  user: { id: role === 'Admin' ? 1 : 2, name: role === 'Admin' ? 'Nadia Rahman' : 'Daniel Lim', email: 'x@y.z', role, department: null },
})

const requestedLoan: Loan = {
  id: 7, assetId: 1, assetTag: 'LAP-0001', assetName: 'Dell Latitude 7450', borrowerId: 2, borrowerName: 'Daniel Lim',
  purpose: 'Client visit', startDate: '2026-11-18', dueDate: '2026-11-20', status: 'Requested', isOverdue: false,
  requestedAt: '2026-11-18T01:00:00Z', reviewedBy: null, reviewNote: null, checkedOutAt: null, returnedAt: null,
}

beforeEach(() => localStorage.clear())
afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

describe('Login page', () => {
  it('shows the API error message when the password is wrong', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => json(401, { title: 'Wrong email or password', status: 401 })))
    renderApp('/login')

    await userEvent.type(screen.getByLabelText('Email'), 'admin@assetflow.dev')
    await userEvent.type(screen.getByLabelText('Password'), 'nope')
    await userEvent.click(screen.getByRole('button', { name: 'Log in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Wrong email or password')
  })

  it('logs in and remembers the session', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => json(200, authAs('Staff'))))
    const router = renderApp('/login')

    await userEvent.click(screen.getByRole('button', { name: 'Log in as Staff' }))

    await waitFor(() => expect(router.state.location.pathname).not.toBe('/login'))
    expect(JSON.parse(localStorage.getItem('assetflow.auth')!).user.name).toBe('Daniel Lim')
  })

  it('after logging out, the next login does not reopen the previous page', async () => {
    localStorage.setItem('assetflow.auth', JSON.stringify(authAs('Staff')))
    vi.stubGlobal('fetch', vi.fn(async () => json(200, [])))
    const router = renderApp('/loans')

    await userEvent.click((await screen.findAllByRole('button', { name: 'Log out' }))[0])

    await waitFor(() => expect(router.state.location.pathname).toBe('/login'))
    expect(router.state.location.state).toBeFalsy() // no "return to /loans"
  })

  it('sends logged-out visitors to the login page', () => {
    const router = renderApp('/loans')
    expect(router.state.location.pathname).toBe('/login')
  })
})

describe('Loans page', () => {
  it('admins see Approve / Reject and approving calls the API', async () => {
    localStorage.setItem('assetflow.auth', JSON.stringify(authAs('Admin')))
    const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
      if (url.endsWith('/approve')) return json(200, { ...requestedLoan, status: 'Approved' })
      expect(new Headers(init?.headers).get('Authorization')).toBe('Bearer test-token')
      return json(200, [requestedLoan])
    })
    vi.stubGlobal('fetch', fetchMock)
    renderApp('/loans')

    expect(await screen.findByText('Daniel Lim')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Reject' })).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Approve' }))
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/loans/7/approve', expect.objectContaining({ method: 'POST' })))
  })

  it('staff only get a Cancel button on their own request', async () => {
    localStorage.setItem('assetflow.auth', JSON.stringify(authAs('Staff')))
    vi.stubGlobal('fetch', vi.fn(async () => json(200, [requestedLoan])))
    renderApp('/loans')

    expect(await screen.findByRole('button', { name: 'Cancel' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Approve' })).not.toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'My loans' })).toBeInTheDocument()
  })
})
