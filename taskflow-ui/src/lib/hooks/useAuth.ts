'use client';

import { useAuthStore } from '../store/authStore';
import { authApi } from '../api/auth';
import { LoginRequest, RegisterRequest } from '../types';
import { signalRService } from '../services/signalr';
import { useRouter } from 'next/navigation';

export const useAuth = () => {
  const router = useRouter();
  const { user, isAuthenticated, setAuth, clearAuth } = useAuthStore();

  const login = async (data: LoginRequest) => {
    const result = await authApi.login(data);
    if (result.success && result.user) {
      setAuth(result.user);

      // Connect to SignalR (cookies handle auth automatically)
      try {
        await signalRService.connect();
      } catch (error) {
        console.error('SignalR connection error:', error);
      }

      if (typeof window !== 'undefined') {
        window.location.href = '/';
      }

      return result;
    }
    throw new Error(result.error || 'Login failed');
  };

  const register = async (data: RegisterRequest) => {
    const result = await authApi.register(data);
    if (result.success && result.user) {
      setAuth(result.user);

      // Connect to SignalR
      try {
        await signalRService.connect();
      } catch (error) {
        console.error('SignalR connection error:', error);
      }

      router.push('/');
      router.refresh();

      return result;
    }
    throw new Error(result.error || 'Registration failed');
  };

  const logout = async () => {
    try {
      await signalRService.disconnect();
      await authApi.logout();
    } catch {
      // Ignore errors during logout
    }
    clearAuth();
    router.push('/login');
    router.refresh();
  };

  return {
    user,
    isAuthenticated,
    login,
    register,
    logout,
  };
};
