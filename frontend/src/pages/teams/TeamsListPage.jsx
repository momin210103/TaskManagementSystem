import React, { useState, useEffect, useCallback } from 'react';
import { Plus, Users2, Shield, User, ArrowRight, Edit2 } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { teamService } from '../../services/teamService';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { ErrorMessage } from '../../components/common/ErrorMessage';
import { EmptyState } from '../../components/common/EmptyState';
import { TeamFormModal } from './TeamFormModal';
import { TeamDetailsModal } from './TeamDetailsModal';
import { formatDate, getInitials } from '../../utils/formatters';

export function TeamsListPage() {
  const { user, isAdmin, isManager } = useAuth();

  const [teams, setTeams] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);

  // Modals
  const [isFormModalOpen, setIsFormModalOpen] = useState(false);
  const [editingTeam, setEditingTeam] = useState(null);
  const [viewingTeamId, setViewingTeamId] = useState(null);

  const fetchTeams = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const list = await teamService.getTeams();
      setTeams(list || []);
    } catch (err) {
      setError(err.message || 'Failed to load teams.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchTeams();
  }, [fetchTeams]);

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-slate-900 tracking-tight">Teams</h1>
          <p className="text-sm text-slate-500 mt-1">
            {isAdmin && 'Manage organizational teams and department assignments'}
            {isManager && 'Overview of your managed team'}
            {!isAdmin && !isManager && 'Your assigned team'}
          </p>
        </div>

        {isAdmin && (
          <button
            onClick={() => {
              setEditingTeam(null);
              setIsFormModalOpen(true);
            }}
            className="inline-flex items-center gap-2 px-4 py-2.5 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-sm transition-colors"
          >
            <Plus className="w-4 h-4" />
            Create Team
          </button>
        )}
      </div>

      <ErrorMessage message={error} onDismiss={() => setError(null)} retry={fetchTeams} />

      {/* Teams Grid */}
      {isLoading ? (
        <LoadingSpinner text="Loading teams..." />
      ) : teams.length === 0 ? (
        <EmptyState
          icon={Users2}
          title="No teams found"
          description="There are currently no active teams configured."
          actionLabel={isAdmin ? 'Create First Team' : undefined}
          onAction={
            isAdmin
              ? () => {
                setEditingTeam(null);
                setIsFormModalOpen(true);
              }
              : undefined
          }
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {teams.map((team) => (
            <div
              key={team.id}
              className="bg-white border border-slate-200/80 rounded-2xl p-6 shadow-sm hover:shadow-md transition-shadow flex flex-col justify-between"
            >
              <div className="space-y-4">
                <div className="flex items-start justify-between gap-3">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 rounded-xl bg-indigo-50 border border-indigo-100 text-indigo-600 flex items-center justify-center font-bold">
                      <Users2 className="w-5 h-5" />
                    </div>
                    <div>
                      <h3 className="font-bold text-slate-900 text-base">{team.name}</h3>
                      <p className="text-xs text-slate-500">Created {formatDate(team.createdAt)}</p>
                    </div>
                  </div>

                  {isAdmin && (
                    <button
                      onClick={() => {
                        setEditingTeam(team);
                        setIsFormModalOpen(true);
                      }}
                      className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors"
                      title="Edit Team"
                    >
                      <Edit2 className="w-4 h-4" />
                    </button>
                  )}
                </div>

                <div className="p-3.5 bg-slate-50 rounded-xl space-y-2 text-xs">
                  <div className="flex items-center justify-between text-slate-600">
                    <span className="flex items-center gap-1.5 font-medium">
                      <Shield className="w-3.5 h-3.5 text-indigo-600" /> Manager:
                    </span>
                    <span className="font-semibold text-slate-900">{team.managerName || '—'}</span>
                  </div>
                  <div className="flex items-center justify-between text-slate-600">
                    <span className="flex items-center gap-1.5 font-medium">
                      <User className="w-3.5 h-3.5 text-slate-400" /> Members:
                    </span>
                    <span className="font-bold text-slate-900 px-2 py-0.5 rounded bg-white border border-slate-200">
                      {team.memberCount}
                    </span>
                  </div>
                </div>
              </div>

              <div className="pt-4 mt-4 border-t border-slate-100 flex items-center justify-between">
                <span className="text-xs text-slate-400">Team Workspace</span>
                <button
                  onClick={() => setViewingTeamId(team.id)}
                  className="inline-flex items-center gap-1 text-xs font-semibold text-indigo-600 hover:text-indigo-800 transition-colors"
                >
                  View Details <ArrowRight className="w-3.5 h-3.5" />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Create / Edit Team Modal */}
      {isFormModalOpen && (
        <TeamFormModal
          isOpen={isFormModalOpen}
          onClose={() => {
            setIsFormModalOpen(false);
            setEditingTeam(null);
          }}
          onSuccess={fetchTeams}
          initialData={editingTeam}
        />
      )}

      {/* View Team Details & Members Modal */}
      {viewingTeamId && (
        <TeamDetailsModal
          isOpen={!!viewingTeamId}
          onClose={() => setViewingTeamId(null)}
          teamId={viewingTeamId}
          onTeamUpdated={fetchTeams}
        />
      )}
    </div>
  );
}

