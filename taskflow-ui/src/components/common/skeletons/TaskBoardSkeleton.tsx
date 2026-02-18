import React from 'react';
import { Skeleton } from '../Skeleton';

const TaskCardSkeleton: React.FC = () => (
  <div className="bg-white rounded-lg border border-gray-200 dark:bg-gray-800 dark:border-gray-700 p-4 shadow-sm">
    <Skeleton className="h-4 w-3/4 mb-2" />
    <Skeleton className="h-3 w-full mb-3" />
    <div className="flex items-center justify-between">
      <Skeleton className="h-5 w-16 rounded-full" />
      <Skeleton className="h-3 w-20" />
    </div>
  </div>
);

const ColumnSkeleton: React.FC<{ color: string; count: number }> = ({ color, count }) => (
  <div className={`${color} rounded-lg p-4`}>
    <div className="flex items-center justify-between mb-4">
      <Skeleton className="h-5 w-24" />
      <Skeleton className="h-5 w-6 rounded-full" />
    </div>
    <div className="space-y-3">
      {Array.from({ length: count }).map((_, i) => (
        <TaskCardSkeleton key={i} />
      ))}
    </div>
  </div>
);

export const TaskBoardSkeleton: React.FC = () => (
  <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
    <ColumnSkeleton color="bg-gray-100 dark:bg-gray-800" count={3} />
    <ColumnSkeleton color="bg-blue-100 dark:bg-blue-900/30" count={2} />
    <ColumnSkeleton color="bg-green-100 dark:bg-green-900/30" count={2} />
  </div>
);
