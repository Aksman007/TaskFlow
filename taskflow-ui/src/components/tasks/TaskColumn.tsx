'use client';

import React from 'react';
import { Task, TaskStatus } from '@/lib/types';
import { TaskCard } from './TaskCard';
import { PlusIcon } from '@heroicons/react/24/outline';

interface TaskColumnProps {
  status: TaskStatus;
  title: string;
  color: string;
  tasks: Task[];
  allTasks: Task[];  // Add this - all tasks from all columns
  onTaskClick: (task: Task) => void;
  onCreateTask: () => void;
  onTaskStatusChange: (taskId: string, newStatus: TaskStatus) => void;  // Add this
}

export const TaskColumn: React.FC<TaskColumnProps> = ({
  status,
  title,
  color,
  tasks,
  allTasks,
  onTaskClick,
  onCreateTask,
  onTaskStatusChange,
}) => {
  const [isDraggingOver, setIsDraggingOver] = React.useState(false);

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDraggingOver(true);
  };

  const handleDragLeave = () => {
    setIsDraggingOver(false);
  };

  const handleDrop = async (e: React.DragEvent) => {
    e.preventDefault();
    setIsDraggingOver(false);

    const taskId = e.dataTransfer.getData('taskId');
    
    // Find the task from all tasks
    const draggedTask = allTasks.find((t) => t.id === taskId);

    if (!draggedTask) {
      console.error('Task not found:', taskId);
      return;
    }

    // Don't update if dropping in same column
    if (draggedTask.status === status) {
      console.log('Task already in this column');
      return;
    }

    console.log('Dropping task:', taskId, 'to status:', status);
    
    // Call the callback to update task status
    onTaskStatusChange(taskId, status);
  };

  return (
    <div
      className={`rounded-lg p-4 ${color} transition-colors ${
        isDraggingOver ? 'ring-2 ring-primary-500' : ''
      }`}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
    >
      {/* Column Header */}
      <div className="flex items-center justify-between mb-4">
        <h3 className="font-semibold text-gray-900">{title}</h3>
        <span className="bg-white px-2 py-1 rounded-full text-sm font-medium text-gray-700">
          {tasks.length}
        </span>
      </div>

      {/* Tasks */}
      <div className="space-y-3 min-h-[200px]">
        {tasks.map((task) => (
          <TaskCard
            key={task.id}
            task={task}
            onClick={() => onTaskClick(task)}
          />
        ))}

        {/* Add Task Button */}
        {status === TaskStatus.Todo && (
          <button
            onClick={onCreateTask}
            className="w-full p-3 border-2 border-dashed border-gray-300 rounded-lg hover:border-primary-400 hover:bg-white transition-all flex items-center justify-center gap-2 text-gray-600 hover:text-primary-600"
          >
            <PlusIcon className="h-5 w-5" />
            <span className="font-medium">Add Task</span>
          </button>
        )}
      </div>
    </div>
  );
};