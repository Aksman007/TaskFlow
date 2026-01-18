/* eslint-disable @typescript-eslint/no-explicit-any */
import apiClient from './client';
import { Project, CreateProjectRequest } from '../types';

export const projectsApi = {
  getAll: async (): Promise<Project[]> => {
    console.log('Fetching all projects...');
    const response = await apiClient.get<Project[]>('/Projects');
    console.log('Projects fetched:', response.data);
    return response.data;
  },

  getById: async (id: string): Promise<Project> => {
    console.log('Fetching project by ID:', id);
    console.log('Full URL:', `/Projects/${id}`);
    
    try {
      const response = await apiClient.get<Project>(`/Projects/${id}`);
      console.log('Project fetched:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('Error fetching project:', {
        url: `/Projects/${id}`,
        status: error.response?.status,
        statusText: error.response?.statusText,
        data: error.response?.data,
      });
      throw error;
    }
  },

  create: async (data: CreateProjectRequest): Promise<Project> => {
    console.log('Creating project:', data);
    const response = await apiClient.post<Project>('/Projects', data);
    console.log('Project created:', response.data);
    return response.data;
  },

  update: async (id: string, data: CreateProjectRequest): Promise<Project> => {
    const response = await apiClient.put<Project>(`/Projects/${id}`, data);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/Projects/${id}`);
  },
};