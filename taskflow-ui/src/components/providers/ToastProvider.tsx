'use client';

import { Toaster } from 'react-hot-toast';
import { useThemeStore } from '@/lib/store/themeStore';

export const ToastProvider = () => {
  const { theme } = useThemeStore();

  const isDark =
    theme === 'dark' ||
    (theme === 'system' &&
      typeof window !== 'undefined' &&
      window.matchMedia('(prefers-color-scheme: dark)').matches);

  return (
    <Toaster
      position="bottom-right"
      toastOptions={{
        style: isDark
          ? {
              background: '#1f2937',
              color: '#f3f4f6',
              border: '1px solid #374151',
            }
          : undefined,
      }}
    />
  );
};
