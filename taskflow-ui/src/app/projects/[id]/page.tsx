/* eslint-disable @typescript-eslint/no-explicit-any */
/* eslint-disable react-hooks/set-state-in-effect */
'use client';

import { use } from 'react';
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useProject } from '@/lib/hooks/useProjects';
import { useTasks } from '@/lib/hooks/useTasks';
import { useSignalR } from '@/lib/hooks/useSignalR';
import { TaskBoard } from '@/components/tasks/TaskBoard';
import { Spinner } from '@/components/common/Spinner';
import { Button } from '@/components/common/Button';
import { ArrowLeftIcon } from '@heroicons/react/24/outline';

interface PageProps {
  params: Promise<{ id: string }>;
}

export default function ProjectPage({ params }: PageProps) {
  const router = useRouter();
  const { id } = use(params);
  const [isCheckingAuth, setIsCheckingAuth] = useState(true);

  console.log('Project Page - ID from params:', id);
  console.log('Project Page - ID type:', typeof id);

  // Check authentication
  useEffect(() => {
    const token = localStorage.getItem('token');
    console.log('Project page - checking auth, token exists:', !!token);
    
    if (!token) {
      console.log('No token found, redirecting to login');
      router.push('/login');
      return;
    }
    
    setIsCheckingAuth(false);
  }, [router]);

  const { data: project, isLoading: projectLoading, error: projectError } = useProject(id);
  const { tasks, isLoading: tasksLoading, error: tasksError } = useTasks(id);
  
  console.log('Project data:', project);
  console.log('Project loading:', projectLoading);
  console.log('Project error:', projectError);

  // Connect to SignalR for real-time updates
  useSignalR(id);

  if (isCheckingAuth) {
    return (
      <div className="flex items-center justify-center h-96">
        <Spinner size="xl" />
      </div>
    );
  }

  if (projectLoading || tasksLoading) {
    return (
      <div className="flex items-center justify-center h-96">
        <Spinner size="xl" />
        <p className="ml-4 text-gray-600">Loading project...</p>
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
        
        <div>
          <h1 className="text-3xl font-bold text-gray-900">{project.name}</h1>
          <p className="text-gray-600 mt-2">{project.description}</p>
          <div className="flex gap-4 mt-4 text-sm text-gray-600">
            <span>{project.memberCount} members</span>
            <span>•</span>
            <span>{project.taskCount} tasks</span>
          </div>
        </div>
      </div>

      {/* Task Board */}
      <TaskBoard projectId={id} tasks={tasks} />
    </div>
  );
}