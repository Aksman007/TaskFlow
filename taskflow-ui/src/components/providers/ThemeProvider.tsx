'use client';

import { useEffect } from 'react';
import { useThemeStore } from '@/lib/store/themeStore';

export const ThemeProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { theme, _hasHydrated } = useThemeStore();

  useEffect(() => {
    if (!_hasHydrated) return;

    const root = document.documentElement;

    if (theme === 'system') {
      const mql = window.matchMedia('(prefers-color-scheme: dark)');
      const apply = () => {
        root.classList.toggle('dark', mql.matches);
      };
      apply();
      mql.addEventListener('change', apply);
      return () => mql.removeEventListener('change', apply);
    }

    root.classList.toggle('dark', theme === 'dark');
  }, [theme, _hasHydrated]);

  return <>{children}</>;
};
