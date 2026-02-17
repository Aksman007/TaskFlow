import apiClient from './client';
import { Comment, AddCommentRequest, PaginatedResponse } from '../types';

export const commentsApi = {
  getTaskComments: async (taskId: string): Promise<PaginatedResponse<Comment>> => {
    const response = await apiClient.get<PaginatedResponse<Comment>>(`/Comments/task/${taskId}`);
    return response.data;
  },

  add: async (data: AddCommentRequest): Promise<Comment> => {
    const response = await apiClient.post<Comment>('/Comments', data);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/Comments/${id}`);
  },
};