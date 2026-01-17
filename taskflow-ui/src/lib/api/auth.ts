import apiClient from './client';
import { AuthResult, LoginRequest, RegisterRequest, User } from '../types';

export const authApi = {
  register: async (data: RegisterRequest): Promise<AuthResult> => {
    const response = await apiClient.post<AuthResult>('/auth/register', data);
    return response.data;
  },

  login: async (data: LoginRequest): Promise<AuthResult> => {
    const response = await apiClient.post<AuthResult>('/auth/login', data);
    return response.data;
  },

  getCurrentUser: async (): Promise<User> => {
    const response = await apiClient.get<User>('/auth/me');
    return response.data;
  },
};