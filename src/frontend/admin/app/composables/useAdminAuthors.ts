import type { ApiResponse } from '~/types/api'
import type { AuthorAdminDto, AuthorCreateDto, AuthorUpdateDto } from '~/types/author'

export function useAdminAuthors() {
  const api = useApi()
  const endpoint = '/api/admin/authors'

  async function getAuthors(): Promise<ApiResponse<AuthorAdminDto[]>> {
    return api.get<AuthorAdminDto[]>(endpoint)
  }

  async function getAuthorById(id: number): Promise<ApiResponse<AuthorAdminDto>> {
    return api.get<AuthorAdminDto>(`${endpoint}/${id}`)
  }

  async function createAuthor(data: AuthorCreateDto): Promise<ApiResponse<AuthorAdminDto>> {
    return api.post<AuthorAdminDto>(endpoint, data)
  }

  async function updateAuthor(id: number, data: AuthorUpdateDto): Promise<ApiResponse<AuthorAdminDto>> {
    return api.put<AuthorAdminDto>(`${endpoint}/${id}`, data)
  }

  async function deleteAuthor(id: number): Promise<ApiResponse<void>> {
    return api.del<void>(`${endpoint}/${id}`)
  }

  return {
    getAuthors,
    getAuthorById,
    createAuthor,
    updateAuthor,
    deleteAuthor
  }
}
