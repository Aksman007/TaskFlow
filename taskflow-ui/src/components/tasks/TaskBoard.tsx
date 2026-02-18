'use client';

import React, { useState } from 'react';
import { Task, TaskStatus } from '@/lib/types';
import { TaskColumn } from './TaskColumn';
import { CreateTaskModal } from './CreateTaskModal';
import { TaskModal } from './TaskModal';
import { useTasks } from '@/lib/hooks/useTasks';

interface TaskBoardProps {
  projectId: string;
  tasks: Task[];
}

export const TaskBoard: React.FC<TaskBoardProps> = ({ projectId, tasks }) => {
  const { updateTask } = useTasks(projectId);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [selectedTask, setSelectedTask] = useState<Task | null>(null);

  const columns: { status: TaskStatus; title: string; color: string }[] = [
    { status: TaskStatus.Todo, title: 'To Do', color: 'bg-gray-100 dark:bg-gray-800' },
    { status: TaskStatus.InProgress, title: 'In Progress', color: 'bg-blue-100 dark:bg-blue-900/30' },
    { status: TaskStatus.Done, title: 'Done', color: 'bg-green-100 dark:bg-green-900/30' },
  ];

  const getTasksByStatus = (status: TaskStatus) => {
    return tasks.filter((task) => task.status === status);
  };

  const handleTaskStatusChange = async (taskId: string, newStatus: TaskStatus) => {
    const task = tasks.find(t => t.id === taskId);
    
    if (!task) {
      console.error('Task not found for status change:', taskId);
      return;
    }

    console.log('Updating task status:', {
      taskId,
      currentStatus: task.status,
      newStatus,
      task,
    });

    await updateTask({
      id: taskId,
      data: {
        title: task.title,
        description: task.description || undefined,
        status: newStatus,
        priority: task.priority,
        assignedToId: task.assignedToId || undefined,
        dueDate: task.dueDate || undefined,
      },
    });
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
            allTasks={tasks}
            onTaskClick={setSelectedTask}
            onCreateTask={() => setIsCreateModalOpen(true)}
            onTaskStatusChange={handleTaskStatusChange}
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