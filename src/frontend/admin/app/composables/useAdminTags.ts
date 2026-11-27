import type { ApiResponse, TagDto, TagCreateDto, TagUpdateDto } from '~/types/api'

export function useAdminTags() {
  const api = useApi()
  const endpoint = '/api/admin/tags'

  async function getTags(): Promise<ApiResponse<TagDto[]>> {
    return api.get<TagDto[]>(endpoint)
  }

  async function getTagById(id: number): Promise<ApiResponse<TagDto>> {
    return api.get<TagDto>(`${endpoint}/${id}`)
  }

  async function createTag(data: TagCreateDto): Promise<ApiResponse<TagDto>> {
    return api.post<TagDto>(endpoint, data)
  }

  async function updateTag(id: number, data: TagUpdateDto): Promise<ApiResponse<TagDto>> {
    return api.put<TagDto>(`${endpoint}/${id}`, data)
  }

  async function deleteTag(id: number): Promise<ApiResponse<void>> {
    return api.del<void>(`${endpoint}/${id}`)
  }

  return {
    getTags,
    getTagById,
    createTag,
    updateTag,
    deleteTag
  }
}
