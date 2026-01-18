'use client';

import { useEffect, useRef } from 'react';
import { signalRService } from '../services/signalr';
import { useAuthStore } from '../store/authStore';

export const useSignalR = (projectId: string | undefined) => {
  const { token } = useAuthStore();
  const hasJoined = useRef(false);

  useEffect(() => {
    if (!projectId || !token) return;

    const initSignalR = async () => {
      try {
        await signalRService.connect(token);

        if (!hasJoined.current) {
          await signalRService.joinProject(projectId);
          hasJoined.current = true;
        }
      } catch (error) {
        console.error('SignalR initialization error:', error);
      }
    };

    initSignalR();

    return () => {
      if (projectId && hasJoined.current) {
        signalRService.leaveProject(projectId);
        hasJoined.current = false;
      }
    };
  }, [projectId, token]);

  return signalRService;
};