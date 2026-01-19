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
    const response = await apiClient.get<Project>(`/Projects/${id}`);
    console.log('Project fetched:', response.data);
    return response.data;
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

  // Member management
  getMembers: async (projectId: string): Promise<any[]> => {
    console.log('Fetching project members:', projectId);
    const response = await apiClient.get(`/Projects/${projectId}/members`);
    console.log('Members fetched:', response.data);
    return response.data;
  },

  addMember: async (
    projectId: string,
    email: string,
    role: number
  ): Promise<void> => {
    console.log('Adding member:', { projectId, email, role });
    await apiClient.post(`/Projects/${projectId}/members`, { email, role });
  },

  removeMember: async (projectId: string, memberId: string): Promise<void> => {
    console.log('Removing member:', { projectId, memberId });
    await apiClient.delete(`/Projects/${projectId}/members/${memberId}`);
  },

  updateMemberRole: async (
    projectId: string,
    memberId: string,
    role: number
  ): Promise<void> => {
    console.log('Updating member role:', { projectId, memberId, role });
    await apiClient.put(`/Projects/${projectId}/members/${memberId}`, { role });
  },
};