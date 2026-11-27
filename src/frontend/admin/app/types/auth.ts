export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  expiresIn: number
  author: AuthorProfile
}

export interface AuthorProfile {
  id: number
  name: string
  email: string
  role: string
  bio?: string
  avatarUrl?: string
}
