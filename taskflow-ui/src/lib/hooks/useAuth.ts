'use client';

import { useAuthStore } from '../store/authStore';
import { authApi } from '../api/auth';
import { LoginRequest, RegisterRequest } from '../types';
import { signalRService } from '../services/signalr';
import { useRouter } from 'next/navigation';

export const useAuth = () => {
  const router = useRouter();
  const { user, token, isAuthenticated, setAuth, clearAuth } = useAuthStore();

  const login = async (data: LoginRequest) => {
    const result = await authApi.login(data);
    if (result.success && result.user && result.token) {
      setAuth(result.user, result.token);
      localStorage.setItem('token', result.token);
      await signalRService.connect(result.token);
      router.push('/');
      return result;
    }
    throw new Error(result.error || 'Login failed');
  };

  const register = async (data: RegisterRequest) => {
    const result = await authApi.register(data);
    if (result.success && result.user && result.token) {
      setAuth(result.user, result.token);
      localStorage.setItem('token', result.token);
      await signalRService.connect(result.token);
      router.push('/');
      return result;
    }
    throw new Error(result.error || 'Registration failed');
  };

  const logout = async () => {
    await signalRService.disconnect();
    localStorage.removeItem('token');
    clearAuth();
    router.push('/login');
  };

  return {
    user,
    token,
    isAuthenticated,
    login,
    register,
    logout,
  };
};