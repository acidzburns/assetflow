// These match the C# DTOs in src/AssetFlow.Api/Contracts/Dtos.cs

export type UserRole = 'Staff' | 'Admin'
export type AssetStatus = 'Available' | 'OnLoan' | 'InRepair' | 'Retired'
export type AssetCategory = 'Laptop' | 'Monitor' | 'Tablet' | 'Projector' | 'Camera' | 'Accessory'
export type LoanStatus = 'Requested' | 'Approved' | 'Rejected' | 'Cancelled' | 'CheckedOut' | 'Returned'

export const ASSET_STATUSES: AssetStatus[] = ['Available', 'OnLoan', 'InRepair', 'Retired']
export const ASSET_CATEGORIES: AssetCategory[] = ['Laptop', 'Monitor', 'Tablet', 'Projector', 'Camera', 'Accessory']
export const LOAN_STATUSES: LoanStatus[] = ['Requested', 'Approved', 'CheckedOut', 'Returned', 'Rejected', 'Cancelled']

export interface User {
  id: number
  name: string
  email: string
  role: UserRole
  department: string | null
}

export interface AuthResponse {
  token: string
  expiresAt: string
  user: User
}

export interface Asset {
  id: number
  tag: string
  name: string
  category: AssetCategory
  serialNumber: string | null
  location: string | null
  purchasedOn: string | null
  status: AssetStatus
}

export interface AssetInput {
  tag: string
  name: string
  category: AssetCategory
  serialNumber: string | null
  location: string | null
  purchasedOn: string | null
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface Loan {
  id: number
  assetId: number
  assetTag: string
  assetName: string
  borrowerId: number
  borrowerName: string
  purpose: string
  startDate: string
  dueDate: string
  status: LoanStatus
  isOverdue: boolean
  requestedAt: string
  reviewedBy: string | null
  reviewNote: string | null
  checkedOutAt: string | null
  returnedAt: string | null
}

export interface Maintenance {
  id: number
  assetId: number
  issue: string
  resolution: string | null
  cost: number | null
  openedAt: string
  closedAt: string | null
}

export interface AssetDetail {
  asset: Asset
  recentLoans: Loan[]
  maintenance: Maintenance[]
}

export interface SummaryReport {
  assetsByStatus: { status: AssetStatus; count: number }[]
  pendingRequests: number
  activeLoans: number
  overdueLoans: number
  mostBorrowed: { tag: string; name: string; loanCount: number }[]
  repairCostThisYear: number
}
