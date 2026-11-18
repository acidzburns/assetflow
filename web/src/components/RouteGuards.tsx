import type { ReactNode } from 'react'
import { Navigate } from 'react-router'
import { useAuth } from '../lib/useAuth'

/** Admins land on the dashboard, staff on the assets list. */
export function Home() {
  const { isAdmin } = useAuth()
  return <Navigate to={isAdmin ? '/dashboard' : '/assets'} replace />
}

/** Sends non-admins away from admin-only pages. */
export function AdminOnly({ children }: { children: ReactNode }) {
  const { isAdmin } = useAuth()
  return isAdmin ? children : <Navigate to="/assets" replace />
}
