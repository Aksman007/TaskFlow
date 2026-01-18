'use client';

import React from 'react';
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

interface CreateTaskModalProps {
  isOpen: boolean;
  onClose: () => void;
  projectId: string;
}

const taskSchema = z.object({
  title: z.string().min(3, 'Task title must be at least 3 characters'),
  description: z.string().optional(),
  priority: z.nativeEnum(TaskPriority),
  dueDate: z.string().optional(),
});

type TaskFormData = z.infer<typeof taskSchema>;

export const CreateTaskModal: React.FC<CreateTaskModalProps> = ({
  isOpen,
  onClose,
  projectId,
}) => {
  const { createTask } = useTasks(projectId);
  const [isSubmitting, setIsSubmitting] = React.useState(false);

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

  const onSubmit = async (data: TaskFormData) => {
    try {
      setIsSubmitting(true);
      await createTask({
        ...data,
        projectId,
      });
      reset();
      onClose();
    } catch (error) {
      console.error('Failed to create task:', error);
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

        <Select
          label="Priority"
          error={errors.priority?.message}
          options={[
            { value: TaskPriority.Low, label: 'Low' },
            { value: TaskPriority.Medium, label: 'Medium' },
            { value: TaskPriority.High, label: 'High' },
          ]}
          {...register('priority')}
        />

        <Input
          label="Due Date"
          type="date"
          error={errors.dueDate?.message}
          {...register('dueDate')}
        />

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