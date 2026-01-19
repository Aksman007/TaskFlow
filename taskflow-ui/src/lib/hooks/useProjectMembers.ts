'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { projectsApi } from '../api/projects';

export interface ProjectMember {
  id: string;
  userId: string;
    userName: string;
    userEmail: string;
  projectId: string;
  role: number;
  joinedAt: string;
}

export const useProjectMembers = (projectId: string) => {
  const queryClient = useQueryClient();

  const { data: members, isLoading, error } = useQuery({
    queryKey: ['projectMembers', projectId],
    queryFn: () => projectsApi.getMembers(projectId),
    enabled: !!projectId,
  });

  const addMember = useMutation({
    mutationFn: ({ email, role }: { email: string; role: number }) =>
      projectsApi.addMember(projectId, email, role),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projectMembers', projectId] });
    },
  });

  const removeMember = useMutation({
    mutationFn: (memberId: string) =>
      projectsApi.removeMember(projectId, memberId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projectMembers', projectId] });
    },
  });

  const updateMemberRole = useMutation({
    mutationFn: ({ memberId, role }: { memberId: string; role: number }) =>
      projectsApi.updateMemberRole(projectId, memberId, role),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projectMembers', projectId] });
    },
  });

  return {
    members: members || [],
    isLoading,
    error,
    addMember: (email: string, role: number) =>
      addMember.mutateAsync({ email, role }),
    removeMember: removeMember.mutateAsync,
    updateMemberRole: (memberId: string, role: number) =>
      updateMemberRole.mutateAsync({ memberId, role }),
  };
};