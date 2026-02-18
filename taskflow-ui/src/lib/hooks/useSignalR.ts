'use client';

import { useEffect, useRef, useCallback } from 'react';
import { signalRService } from '../services/signalr';
import { useAuthStore } from '../store/authStore';

const RETRY_DELAY_MS = 3000;
const MAX_RETRIES = 3;

export const useSignalR = (projectId: string | undefined) => {
  const { isAuthenticated, _hasHydrated } = useAuthStore();
  const hasJoined = useRef(false);
  const retryTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const cancelledRef = useRef(false);

  const connectWithRetry = useCallback(async () => {
    let attempt = 0;

    while (attempt < MAX_RETRIES && !cancelledRef.current) {
      try {
        await signalRService.connect();

        if (!cancelledRef.current && !hasJoined.current && projectId) {
          await signalRService.joinProject(projectId);
          hasJoined.current = true;
        }
        return; // success
      } catch (error) {
        attempt++;
        if (attempt < MAX_RETRIES && !cancelledRef.current) {
          console.warn(
            `SignalR connection attempt ${attempt} failed, retrying in ${RETRY_DELAY_MS}ms...`,
            error
          );
          await new Promise<void>((resolve) => {
            retryTimeoutRef.current = setTimeout(resolve, RETRY_DELAY_MS);
          });
        } else if (!cancelledRef.current) {
          console.error(
            'SignalR failed to connect after maximum retries. Real-time updates will be unavailable.',
            error
          );
        }
      }
    }
  }, [projectId]);

  useEffect(() => {
    // Wait until Zustand has rehydrated from localStorage before attempting connection
    if (!_hasHydrated || !projectId || !isAuthenticated) return;

    cancelledRef.current = false;
    connectWithRetry();

    return () => {
      cancelledRef.current = true;
      if (retryTimeoutRef.current) {
        clearTimeout(retryTimeoutRef.current);
        retryTimeoutRef.current = null;
      }
      if (projectId && hasJoined.current) {
        signalRService.leaveProject(projectId);
        hasJoined.current = false;
      }
    };
  }, [projectId, isAuthenticated, _hasHydrated, connectWithRetry]);

  return signalRService;
};
