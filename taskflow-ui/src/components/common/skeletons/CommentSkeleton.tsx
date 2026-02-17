import React from 'react';
import { Skeleton } from '../Skeleton';

const CommentItemSkeleton: React.FC = () => (
  <div className="bg-gray-50 rounded-lg p-4">
    <div className="flex items-start justify-between mb-2">
      <div>
        <Skeleton className="h-4 w-24 mb-1" />
        <Skeleton className="h-3 w-32" />
      </div>
    </div>
    <Skeleton className="h-3 w-full mb-1" />
    <Skeleton className="h-3 w-3/4" />
  </div>
);

export const CommentSkeleton: React.FC = () => (
  <div className="space-y-4">
    <Skeleton className="h-5 w-32" />
    <div className="space-y-2">
      <Skeleton className="h-20 w-full rounded-lg" />
      <Skeleton className="h-8 w-28 rounded-lg" />
    </div>
    <div className="space-y-3">
      {Array.from({ length: 3 }).map((_, i) => (
        <CommentItemSkeleton key={i} />
      ))}
    </div>
  </div>
);
