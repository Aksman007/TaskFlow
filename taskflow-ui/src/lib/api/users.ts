import apiClient from './client';
import { User } from '../types';
import { projectsApi } from './projects';

export const usersApi = {
  getMe: async (): Promise<User> => {
    const response = await apiClient.get<User>('/Users/me');
    return response.data;
  },

  searchByEmail: async (email: string): Promise<User[]> => {
    const response = await apiClient.get<User[]>(`/Users/search?email=${encodeURIComponent(email)}`);
    return response.data;
  },

  getProjectMembers: async (projectId: string): Promise<User[]> => {
    // Use projectsApi instead
    return projectsApi.getMembers(projectId);
  },

  updateProfile: async (data: { fullName: string }): Promise<User> => {
    const response = await apiClient.put<User>('/Users/me', data);
    return response.data;
  },
};
