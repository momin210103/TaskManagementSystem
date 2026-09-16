import React, { useState, useEffect, useCallback } from 'react';
import { Modal } from '../../components/common/Modal';
import { Skeleton } from '../../components/common/SkeletonLoader';
import { ErrorMessage } from '../../components/common/ErrorMessage';
import { EmptyState } from '../../components/common/EmptyState';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { teamService } from '../../services/teamService';
import { userService } from '../../services/userService';
import { useAuth } from '../../context/AuthContext';
import { Users2, UserPlus, Trash2 } from 'lucide-react';
import { getInitials } from '../../utils/formatters';

export function TeamDetailsModal({ isOpen, onClose, teamId, onTeamUpdated }) {
  const { user, isAdmin } = useAuth();

  const [team, setTeam] = useState(null);
  const [allUsers, setAllUsers] = useState([]);
  const [selectedUserId, setSelectedUserId] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isAddingMember, setIsAddingMember] = useState(false);
  const [removingUserId, setRemovingUserId] = useState(null);
  const [isRemoving, setIsRemoving] = useState(false);
  const [error, setError] = useState(null);

  const loadTeamData = useCallback(async () => {
    if (!teamId) return;
    setIsLoading(true);
    setError(null);
    try {
      const details = await teamService.getTeamById(teamId);
      setTeam(details);
    } catch (err) {
      setError(err.message || 'Failed to load team details.');
    } finally {
      setIsLoading(false);
    }
  }, [teamId]);

  useEffect(() => {
    if (isOpen && teamId) {
      loadTeamData();
    }
  }, [isOpen, teamId, loadTeamData]);

  // Load available users for adding to team
  useEffect(() => {
    if (isOpen && (isAdmin || (team && team.managerId === user?.id))) {
      async function loadUsers() {
        try {
          const res = await userService.getUsers({ page: 1, pageSize: 100 });
          setAllUsers(res.items || []);
        } catch {
          // ignore background users load
        }
      }
      loadUsers();
    }
  }, [isOpen, isAdmin, team, user?.id]);

  const canManageMembers =
    isAdmin || (team && team.managerId?.toLowerCase() === user?.id?.toLowerCase());

  const handleAddMember = async (e) => {
    e.preventDefault();
    if (!selectedUserId) return;

    setIsAddingMember(true);
    setError(null);
    try {
      await teamService.addMember(teamId, selectedUserId);
      setSelectedUserId('');
      await loadTeamData();
      if (onTeamUpdated) onTeamUpdated();
    } catch (err) {
      setError(err.message || 'Failed to add member.');
    } finally {
      setIsAddingMember(false);
    }
  };

  const handleRemoveMember = async () => {
    if (!removingUserId) return;
    setIsRemoving(true);
    setError(null);
    try {
      await teamService.removeMember(teamId, removingUserId);
      setRemovingUserId(null);
      await loadTeamData();
      if (onTeamUpdated) onTeamUpdated();
    } catch (err) {
      setError(err.message || 'Failed to remove member.');
    } finally {
      setIsRemoving(false);
    }
  };

  const availableUsersToAdd = allUsers.filter(
    (u) => !team?.members?.some((m) => m.id === u.id)
  );

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={team ? `${team.name} — Team Workspace` : 'Team Details'}
      maxWidth="max-w-2xl"
    >
      <ErrorMessage message={error} onDismiss={() => setError(null)} />

      {isLoading ? (
        <div className="space-y-4 py-2">
          <Skeleton className="h-20 w-full rounded-xl" />
          <Skeleton className="h-12 w-full rounded-xl" />
          <div className="space-y-2 pt-2">
            <Skeleton className="h-14 w-full rounded-xl" />
            <Skeleton className="h-14 w-full rounded-xl" />
          </div>
        </div>
      ) : !team ? (
        <EmptyState title="Team details not available" />
      ) : (
        <div className="space-y-5">
          {/* Manager Info Banner */}
          <div className="p-4 bg-slate-50 border border-slate-200 rounded-xl flex items-center justify-between gap-3">
            <div className="flex items-center gap-3 min-w-0">
              <div className="w-10 h-10 rounded-full bg-indigo-100 text-indigo-700 font-bold flex items-center justify-center text-xs shadow-inner shrink-0">
                {getInitials(team.managerName)}
              </div>
              <div className="min-w-0">
                <div className="flex items-center gap-1.5 flex-wrap">
                  <span className="text-sm font-bold text-slate-900 truncate">{team.managerName}</span>
                  <span className="text-[10px] font-bold px-2 py-0.5 rounded bg-indigo-100 text-indigo-800">
                    Manager
                  </span>
                </div>
                <div className="text-xs text-slate-500 truncate">{team.managerEmail}</div>
              </div>
            </div>
            <div className="text-right shrink-0">
              <span className="text-[11px] font-bold text-slate-500 uppercase tracking-wider block">Members</span>
              <div className="text-base sm:text-lg font-bold text-slate-900">{team.members?.length || 0}</div>
            </div>
          </div>

          {/* Add Member Form (if permitted) */}
          {canManageMembers && (
            <form onSubmit={handleAddMember} className="p-3.5 sm:p-4 border border-indigo-100 bg-indigo-50/40 rounded-xl space-y-2.5">
              <label className="block text-xs font-bold text-indigo-900 uppercase tracking-wider">
                Add Member to Team
              </label>
              <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2">
                <select
                  required
                  value={selectedUserId}
                  onChange={(e) => setSelectedUserId(e.target.value)}
                  className="flex-1 px-3 py-2 text-xs sm:text-sm bg-white border border-slate-200 rounded-xl text-slate-900 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                >
                  <option value="">Select User to Add</option>
                  {availableUsersToAdd.map((u) => (
                    <option key={u.id} value={u.id}>
                      {u.name} ({u.email}) — {u.role}
                    </option>
                  ))}
                </select>
                <button
                  type="submit"
                  disabled={isAddingMember || !selectedUserId}
                  className="inline-flex items-center justify-center gap-1.5 px-4 py-2 text-xs sm:text-sm font-bold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-sm transition-colors disabled:opacity-50"
                >
                  <UserPlus className="w-4 h-4" />
                  {isAddingMember ? 'Adding...' : 'Add Member'}
                </button>
              </div>
            </form>
          )}

          {/* Members List */}
          <div>
            <h4 className="text-xs font-bold text-slate-500 uppercase tracking-wider mb-2.5">
              Team Members ({team.members?.length || 0})
            </h4>

            {team.members?.length === 0 ? (
              <EmptyState
                icon={Users2}
                title="No members assigned"
                description="Use the form above to add engineers or contributors to this team."
              />
            ) : (
              <div className="divide-y divide-slate-100 border border-slate-200 rounded-xl overflow-hidden bg-white">
                {team.members.map((member) => {
                  const isTeamManager = member.id === team.managerId;
                  return (
                    <div
                      key={member.id}
                      className="p-3 sm:p-3.5 flex items-center justify-between gap-3 hover:bg-slate-50 transition-colors"
                    >
                      <div className="flex items-center gap-2.5 min-w-0">
                        <div className="w-8 h-8 rounded-full bg-slate-100 text-slate-700 font-bold text-xs flex items-center justify-center shrink-0">
                          {getInitials(member.name)}
                        </div>
                        <div className="min-w-0">
                          <div className="flex items-center gap-1.5 flex-wrap">
                            <span className="text-xs sm:text-sm font-bold text-slate-900 truncate">
                              {member.name}
                            </span>
                            {isTeamManager && (
                              <span className="text-[10px] font-bold px-1.5 py-0.5 rounded bg-amber-100 text-amber-800">
                                Team Lead
                              </span>
                            )}
                          </div>
                          <div className="text-xs text-slate-500 truncate">{member.email}</div>
                        </div>
                      </div>

                      <div className="flex items-center gap-2 shrink-0">
                        <span className="text-xs font-medium px-2 py-0.5 rounded-full bg-slate-100 text-slate-700">
                          {member.role}
                        </span>

                        {canManageMembers && !isTeamManager && (
                          <button
                            onClick={() => setRemovingUserId(member.id)}
                            className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors"
                            title="Remove from Team"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Remove Member Confirmation */}
      {removingUserId && (
        <ConfirmDialog
          isOpen={!!removingUserId}
          onClose={() => setRemovingUserId(null)}
          onConfirm={handleRemoveMember}
          title="Remove Team Member"
          message="Are you sure you want to remove this user from the team? Their existing tasks will remain preserved."
          confirmLabel="Remove Member"
          isDestructive
          isLoading={isRemoving}
        />
      )}
    </Modal>
  );
}
