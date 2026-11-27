export enum CommentStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2
}

export interface CommentAdminDto {
  id: number
  articleId: number
  articleTitle?: string
  authorName: string
  authorEmail: string
  content: string
  status: CommentStatus
  ipAddress?: string
  parentCommentId?: number
  createdAt: string
  updatedAt?: string
}

export interface CommentCountsDto {
  pending: number
  approved: number
  rejected: number
}

export function getCommentStatusLabel(status: CommentStatus): string {
  switch (status) {
    case CommentStatus.Pending:
      return 'Pending'
    case CommentStatus.Approved:
      return 'Approved'
    case CommentStatus.Rejected:
      return 'Rejected'
    default:
      return 'Unknown'
  }
}

export function getCommentStatusColor(status: CommentStatus): string {
  switch (status) {
    case CommentStatus.Pending:
      return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300'
    case CommentStatus.Approved:
      return 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300'
    case CommentStatus.Rejected:
      return 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300'
    default:
      return 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
  }
}
