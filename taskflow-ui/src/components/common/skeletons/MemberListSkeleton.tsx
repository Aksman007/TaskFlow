import React from 'react';
import { Skeleton } from '../Skeleton';

const MemberRowSkeleton: React.FC = () => (
  <div className="flex items-center justify-between py-4 border-b border-gray-100 dark:border-gray-700">
    <div className="flex items-center gap-3">
      <Skeleton className="h-10 w-10 rounded-full" />
      <div>
        <Skeleton className="h-4 w-32 mb-1" />
        <Skeleton className="h-3 w-40" />
      </div>
    </div>
    <Skeleton className="h-6 w-20 rounded-full" />
  </div>
);

export const MemberListSkeleton: React.FC = () => (
  <div className="bg-white rounded-lg border border-gray-200 dark:bg-gray-800 dark:border-gray-700 p-6">
    <Skeleton className="h-6 w-40 mb-6" />
    {Array.from({ length: 5 }).map((_, i) => (
      <MemberRowSkeleton key={i} />
    ))}
  </div>
);
