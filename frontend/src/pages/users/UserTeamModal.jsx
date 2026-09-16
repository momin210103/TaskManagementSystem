import React, { useState, useEffect } from 'react';
import { Modal } from '../../components/common/Modal';
import { ErrorMessage } from '../../components/common/ErrorMessage';
import { userService } from '../../services/userService';
import { teamService } from '../../services/teamService';

export function UserTeamModal({ isOpen, onClose, user, onSuccess }) {
  const [teamId, setTeamId] = useState('');
  const [teams, setTeams] = useState([]);
  const [isLoadingTeams, setIsLoadingTeams] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (user) {
      setTeamId(user.teamId || '');
    }
    setError(null);
  }, [user, isOpen]);

  useEffect(() => {
    if (isOpen) {
      async function loadTeams() {
        setIsLoadingTeams(true);
        try {
          const list = await teamService.getTeams();
          setTeams(list || []);
        } catch (err) {
          setError('Failed to load teams: ' + err.message);
        } finally {
          setIsLoadingTeams(false);
        }
      }
      loadTeams();
    }
  }, [isOpen]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!user) return;

    setIsSubmitting(true);
    setError(null);
    try {
      await userService.updateUserTeam(user.id, teamId ? teamId : null);
      onSuccess();
      onClose();
    } catch (err) {
      setError(err.message || 'Failed to update user team.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`Assign Team: ${user?.name}`} maxWidth="max-w-md">
      <ErrorMessage message={error} onDismiss={() => setError(null)} />

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
            Select Team
          </label>
          <select
            value={teamId}
            disabled={isLoadingTeams}
            onChange={(e) => setTeamId(e.target.value)}
            className="w-full px-3.5 py-2.5 text-xs sm:text-sm bg-slate-50 border border-slate-200 rounded-xl text-slate-900 focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all"
          >
            <option value="">No Team (Unassigned)</option>
            {teams.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
              </option>
            ))}
          </select>
        </div>

        <div className="flex items-center justify-end gap-2.5 pt-4 border-t border-slate-100">
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="px-4 py-2 text-xs sm:text-sm font-semibold text-slate-700 bg-white border border-slate-300 rounded-xl hover:bg-slate-50 transition-colors"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="px-5 py-2 text-xs sm:text-sm font-bold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-sm transition-colors disabled:opacity-50"
          >
            {isSubmitting ? 'Assigning...' : 'Save Team'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
