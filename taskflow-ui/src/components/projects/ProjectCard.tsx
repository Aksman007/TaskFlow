'use client';

import React from 'react';
import { useRouter } from 'next/navigation';
import { Project } from '@/lib/types';
import { format } from 'date-fns';
import {
  FolderIcon,
  UserGroupIcon,
  CheckCircleIcon,
  TrashIcon,
} from '@heroicons/react/24/outline';
import { useProjects } from '@/lib/hooks/useProjects';

interface ProjectCardProps {
  project: Project;
}

export const ProjectCard: React.FC<ProjectCardProps> = ({ project }) => {
  const router = useRouter();
  const { deleteProject } = useProjects();
  const [isDeleting, setIsDeleting] = React.useState(false);

  const handleDelete = async (e: React.MouseEvent) => {
    e.stopPropagation();
    
    if (!confirm('Are you sure you want to delete this project? This action cannot be undone.')) {
      return;
    }

    try {
      setIsDeleting(true);
      await deleteProject(project.id);
    } catch (error) {
      console.error('Failed to delete project:', error);
      alert('Failed to delete project');
    } finally {
      setIsDeleting(false);
    }
  };

  const handleClick = () => {
    console.log('Navigating to project:', project.id);
    router.push(`/dashboard/projects/${project.id}`);
  };

  return (
    <div
      onClick={handleClick}
      className="card hover:shadow-lg transition-all cursor-pointer group relative"
    >
      {/* Delete Button */}
      <button
        onClick={handleDelete}
        disabled={isDeleting}
        className="absolute top-4 right-4 p-2 opacity-0 group-hover:opacity-100 transition-opacity rounded-lg hover:bg-red-50 dark:hover:bg-red-900/30 text-red-600 disabled:opacity-50"
      >
        <TrashIcon className="h-5 w-5" />
      </button>

      {/* Project Icon */}
      <div className="flex items-center gap-4 mb-4">
        <div className="flex items-center justify-center w-12 h-12 bg-primary-100 dark:bg-primary-900/30 rounded-lg">
          <FolderIcon className="h-6 w-6 text-primary-600" />
        </div>
        <div className="flex-1 min-w-0">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 group-hover:text-primary-600 transition-colors truncate">
            {project.name}
          </h3>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            {format(new Date(project.createdAt), 'MMM d, yyyy')}
          </p>
        </div>
      </div>

      {/* Description */}
      <p className="text-gray-600 dark:text-gray-400 text-sm mb-4 line-clamp-2">
        {project.description || 'No description provided'}
      </p>

      {/* Stats */}
      <div className="flex items-center gap-4 text-sm">
        <div className="flex items-center gap-1.5 text-gray-600 dark:text-gray-400">
          <UserGroupIcon className="h-4 w-4" />
          <span>{project.memberCount} members</span>
        </div>
        <div className="flex items-center gap-1.5 text-gray-600 dark:text-gray-400">
          <CheckCircleIcon className="h-4 w-4" />
          <span>{project.taskCount} tasks</span>
        </div>
      </div>

      {/* Owner */}
      <div className="mt-4 pt-4 border-t border-gray-200 dark:border-gray-700">
        <p className="text-xs text-gray-500 dark:text-gray-400">
          Owner: <span className="font-medium text-gray-700 dark:text-gray-300">{project.ownerName}</span>
        </p>
      </div>
    </div>
  );
};