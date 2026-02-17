/* eslint-disable react-hooks/set-state-in-effect */
'use client';

import { use, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useProject } from '@/lib/hooks/useProjects';
import { useProjectMembers } from '@/lib/hooks/useProjectMembers';
import { Button } from '@/components/common/Button';
import { Input } from '@/components/common/Input';
import { MemberListSkeleton } from '@/components/common/skeletons/MemberListSkeleton';
import { Modal } from '@/components/common/Modal';
import { ArrowLeftIcon, PlusIcon, TrashIcon } from '@heroicons/react/24/outline';
import { ProjectRole } from '@/lib/types';

interface PageProps {
  params: Promise<{ id: string }>;
}

export default function ProjectMembersPage({ params }: PageProps) {
  const router = useRouter();
  const { id } = use(params);
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);

  const { data: project, isLoading: projectLoading } = useProject(id);
  const {
    members,
    isLoading: membersLoading,
    addMember,
    removeMember,
    updateMemberRole,
  } = useProjectMembers(id);

  if (projectLoading || membersLoading) {
    return (
      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <MemberListSkeleton />
      </div>
    );
  }

  if (!project) {
    return (
      <div className="text-center py-12">
        <h2 className="text-2xl font-bold text-gray-900 mb-4">
          Project not found
        </h2>
        <Button onClick={() => router.push('/dashboard')}>
          Back to Dashboard
        </Button>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Header */}
      <div className="mb-8">
        <Button
          variant="ghost"
          onClick={() => router.push(`/dashboard/projects/${id}`)}
          className="mb-4 gap-2"
        >
          <ArrowLeftIcon className="h-4 w-4" />
          Back to Project
        </Button>

        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">Team Members</h1>
            <p className="text-gray-600 mt-2">{project.name}</p>
          </div>
          <Button
            variant="primary"
            onClick={() => setIsAddModalOpen(true)}
            className="gap-2"
          >
            <PlusIcon className="h-5 w-5" />
            Add Member
          </Button>
        </div>
      </div>

      {/* Members List */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200">
        <div className="divide-y divide-gray-200">
          {members.length === 0 ? (
            <div className="text-center py-12">
              <p className="text-gray-600 mb-4">No team members yet</p>
              <Button
                variant="primary"
                onClick={() => setIsAddModalOpen(true)}
              >
                Add First Member
              </Button>
            </div>
          ) : (
            members.map((member) => (
              <div
                key={member.id}
                className="flex items-center justify-between p-4 hover:bg-gray-50"
              >
                <div className="flex-1">
                  <h3 className="font-medium text-gray-900">{member.userName}</h3>
                  <p className="text-sm text-gray-600">{member.userEmail}</p>
                </div>

                <div className="flex items-center gap-4">
                  <select
                    value={member.role}
                    onChange={(e) =>
                      updateMemberRole(member.id, parseInt(e.target.value))
                    }
                    className="px-3 py-1.5 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
                  >
                    <option value={ProjectRole.Viewer}>Viewer</option>
                    <option value={ProjectRole.Member}>Member</option>
                    <option value={ProjectRole.Admin}>Admin</option>
                  </select>

                  {member.role !== 'Owner' && (
                    <button
                      onClick={() => {
                        if (
                          confirm(
                            `Remove ${member.userName} from the project?`
                          )
                        ) {
                          removeMember(member.id);
                        }
                      }}
                      className="p-2 text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                    >
                      <TrashIcon className="h-5 w-5" />
                    </button>
                  )}
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      {/* Add Member Modal */}
      <AddMemberModal
        isOpen={isAddModalOpen}
        onClose={() => setIsAddModalOpen(false)}
        projectId={id}
        onMemberAdded={() => {
          setIsAddModalOpen(false);
        }}
      />
    </div>
  );
}

// Add Member Modal Component
interface AddMemberModalProps {
  isOpen: boolean;
  onClose: () => void;
  projectId: string;
  onMemberAdded: () => void;
}

function AddMemberModal({
  isOpen,
  onClose,
  projectId,
  onMemberAdded,
}: AddMemberModalProps) {
  const { addMember } = useProjectMembers(projectId);
  const [email, setEmail] = useState('');
  const [role, setRole] = useState('0');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!email.trim()) {
      setError('Email is required');
      return;
    }

    try {
      setIsLoading(true);
      setError('');
      
      await addMember(email, Number(role));
      
      setEmail('');
      setRole('0');
      onMemberAdded();
    } catch (err: any) {
      console.error('Failed to add member:', err);
      setError(
        err.response?.data?.error ||
          err.message ||
          'Failed to add member. Make sure the user exists.'
      );
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Add Team Member">
      <form onSubmit={handleSubmit} className="space-y-6">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
            {error}
          </div>
        )}

        <Input
          label="Email Address"
          type="email"
          placeholder="colleague@example.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">
            Role
          </label>
          <select
            value={role}
            onChange={(e) => setRole(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
          >
            <option value={ProjectRole.Viewer}>Viewer - Can view only</option>
            <option value={ProjectRole.Member}>Member - Can create and edit tasks</option>
            <option value={ProjectRole.Admin}>Admin - Can manage project settings</option>
          </select>
        </div>

        <div className="flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" variant="primary" isLoading={isLoading}>
            Add Member
          </Button>
        </div>
      </form>
    </Modal>
  );
}