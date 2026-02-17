/* eslint-disable @typescript-eslint/no-explicit-any */
import apiClient from './client';
import { Task, CreateTaskRequest, UpdateTaskRequest, PaginatedResponse } from '../types';

export const tasksApi = {
  getProjectTasks: async (projectId: string): Promise<PaginatedResponse<Task>> => {
    console.log('API: Fetching tasks for project:', projectId);
    try {
      const response = await apiClient.get<PaginatedResponse<Task>>(`/Tasks/project/${projectId}`);
      console.log('API: Tasks fetched:', response.data.items.length, 'tasks');
      return response.data;
    } catch (error: any) {
      console.error('API: Error fetching tasks:', {
        projectId,
        status: error.response?.status,
        message: error.message,
        data: error.response?.data,
      });
      throw error;
    }
  },

  getById: async (id: string): Promise<Task> => {
    console.log('API: Fetching task by ID:', id);
    const response = await apiClient.get<Task>(`/Tasks/${id}`);
    console.log('API: Task fetched:', response.data);
    return response.data;
  },

  create: async (data: CreateTaskRequest): Promise<Task> => {
    console.log('API: Creating task with data:', data);
    try {
      const response = await apiClient.post<Task>('/Tasks', data);
      console.log('API: Task created:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('API: Error creating task:', {
        data,
        status: error.response?.status,
        message: error.message,
        responseData: error.response?.data,
      });
      throw error;
    }
  },

  update: async (id: string, data: UpdateTaskRequest): Promise<Task> => {
    console.log('API: Updating task:', id, 'with data:', data);
    try {
      const response = await apiClient.put<Task>(`/Tasks/${id}`, data);
      console.log('API: Task updated:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('API: Error updating task:', {
        id,
        data,
        status: error.response?.status,
        message: error.message,
        responseData: error.response?.data,
      });
      throw error;
    }
  },

  delete: async (id: string): Promise<void> => {
    console.log('API: Deleting task:', id);
    try {
      await apiClient.delete(`/Tasks/${id}`);
      console.log('API: Task deleted successfully');
    } catch (error: any) {
      console.error('API: Error deleting task:', {
        id,
        status: error.response?.status,
        message: error.message,
        responseData: error.response?.data,
      });
      throw error;
    }
  },
};