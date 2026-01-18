'use client';

import React from 'react';
import { Task, TaskPriority } from '@/lib/types';
import { format } from 'date-fns';
import {
  CalendarIcon,
  UserCircleIcon,
  FlagIcon,
} from '@heroicons/react/24/outline';
import clsx from 'clsx';

interface TaskCardProps {
  task: Task;
  onClick: () => void;
}

export const TaskCard: React.FC<TaskCardProps> = ({ task, onClick }) => {
  const priorityColors = {
    [TaskPriority.Low]: 'text-green-600 bg-green-50',
    [TaskPriority.Medium]: 'text-yellow-600 bg-yellow-50',
    [TaskPriority.High]: 'text-red-600 bg-red-50',
  };

  const handleDragStart = (e: React.DragEvent) => {
    e.dataTransfer.setData('taskId', task.id);
  };

  return (
    <div
      draggable
      onDragStart={handleDragStart}
      onClick={onClick}
      className="task-card"
    >
      {/* Priority & Title */}
      <div className="flex items-start justify-between mb-2">
        <h4 className="font-medium text-gray-900 flex-1 line-clamp-2">
          {task.title}
        </h4>
        <span
          className={clsx(
            'ml-2 p-1 rounded',
            priorityColors[task.priority]
          )}
        >
          <FlagIcon className="h-4 w-4" />
        </span>
      </div>

      {/* Description */}
      {task.description && (
        <p className="text-sm text-gray-600 mb-3 line-clamp-2">
          {task.description}
        </p>
      )}

      {/* Footer */}
      <div className="flex items-center justify-between text-xs text-gray-500">
        {task.assignedToName && (
          <div className="flex items-center gap-1">
            <UserCircleIcon className="h-4 w-4" />
            <span className="truncate max-w-[100px]">{task.assignedToName}</span>
          </div>
        )}

        {task.dueDate && (
          <div className="flex items-center gap-1">
            <CalendarIcon className="h-4 w-4" />
            <span>{format(new Date(task.dueDate), 'MMM d')}</span>
          </div>
        )}
      </div>
    </div>
  );
};