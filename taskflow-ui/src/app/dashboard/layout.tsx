'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuthStore } from '@/lib/store/authStore';
import { authApi } from '@/lib/api/auth';
import { Navbar } from '@/components/layout/Navbar';
import { Sidebar } from '@/components/layout/Sidebar';
import { Spinner } from '@/components/common/Spinner';
import { OfflineBanner } from '@/components/common/OfflineBanner';

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const { isAuthenticated, _hasHydrated, clearAuth } = useAuthStore();

  useEffect(() => {
    // Wait for Zustand to hydrate from localStorage before checking auth
    if (!_hasHydrated) return;

    if (!isAuthenticated) {
      router.push('/login');
    }
  }, [router, isAuthenticated, _hasHydrated]);

  // Validate session against server on mount
  useEffect(() => {
    if (!_hasHydrated || !isAuthenticated) return;

    authApi.getCurrentUser().catch(() => {
      clearAuth();
      router.push('/login');
    });
  }, [_hasHydrated, isAuthenticated, clearAuth, router]);

  // Show spinner while Zustand is hydrating from localStorage
  if (!_hasHydrated) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <Spinner size="xl" />
      </div>
    );
  }

  // Don't render dashboard content if not authenticated
  if (!isAuthenticated) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <Spinner size="xl" />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar onMenuClick={() => setIsSidebarOpen(true)} />
      <div className="flex">
        <Sidebar
          isOpen={isSidebarOpen}
          onClose={() => setIsSidebarOpen(false)}
        />
        <main className="flex-1">
          {children}
        </main>
      </div>
      <OfflineBanner />
    </div>
  );
}
