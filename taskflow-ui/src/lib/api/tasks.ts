import apiClient from './client';
import { Task, CreateTaskRequest, UpdateTaskRequest } from '../types';

export const tasksApi = {
  getProjectTasks: async (projectId: string): Promise<Task[]> => {
    const response = await apiClient.get<Task[]>(`/tasks/project/${projectId}`);
    return response.data;
  },

  getById: async (id: string): Promise<Task> => {
    const response = await apiClient.get<Task>(`/tasks/${id}`);
    return response.data;
  },

  create: async (data: CreateTaskRequest): Promise<Task> => {
    const response = await apiClient.post<Task>('/tasks', data);
    return response.data;
  },

  update: async (id: string, data: UpdateTaskRequest): Promise<Task> => {
    const response = await apiClient.put<Task>(`/tasks/${id}`, data);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/tasks/${id}`);
  },
};