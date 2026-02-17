/* eslint-disable react-hooks/set-state-in-effect */
'use client';

import { use } from 'react';
import { useRouter } from 'next/navigation';
import { useProject } from '@/lib/hooks/useProjects';
import { useTasks } from '@/lib/hooks/useTasks';
import { useSignalR } from '@/lib/hooks/useSignalR';
import { TaskBoard } from '@/components/tasks/TaskBoard';
import { TaskBoardSkeleton } from '@/components/common/skeletons/TaskBoardSkeleton';
import { Skeleton } from '@/components/common/Skeleton';
import { Button } from '@/components/common/Button';
import { ArrowLeftIcon, UserGroupIcon } from '@heroicons/react/24/outline';

interface PageProps {
  params: Promise<{ id: string }>;
}

export default function ProjectPage({ params }: PageProps) {
  const router = useRouter();
  const { id } = use(params);
  const { data: project, isLoading: projectLoading, error: projectError } = useProject(id);
  const { tasks, isLoading: tasksLoading, error: tasksError } = useTasks(id);

  // Connect to SignalR for real-time updates
  useSignalR(id);

  if (projectLoading || tasksLoading) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="mb-8">
          <Skeleton className="h-8 w-32 mb-4" />
          <Skeleton className="h-10 w-64 mb-2" />
          <Skeleton className="h-5 w-96 mb-4" />
          <div className="flex gap-4">
            <Skeleton className="h-4 w-24" />
            <Skeleton className="h-4 w-20" />
          </div>
        </div>
        <TaskBoardSkeleton />
      </div>
    );
  }

  if (projectError) {
    console.error('Project error details:', projectError);
    return (
      <div className="text-center py-12">
        <h2 className="text-2xl font-bold text-gray-900 mb-4">
          Error loading project
        </h2>
        <p className="text-red-600 mb-4">
          {(projectError as any)?.response?.data?.error || (projectError as any)?.message || 'Unknown error'}
        </p>
        <p className="text-sm text-gray-600 mb-4">Project ID: {id}</p>
        <Button onClick={() => router.push('/dashboard')}>
          Back to Dashboard
        </Button>
      </div>
    );
  }

  if (!project) {
    return (
      <div className="text-center py-12">
        <h2 className="text-2xl font-bold text-gray-900 mb-4">
          Project not found
        </h2>
        <p className="text-gray-600 mb-4">Project ID: {id}</p>
        <Button onClick={() => router.push('/dashboard')}>
          Back to Dashboard
        </Button>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Header */}
      <div className="mb-8">
        <Button
          variant="ghost"
          onClick={() => router.push('/dashboard')}
          className="mb-4 gap-2"
        >
          <ArrowLeftIcon className="h-4 w-4" />
          Back to Projects
        </Button>
        
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">{project.name}</h1>
            <p className="text-gray-600 mt-2">{project.description}</p>
            <div className="flex gap-4 mt-4 text-sm text-gray-600">
              <span>{project.memberCount} members</span>
              <span>•</span>
              <span>{project.taskCount} tasks</span>
            </div>
          </div>

          <Button
            variant="secondary"
            onClick={() => router.push(`/dashboard/projects/${id}/members`)}
            className="gap-2"
          >
            <UserGroupIcon className="h-5 w-5" />
            Manage Team
          </Button>
        </div>
      </div>

      {/* Task Board */}
      <TaskBoard projectId={id} tasks={tasks} />
    </div>
  );
}