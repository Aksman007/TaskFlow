/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { tasksApi } from '../api/tasks';
import { CreateTaskRequest, UpdateTaskRequest, Task } from '../types';
import { useEffect } from 'react';
import { signalRService } from '../services/signalr';

export const useTasks = (projectId: string) => {
  const queryClient = useQueryClient();

  const { data: tasks, isLoading, error } = useQuery({
    queryKey: ['tasks', projectId],
    queryFn: () => {
      console.log('Fetching tasks for project:', projectId);
      return tasksApi.getProjectTasks(projectId);
    },
    enabled: !!projectId,
    retry: 1,
  });

  // Listen to SignalR events for real-time updates
  useEffect(() => {
    if (!projectId) return;

    console.log('Setting up SignalR listeners for project:', projectId);

    const handleTaskCreated = (task: Task) => {
      console.log('SignalR: Task created', task);
      queryClient.setQueryData(['tasks', projectId], (old: Task[] = []) => {
        // Avoid duplicates
        const exists = old.some(t => t.id === task.id);
        if (exists) return old;
        return [...old, task];
      });
    };

    const handleTaskUpdated = (task: Task) => {
      console.log('SignalR: Task updated', task);
      queryClient.setQueryData(['tasks', projectId], (old: Task[] = []) =>
        old.map((t) => (t.id === task.id ? task : t))
      );
    };

    const handleTaskDeleted = (taskId: string) => {
      console.log('SignalR: Task deleted', taskId);
      queryClient.setQueryData(['tasks', projectId], (old: Task[] = []) =>
        old.filter((t) => t.id !== taskId)
      );
    };

    const handleTaskStatusChanged = (data: any) => {
      console.log('SignalR: Task status changed', data);
      // Refetch to get updated task
      queryClient.invalidateQueries({ queryKey: ['tasks', projectId] });
    };

    signalRService.on('TaskCreated', handleTaskCreated);
    signalRService.on('TaskUpdated', handleTaskUpdated);
    signalRService.on('TaskDeleted', handleTaskDeleted);
    signalRService.on('TaskStatusChanged', handleTaskStatusChanged);

    return () => {
      console.log('Cleaning up SignalR listeners for project:', projectId);
      signalRService.off('TaskCreated', handleTaskCreated);
      signalRService.off('TaskUpdated', handleTaskUpdated);
      signalRService.off('TaskDeleted', handleTaskDeleted);
      signalRService.off('TaskStatusChanged', handleTaskStatusChanged);
    };
  }, [projectId, queryClient]);

  const createTask = useMutation({
    mutationFn: (data: CreateTaskRequest) => {
      console.log('Creating task:', data);
      return tasksApi.create(data);
    },
    onSuccess: (newTask) => {
      console.log('Task created successfully:', newTask);
      // Optimistically update the cache
      queryClient.setQueryData(['tasks', projectId], (old: Task[] = []) => {
        const exists = old.some(t => t.id === newTask.id);
        if (exists) return old;
        return [...old, newTask];
      });
    },
    onError: (error) => {
      console.error('Failed to create task:', error);
    },
  });

  const updateTask = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateTaskRequest }) => {
      console.log('Updating task:', id, data);
      return tasksApi.update(id, data);
    },
    onSuccess: (updatedTask) => {
      console.log('Task updated successfully:', updatedTask);
      // Optimistically update the cache
      queryClient.setQueryData(['tasks', projectId], (old: Task[] = []) =>
        old.map((t) => (t.id === updatedTask.id ? updatedTask : t))
      );
    },
    onError: (error) => {
      console.error('Failed to update task:', error);
    },
  });

  const deleteTask = useMutation({
    mutationFn: (taskId: string) => {
      console.log('Deleting task:', taskId);
      return tasksApi.delete(taskId);
    },
    onSuccess: (_, taskId) => {
      console.log('Task deleted successfully:', taskId);
      // Optimistically update the cache
      queryClient.setQueryData(['tasks', projectId], (old: Task[] = []) =>
        old.filter((t) => t.id !== taskId)
      );
    },
    onError: (error) => {
      console.error('Failed to delete task:', error);
    },
  });

  return {
    tasks: tasks || [],
    isLoading,
    error,
    createTask: createTask.mutateAsync,
    updateTask: updateTask.mutateAsync,
    deleteTask: deleteTask.mutateAsync,
    isCreating: createTask.isPending,
    isUpdating: updateTask.isPending,
    isDeleting: deleteTask.isPending,
  };
};