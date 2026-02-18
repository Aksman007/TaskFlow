'use client';

import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Modal } from '@/components/common/Modal';
import { Input } from '@/components/common/Input';
import { TextArea } from '@/components/common/TextArea';
import { Select } from '@/components/common/Select';
import { Button } from '@/components/common/Button';
import { useTasks } from '@/lib/hooks/useTasks';
import { TaskPriority } from '@/lib/types';
import { usersApi } from '@/lib/api/users';

interface CreateTaskModalProps {
  isOpen: boolean;
  onClose: () => void;
  projectId: string;
}

const taskSchema = z.object({
  title: z.string().min(3, 'Task title must be at least 3 characters'),
  description: z.string().optional(),
  priority: z.number().min(0).max(2),
  assignedToId: z.string().optional(),
  dueDate: z.string().optional(),
});

type TaskFormData = z.infer<typeof taskSchema>;

interface ProjectMember {
  userName: string;
  userEmail: string;
  userId: string;
  id: string;
}

export const CreateTaskModal: React.FC<CreateTaskModalProps> = ({
  isOpen,
  onClose,
  projectId,
}) => {
  const { createTask } = useTasks(projectId);
  const [isSubmitting, setIsSubmitting] = React.useState(false);
  const [projectMembers, setProjectMembers] = useState<ProjectMember[]>([]);
  const [loadingMembers, setLoadingMembers] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<TaskFormData>({
    resolver: zodResolver(taskSchema),
    defaultValues: {
      priority: TaskPriority.Medium,
    },
  });

  // Load project members when modal opens
  useEffect(() => {
    if (isOpen && projectId) {
      loadProjectMembers();
    }
  }, [isOpen, projectId]);

  const loadProjectMembers = async () => {
    try {
      setLoadingMembers(true);
      const members = await usersApi.getProjectMembers(projectId);
      setProjectMembers(members.map(m => ({
        id: m.id,
        userId: m.id,
        userName: m.fullName,
        userEmail: m.email,
      })));
    } catch (error) {
      console.error('Failed to load project members:', error);
    } finally {
      setLoadingMembers(false);
    }
  };

  const onSubmit = async (data: TaskFormData) => {
    try {
      setIsSubmitting(true);

      console.log('Submitting task with data:', {
        title: data.title,
        description: data.description,
        projectId: projectId,
        priority: Number(data.priority),
        assignedToId: data.assignedToId || undefined,
        dueDate: data.dueDate,
      });

      await createTask({
        title: data.title,
        description: data.description || undefined,
        projectId: projectId,
        priority: Number(data.priority) as TaskPriority,
        assignedToId: data.assignedToId || undefined,
        dueDate: data.dueDate || undefined,
      });

      console.log('Task created successfully');
      reset();
      onClose();
    } catch (error: any) {
      console.error('Failed to create task:', error);
      console.error('Error response:', error.response?.data);
      alert(`Failed to create task: ${error.response?.data?.title || error.message}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    reset();
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title="Create New Task">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
        <Input
          label="Task Title"
          placeholder="Enter task title"
          error={errors.title?.message}
          {...register('title')}
        />

        <TextArea
          label="Description"
          placeholder="What needs to be done?"
          error={errors.description?.message}
          {...register('description')}
          rows={4}
        />

        <div className="grid grid-cols-2 gap-4">
          <Select
            label="Priority"
            error={errors.priority?.message}
            options={[
              { value: '0', label: 'Low' },
              { value: '1', label: 'Medium' },
              { value: '2', label: 'High' },
            ]}
            {...register('priority')}
          />

          <Input
            label="Due Date"
            type="date"
            error={errors.dueDate?.message}
            {...register('dueDate')}
          />
        </div>

        <Select
          label="Assign To"
          error={errors.assignedToId?.message}
          options={[
            { value: '', label: 'Unassigned' },
            ...projectMembers.map(member => ({
              value: member.userId,
              label: `${member.userName} (${member.userEmail})`,
            })),
          ]}
          {...register('assignedToId')}
        />

        {loadingMembers && (
          <p className="text-sm text-gray-500 dark:text-gray-400">Loading team members...</p>
        )}

        <div className="flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={handleClose}>
            Cancel
          </Button>
          <Button type="submit" variant="primary" isLoading={isSubmitting}>
            Create Task
          </Button>
        </div>
      </form>
    </Modal>
  );
};