import apiClient from './client';
import { AuthResult, LoginRequest, RegisterRequest, User } from '../types';

export const authApi = {
  register: async (data: RegisterRequest): Promise<AuthResult> => {
    const response = await apiClient.post<AuthResult>('/Auth/register', data);
    return response.data;
  },

  login: async (data: LoginRequest): Promise<AuthResult> => {
    const response = await apiClient.post<AuthResult>('/Auth/login', data);
    return response.data;
  },

  getCurrentUser: async (): Promise<User> => {
    const response = await apiClient.get<User>('/Auth/me');
    return response.data;
  },
};