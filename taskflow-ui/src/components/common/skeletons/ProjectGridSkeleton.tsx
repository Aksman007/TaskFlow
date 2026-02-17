import React from 'react';
import { ProjectCardSkeleton } from './ProjectCardSkeleton';

export const ProjectGridSkeleton: React.FC = () => (
  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
    {Array.from({ length: 6 }).map((_, i) => (
      <ProjectCardSkeleton key={i} />
    ))}
  </div>
);
