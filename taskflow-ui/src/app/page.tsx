'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuthStore } from '@/lib/store/authStore';
import { Spinner } from '@/components/common/Spinner';

export default function HomePage() {
  const router = useRouter();
  const { isAuthenticated } = useAuthStore();

  useEffect(() => {
    if (isAuthenticated) {
      router.push('/dashboard');
    } else {
      router.push('/login');
    }
  }, [router, isAuthenticated]);

  return (
    <div className="min-h-screen flex items-center justify-center">
      <Spinner size="xl" />
    </div>
  );
}