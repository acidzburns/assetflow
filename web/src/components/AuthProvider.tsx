import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { api, getStoredAuth, setUnauthorizedHandler, storeAuth } from '../lib/api'
import { AuthContext, type AuthContextValue } from '../lib/useAuth'
import type { User } from '../lib/types'

/** Keeps track of who is logged in. The token is saved so a page reload keeps you logged in. */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(() => getStoredAuth()?.user ?? null)
  const queryClient = useQueryClient()

  const logout = useCallback(() => {
    storeAuth(null)
    setUser(null)
    queryClient.clear() // forget the previous user's data
  }, [queryClient])

  useEffect(() => setUnauthorizedHandler(logout), [logout])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAdmin: user?.role === 'Admin',
      login: async (email, password) => {
        const auth = await api.login(email, password)
        storeAuth(auth)
        setUser(auth.user)
      },
      register: async (name, email, password, department) => {
        const auth = await api.register(name, email, password, department)
        storeAuth(auth)
        setUser(auth.user)
      },
      logout,
    }),
    [user, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
