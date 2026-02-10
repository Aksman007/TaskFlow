'use client';

import React, { useState, useEffect } from 'react';
import { Task, TaskStatus, TaskPriority } from '@/lib/types';
import { Modal } from '@/components/common/Modal';
import { Button } from '@/components/common/Button';
import { Select } from '@/components/common/Select';
import { CommentSection } from '@/components/comments/CommentSection';
import { useTasks } from '@/lib/hooks/useTasks';
import { usersApi } from '@/lib/api/users';
import { TrashIcon, PencilIcon } from '@heroicons/react/24/outline';
import { format } from 'date-fns';

interface TaskModalProps {
  task: Task;
  isOpen: boolean;
  onClose: () => void;
}

interface ProjectMember {
  id: string;
  userName: string;
    userId: string;
    userEmail: string;
}

export const TaskModal: React.FC<TaskModalProps> = ({
  task,
  isOpen,
  onClose,
}) => {
  const { updateTask, deleteTask } = useTasks(task.projectId);
  const [isEditing, setIsEditing] = useState(false);
  const [status, setStatus] = useState(task.status);
  const [priority, setPriority] = useState(task.priority);
  const [assignedToId, setAssignedToId] = useState(task.assignedToId || '');
  const [projectMembers, setProjectMembers] = useState<ProjectMember[]>([]);
  const [loadingMembers, setLoadingMembers] = useState(false);

  // Load project members when modal opens
  useEffect(() => {
    if (isOpen && task.projectId) {
      loadProjectMembers();
    }
  }, [isOpen, task.projectId]);

  const loadProjectMembers = async () => {
    try {
      setLoadingMembers(true);
      const members = await usersApi.getProjectMembers(task.projectId);
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

  const handleUpdateStatus = async (newStatus: TaskStatus) => {
    try {
      await updateTask({
        id: task.id,
        data: {
          title: task.title,
          description: task.description,
          status: newStatus,
          priority: task.priority,
          assignedToId: task.assignedToId,
          dueDate: task.dueDate,
        },
      });
      setStatus(newStatus);
    } catch (error) {
      console.error('Failed to update task:', error);
    }
  };

  const handleUpdatePriority = async (newPriority: TaskPriority) => {
    try {
      await updateTask({
        id: task.id,
        data: {
          title: task.title,
          description: task.description,
          status: task.status,
          priority: newPriority,
          assignedToId: task.assignedToId,
          dueDate: task.dueDate,
        },
      });
      setPriority(newPriority);
    } catch (error) {
      console.error('Failed to update task:', error);
    }
  };

  const handleUpdateAssignee = async (newAssignedToId: string) => {
    try {
      await updateTask({
        id: task.id,
        data: {
          title: task.title,
          description: task.description,
          status: task.status,
          priority: task.priority,
          assignedToId: newAssignedToId || undefined,
          dueDate: task.dueDate,
        },
      });
      setAssignedToId(newAssignedToId);
    } catch (error) {
      console.error('Failed to update task:', error);
    }
  };

  const handleDelete = async () => {
    if (!confirm('Are you sure you want to delete this task?')) return;

    try {
      await deleteTask(task.id);
      onClose();
    } catch (error) {
      console.error('Failed to delete task:', error);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={task.title} size="xl">
      <div className="space-y-6">
        {/* Actions */}
        <div className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setIsEditing(!isEditing)}
            className="gap-2"
          >
            <PencilIcon className="h-4 w-4" />
            Edit
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={handleDelete}
            className="gap-2 text-red-600 hover:bg-red-50"
          >
            <TrashIcon className="h-4 w-4" />
            Delete
          </Button>
        </div>

        {/* Description */}
        <div>
          <h4 className="font-medium text-gray-900 mb-2">Description</h4>
          <p className="text-gray-600">
            {task.description || 'No description provided'}
          </p>
        </div>

        {/* Status, Priority, Assignee */}
        <div className="grid grid-cols-3 gap-4">
          <Select
            label="Status"
            value={status.toString()}
            onChange={(e) => handleUpdateStatus(parseInt(e.target.value) as TaskStatus)}
            options={[
              { value: TaskStatus.Todo.toString(), label: 'To Do' },
              { value: TaskStatus.InProgress.toString(), label: 'In Progress' },
              { value: TaskStatus.Done.toString(), label: 'Done' },
            ]}
          />

          <Select
            label="Priority"
            value={priority.toString()}
            onChange={(e) => handleUpdatePriority(parseInt(e.target.value) as TaskPriority)}
            options={[
              { value: TaskPriority.Low.toString(), label: 'Low' },
              { value: TaskPriority.Medium.toString(), label: 'Medium' },
              { value: TaskPriority.High.toString(), label: 'High' },
            ]}
          />

          <Select
            label="Assign To"
            value={assignedToId}
            onChange={(e) => handleUpdateAssignee(e.target.value)}
            options={[
              { value: '', label: 'Unassigned' },
              ...projectMembers.map(member => ({
                value: member.userId,
                label: member.userName,
              })),
            ]}
          />
        </div>

        {/* Metadata */}
        <div className="border-t border-gray-200 pt-4 space-y-2 text-sm">
          {task.assignedToName && (
            <p className="text-gray-600">
              <span className="font-medium">Assigned to:</span> {task.assignedToName}
            </p>
          )}
          {task.dueDate && (
            <p className="text-gray-600">
              <span className="font-medium">Due date:</span>{' '}
              {format(new Date(task.dueDate), 'PPP')}
            </p>
          )}
          <p className="text-gray-600">
            <span className="font-medium">Created:</span>{' '}
            {format(new Date(task.createdAt), 'PPP')}
          </p>
        </div>

        {/* Comments Section */}
        <div className="border-t border-gray-200 pt-6">
          <CommentSection taskId={task.id} projectId={task.projectId} />
        </div>
      </div>
    </Modal>
  );
};