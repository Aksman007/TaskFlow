'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { tasksApi } from '../api/tasks';
import { CreateTaskRequest, UpdateTaskRequest, Task } from '../types';
import { useEffect } from 'react';
import { signalRService } from '../services/signalr';

export const useTasks = (projectId: string) => {
  const queryClient = useQueryClient();

  const { data: tasks, isLoading } = useQuery({
    queryKey: ['tasks', projectId],
    queryFn: () => tasksApi.getProjectTasks(projectId),
    enabled: !!projectId,
  });

  // Listen to SignalR events
  useEffect(() => {
    const handleTaskCreated = (task: Task) => {
      queryClient.setQueryData(['tasks', projectId], (old: Task[] = []) => [
        ...old,
        task,
      ]);
    };

    const handleTaskUpdated = (task: Task) => {
      queryClient.setQueryData(['tasks', projectId], (old: Task[] = []) =>
        old.map((t) => (t.id === task.id ? task : t))
      );
    };

    const handleTaskDeleted = (taskId: string) => {
      queryClient.setQueryData(['tasks', projectId], (old: Task[] = []) =>
        old.filter((t) => t.id !== taskId)
      );
    };

    signalRService.on('TaskCreated', handleTaskCreated);
    signalRService.on('TaskUpdated', handleTaskUpdated);
    signalRService.on('TaskDeleted', handleTaskDeleted);

    return () => {
      signalRService.off('TaskCreated', handleTaskCreated);
      signalRService.off('TaskUpdated', handleTaskUpdated);
      signalRService.off('TaskDeleted', handleTaskDeleted);
    };
  }, [projectId, queryClient]);

  const createTask = useMutation({
    mutationFn: tasksApi.create,
  });

  const updateTask = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateTaskRequest }) =>
      tasksApi.update(id, data),
  });

  const deleteTask = useMutation({
    mutationFn: tasksApi.delete,
  });

  return {
    tasks: tasks || [],
    isLoading,
    createTask: createTask.mutateAsync,
    updateTask: updateTask.mutateAsync,
    deleteTask: deleteTask.mutateAsync,
  };
};