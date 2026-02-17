/* eslint-disable @typescript-eslint/no-explicit-any */
'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { tasksApi } from '../api/tasks';
import { CreateTaskRequest, UpdateTaskRequest, Task, PaginatedResponse } from '../types';
import { useEffect } from 'react';
import { signalRService } from '../services/signalr';
import toast from 'react-hot-toast';

export const useTasks = (projectId: string) => {
  const queryClient = useQueryClient();

  const { data: tasksData, isLoading, error } = useQuery({
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
      queryClient.setQueryData<PaginatedResponse<Task>>(['tasks', projectId], (old) => {
        if (!old) return old;
        const exists = old.items.some(t => t.id === task.id);
        if (exists) return old;
        return { ...old, items: [...old.items, task], totalCount: old.totalCount + 1 };
      });
    };

    const handleTaskUpdated = (task: Task) => {
      console.log('SignalR: Task updated', task);
      queryClient.setQueryData<PaginatedResponse<Task>>(['tasks', projectId], (old) => {
        if (!old) return old;
        return { ...old, items: old.items.map((t) => (t.id === task.id ? task : t)) };
      });
    };

    const handleTaskDeleted = (taskId: string) => {
      console.log('SignalR: Task deleted', taskId);
      queryClient.setQueryData<PaginatedResponse<Task>>(['tasks', projectId], (old) => {
        if (!old) return old;
        return { ...old, items: old.items.filter((t) => t.id !== taskId), totalCount: old.totalCount - 1 };
      });
    };

    const handleTaskStatusChanged = (data: any) => {
      console.log('SignalR: Task status changed', data);
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
    onMutate: async (newTaskData) => {
      await queryClient.cancelQueries({ queryKey: ['tasks', projectId] });
      const previous = queryClient.getQueryData<PaginatedResponse<Task>>(['tasks', projectId]);
      const tempTask: Task = {
        id: `temp-${Date.now()}`,
        title: newTaskData.title,
        description: newTaskData.description || '',
        projectId: newTaskData.projectId,
        assignedToId: newTaskData.assignedToId,
        status: 0,
        priority: newTaskData.priority,
        createdAt: new Date().toISOString(),
        dueDate: newTaskData.dueDate,
      };
      queryClient.setQueryData<PaginatedResponse<Task>>(['tasks', projectId], (old) => {
        if (!old) return old;
        return { ...old, items: [...old.items, tempTask], totalCount: old.totalCount + 1 };
      });
      return { previous };
    },
    onError: (error, _, context) => {
      console.error('Failed to create task:', error);
      if (context?.previous) {
        queryClient.setQueryData(['tasks', projectId], context.previous);
      }
      toast.error('Failed to create task');
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', projectId] });
    },
  });

  const updateTask = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateTaskRequest }) => {
      console.log('Updating task:', id, data);
      return tasksApi.update(id, data);
    },
    onMutate: async ({ id, data }) => {
      await queryClient.cancelQueries({ queryKey: ['tasks', projectId] });
      const previous = queryClient.getQueryData<PaginatedResponse<Task>>(['tasks', projectId]);
      queryClient.setQueryData<PaginatedResponse<Task>>(['tasks', projectId], (old) => {
        if (!old) return old;
        return {
          ...old,
          items: old.items.map((t) => (t.id === id ? { ...t, ...data } : t)),
        };
      });
      return { previous };
    },
    onError: (error, _, context) => {
      console.error('Failed to update task:', error);
      if (context?.previous) {
        queryClient.setQueryData(['tasks', projectId], context.previous);
      }
      toast.error('Failed to update task');
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', projectId] });
    },
  });

  const deleteTask = useMutation({
    mutationFn: (taskId: string) => {
      console.log('Deleting task:', taskId);
      return tasksApi.delete(taskId);
    },
    onMutate: async (taskId) => {
      await queryClient.cancelQueries({ queryKey: ['tasks', projectId] });
      const previous = queryClient.getQueryData<PaginatedResponse<Task>>(['tasks', projectId]);
      queryClient.setQueryData<PaginatedResponse<Task>>(['tasks', projectId], (old) => {
        if (!old) return old;
        return { ...old, items: old.items.filter((t) => t.id !== taskId), totalCount: old.totalCount - 1 };
      });
      return { previous };
    },
    onError: (error, _, context) => {
      console.error('Failed to delete task:', error);
      if (context?.previous) {
        queryClient.setQueryData(['tasks', projectId], context.previous);
      }
      toast.error('Failed to delete task');
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', projectId] });
    },
  });

  return {
    tasks: tasksData?.items || [],
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
