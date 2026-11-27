import type { ApiResponse } from '~/types/api'
import type { MediaDto, MediaUploadResultDto, MediaUsageDto } from '~/types/media'

export function useAdminMedia() {
  const api = useApi()
  const endpoint = '/api/admin/media'

  async function getMedia(
    page = 1,
    pageSize = 20
  ): Promise<ApiResponse<MediaDto[]>> {
    return api.get<MediaDto[]>(endpoint, { page, pageSize })
  }

  // Goes through useApi so an upload gets the same token refresh and 401 retry as every other request
  async function uploadMedia(file: File): Promise<ApiResponse<MediaUploadResultDto>> {
    const formData = new FormData()
    formData.append('file', file)
    return api.post<MediaUploadResultDto>(endpoint, formData)
  }

  /** Where the media is still used: featured images, content, layouts, settings and avatars */
  async function getMediaUsages(id: number): Promise<ApiResponse<MediaUsageDto[]>> {
    return api.get<MediaUsageDto[]>(`${endpoint}/${id}/usages`)
  }

  /**
   * The API refuses (409 MEDIA_IN_USE) to delete media that is still used; `force` deletes it anyway, which clears
   * featured images and leaves inline references broken.
   */
  async function deleteMedia(id: number, force = false): Promise<ApiResponse<void>> {
    return api.del<void>(`${endpoint}/${id}${force ? '?force=true' : ''}`)
  }

  return {
    getMedia,
    uploadMedia,
    getMediaUsages,
    deleteMedia
  }
}
