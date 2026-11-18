import { useState } from 'react'
import { Navigate, NavLink, Outlet, useLocation } from 'react-router'
import { useAuth } from '../lib/useAuth'

/** The frame around every signed-in page: sidebar navigation + content area. */
export function Layout() {
  const { user, isAdmin, logout } = useAuth()
  const location = useLocation()
  const [signedOut, setSignedOut] = useState(false)

  // An explicit log out starts fresh: the next person should land on their own home page,
  // not on whatever page the previous user had open.
  const signOut = () => {
    setSignedOut(true) // React applies this together with the logout below
    logout()
  }

  // Not logged in (or the token expired): go to login, and come back here afterwards
  if (!user) {
    return <Navigate to="/login" replace state={signedOut ? undefined : { from: location.pathname }} />
  }

  const links = [
    ...(isAdmin ? [{ to: '/dashboard', label: 'Dashboard' }] : []),
    { to: '/assets', label: 'Assets' },
    { to: '/loans', label: isAdmin ? 'Loans' : 'My loans' },
  ]

  return (
    <div className="min-h-screen md:grid md:grid-cols-[220px_1fr]">
      <aside className="flex flex-col border-b border-line bg-white md:sticky md:top-0 md:h-screen md:border-b-0 md:border-r">
        <div className="flex items-center gap-2 px-5 py-4">
          <img src="/favicon.svg" alt="" className="h-6 w-6" />
          <span className="text-lg font-semibold tracking-tight">AssetFlow</span>
        </div>

        <nav className="flex gap-1 px-3 pb-3 md:flex-col md:pb-0">
          {links.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              className={({ isActive }) =>
                `rounded-md px-3 py-2 text-sm font-medium transition ${
                  isActive ? 'bg-accent-soft text-accent' : 'text-muted hover:bg-card hover:text-ink'
                }`
              }
            >
              {link.label}
            </NavLink>
          ))}
        </nav>

        <div className="mt-auto hidden border-t border-line p-4 md:block">
          <p className="truncate text-sm font-medium">{user.name}</p>
          <p className="truncate text-xs text-muted">
            {user.role}
            {user.department ? ` · ${user.department}` : ''}
          </p>
          <button onClick={signOut} className="mt-3 text-xs font-medium text-muted hover:text-ink">
            Log out
          </button>
        </div>
      </aside>

      <main className="min-w-0 px-5 py-8 sm:px-8">
        <div className="mb-4 flex items-center justify-between text-sm md:hidden">
          <span className="text-muted">
            {user.name} · {user.role}
          </span>
          <button onClick={signOut} className="font-medium text-muted">
            Log out
          </button>
        </div>
        <Outlet />
      </main>
    </div>
  )
}
