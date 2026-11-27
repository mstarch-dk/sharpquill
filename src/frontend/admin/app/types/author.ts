export interface AuthorAdminDto {
  id: number
  email: string
  name: string
  role: string
  bio?: string
  avatarUrl?: string
  certificateThumbprint?: string
  createdAt: string
  updatedAt?: string
}

export interface AuthorCreateDto {
  email: string
  password: string
  name: string
  role: string
  bio?: string
  avatarUrl?: string
  certificateThumbprint?: string
}

export interface AuthorUpdateDto {
  email: string
  name: string
  role: string
  password?: string
  bio?: string
  avatarUrl?: string
  certificateThumbprint?: string
}
