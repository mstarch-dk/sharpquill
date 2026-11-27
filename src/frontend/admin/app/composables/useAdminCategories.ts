import type { ApiResponse, CategoryDto, CategoryCreateDto, CategoryUpdateDto } from '~/types/api'

export function useAdminCategories() {
  const api = useApi()
  const endpoint = '/api/admin/categories'

  async function getCategories(): Promise<ApiResponse<CategoryDto[]>> {
    return api.get<CategoryDto[]>(endpoint)
  }

  async function getCategoryById(id: number): Promise<ApiResponse<CategoryDto>> {
    return api.get<CategoryDto>(`${endpoint}/${id}`)
  }

  async function createCategory(data: CategoryCreateDto): Promise<ApiResponse<CategoryDto>> {
    return api.post<CategoryDto>(endpoint, data)
  }

  async function updateCategory(id: number, data: CategoryUpdateDto): Promise<ApiResponse<CategoryDto>> {
    return api.put<CategoryDto>(`${endpoint}/${id}`, data)
  }

  async function deleteCategory(id: number): Promise<ApiResponse<void>> {
    return api.del<void>(`${endpoint}/${id}`)
  }

  return {
    getCategories,
    getCategoryById,
    createCategory,
    updateCategory,
    deleteCategory
  }
}
