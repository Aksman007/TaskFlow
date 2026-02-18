'use client';

import React from 'react';
import { useNetworkStatus } from '@/lib/hooks/useNetworkStatus';
import { WifiIcon, ExclamationTriangleIcon } from '@heroicons/react/24/outline';

export const OfflineBanner: React.FC = () => {
  const { isOnline, wasOffline } = useNetworkStatus();

  if (isOnline && !wasOffline) return null;

  return (
    <div
      className={`fixed bottom-0 left-0 right-0 z-50 px-4 py-3 text-center text-sm font-medium transition-colors ${
        isOnline
          ? 'bg-green-500 text-white dark:bg-green-600'
          : 'bg-yellow-500 text-yellow-900 dark:bg-yellow-600 dark:text-yellow-100'
      }`}
    >
      <div className="flex items-center justify-center gap-2">
        {isOnline ? (
          <>
            <WifiIcon className="h-4 w-4" />
            Back online
          </>
        ) : (
          <>
            <ExclamationTriangleIcon className="h-4 w-4" />
            You are offline. Some features may not work.
          </>
        )}
      </div>
    </div>
  );
};
