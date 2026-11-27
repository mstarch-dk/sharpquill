export enum WebsiteUserStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2
}

export interface WebsiteUserDto {
  id: number
  name: string
  email: string
  status: WebsiteUserStatus
  createdAt: string
  updatedAt?: string
}

export interface WebsiteUserCountsDto {
  pending: number
  approved: number
  rejected: number
}

export interface WebsiteLoginTokenDto {
  token: string
  expiresAt: string
}

export function getWebsiteUserStatusLabel(status: WebsiteUserStatus): string {
  switch (status) {
    case WebsiteUserStatus.Pending:
      return 'Pending'
    case WebsiteUserStatus.Approved:
      return 'Approved'
    case WebsiteUserStatus.Rejected:
      return 'Rejected'
    default:
      return 'Unknown'
  }
}

export function getWebsiteUserStatusColor(status: WebsiteUserStatus): string {
  switch (status) {
    case WebsiteUserStatus.Pending:
      return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300'
    case WebsiteUserStatus.Approved:
      return 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300'
    case WebsiteUserStatus.Rejected:
      return 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300'
    default:
      return 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
  }
}
