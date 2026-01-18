'use client';

import React, { useState } from 'react';
import { Task, TaskStatus } from '@/lib/types';
import { TaskColumn } from './TaskColumn';
import { CreateTaskModal } from './CreateTaskModal';
import { TaskModal } from './TaskModal';

interface TaskBoardProps {
  projectId: string;
  tasks: Task[];
}

export const TaskBoard: React.FC<TaskBoardProps> = ({ projectId, tasks }) => {
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [selectedTask, setSelectedTask] = useState<Task | null>(null);

  const columns: { status: TaskStatus; title: string; color: string }[] = [
    { status: TaskStatus.Todo, title: 'To Do', color: 'bg-gray-100' },
    { status: TaskStatus.InProgress, title: 'In Progress', color: 'bg-blue-100' },
    { status: TaskStatus.Done, title: 'Done', color: 'bg-green-100' },
  ];

  const getTasksByStatus = (status: TaskStatus) => {
    return tasks.filter((task) => task.status === status);
  };

  return (
    <>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {columns.map((column) => (
          <TaskColumn
            key={column.status}
            status={column.status}
            title={column.title}
            color={column.color}
            tasks={getTasksByStatus(column.status)}
            onTaskClick={setSelectedTask}
            onCreateTask={() => setIsCreateModalOpen(true)}
          />
        ))}
      </div>

      {/* Create Task Modal */}
      <CreateTaskModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        projectId={projectId}
      />

      {/* Task Detail Modal */}
      {selectedTask && (
        <TaskModal
          task={selectedTask}
          isOpen={!!selectedTask}
          onClose={() => setSelectedTask(null)}
        />
      )}
    </>
  );
};