import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createBrowserRouter, RouterProvider } from 'react-router'
import { AuthProvider } from './components/AuthProvider'
import { AdminOnly, Home } from './components/RouteGuards'
import { ApiError } from './lib/api'
import { Layout } from './components/Layout'
import { LoginPage } from './pages/LoginPage'
import { AssetsPage } from './pages/AssetsPage'
import { AssetDetailPage } from './pages/AssetDetailPage'
import { LoansPage } from './pages/LoansPage'
import { DashboardPage } from './pages/DashboardPage'
import { EmptyState } from './components/ui'
import './index.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 15_000,
      // Don't retry "you're not allowed" or "not found" errors
      retry: (count, error) => !(error instanceof ApiError && error.status >= 400 && error.status < 500) && count < 2,
    },
  },
})

const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    element: <Layout />,
    children: [
      { index: true, element: <Home /> },
      { path: 'dashboard', element: <AdminOnly><DashboardPage /></AdminOnly> },
      { path: 'assets', element: <AssetsPage /> },
      { path: 'assets/:id', element: <AssetDetailPage /> },
      { path: 'loans', element: <LoansPage /> },
      { path: '*', element: <EmptyState title="Page not found" /> },
    ],
  },
])

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </QueryClientProvider>
  </StrictMode>,
)
