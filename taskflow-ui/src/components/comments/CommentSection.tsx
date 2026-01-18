'use client';

import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { commentsApi } from '@/lib/api/comments';
import { Button } from '@/components/common/Button';
import { TextArea } from '@/components/common/TextArea';
import { Spinner } from '@/components/common/Spinner';
import { useAuth } from '@/lib/hooks/useAuth';
import { format } from 'date-fns';
import { TrashIcon } from '@heroicons/react/24/outline';

interface CommentSectionProps {
  taskId: string;
  projectId: string;
}

export const CommentSection: React.FC<CommentSectionProps> = ({
  taskId,
  projectId,
}) => {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [newComment, setNewComment] = useState('');

  const { data: comments, isLoading } = useQuery({
    queryKey: ['comments', taskId],
    queryFn: () => commentsApi.getTaskComments(taskId),
  });

  const addCommentMutation = useMutation({
    mutationFn: commentsApi.add,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments', taskId] });
      setNewComment('');
    },
  });

  const deleteCommentMutation = useMutation({
    mutationFn: commentsApi.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments', taskId] });
    },
  });

  const handleAddComment = async () => {
    if (!newComment.trim()) return;

    try {
      await addCommentMutation.mutateAsync({
        taskId,
        projectId,
        content: newComment,
      });
    } catch (error) {
      console.error('Failed to add comment:', error);
    }
  };

  const handleDeleteComment = async (commentId: string) => {
    if (!confirm('Are you sure you want to delete this comment?')) return;

    try {
      await deleteCommentMutation.mutateAsync(commentId);
    } catch (error) {
      console.error('Failed to delete comment:', error);
    }
  };

  if (isLoading) {
    return <Spinner />;
  }

  return (
    <div className="space-y-4">
      <h4 className="font-medium text-gray-900">
        Comments ({comments?.length || 0})
      </h4>

      {/* Add Comment */}
      <div className="space-y-2">
        <TextArea
          placeholder="Add a comment..."
          value={newComment}
          onChange={(e) => setNewComment(e.target.value)}
          rows={3}
        />
        <Button
          onClick={handleAddComment}
          isLoading={addCommentMutation.isPending}
          disabled={!newComment.trim()}
          size="sm"
        >
          Add Comment
        </Button>
      </div>

      {/* Comments List */}
      <div className="space-y-3">
        {comments?.map((comment) => (
          <div
            key={comment.id}
            className="bg-gray-50 rounded-lg p-4 relative group"
          >
            <div className="flex items-start justify-between mb-2">
              <div>
                <p className="font-medium text-sm text-gray-900">
                  {comment.userName}
                </p>
                <p className="text-xs text-gray-500">
                  {format(new Date(comment.createdAt), 'PPp')}
                </p>
              </div>
              {user?.id === comment.userId && (
                <button
                  onClick={() => handleDeleteComment(comment.id)}
                  className="opacity-0 group-hover:opacity-100 transition-opacity p-1 hover:bg-red-50 rounded text-red-600"
                >
                  <TrashIcon className="h-4 w-4" />
                </button>
              )}
            </div>
            <p className="text-gray-700 text-sm">{comment.content}</p>
          </div>
        ))}

        {comments?.length === 0 && (
          <p className="text-center text-gray-500 text-sm py-4">
            No comments yet. Be the first to comment!
          </p>
        )}
      </div>
    </div>
  );
};