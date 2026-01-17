import apiClient from './client';
import { Comment, AddCommentRequest } from '../types';

export const commentsApi = {
  getTaskComments: async (taskId: string): Promise<Comment[]> => {
    const response = await apiClient.get<Comment[]>(`/comments/task/${taskId}`);
    return response.data;
  },

  add: async (data: AddCommentRequest): Promise<Comment> => {
    const response = await apiClient.post<Comment>('/comments', data);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/comments/${id}`);
  },
};