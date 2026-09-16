import React, { useState, useEffect } from 'react';
import { Modal } from '../../components/common/Modal';
import { ErrorMessage } from '../../components/common/ErrorMessage';
import { teamService } from '../../services/teamService';
import { userService } from '../../services/userService';

export function TeamFormModal({ isOpen, onClose, onSuccess, initialData = null }) {
  const isEdit = !!initialData;

  const [name, setName] = useState('');
  const [managerId, setManagerId] = useState('');
  const [managers, setManagers] = useState([]);
  const [isLoadingManagers, setIsLoadingManagers] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (initialData) {
      setName(initialData.name || '');
      setManagerId(initialData.managerId || '');
    } else {
      setName('');
      setManagerId('');
    }
    setError(null);
  }, [initialData, isOpen]);

  useEffect(() => {
    if (isOpen) {
      async function loadManagers() {
        setIsLoadingManagers(true);
        try {
          // Fetch users with Manager role
          const result = await userService.getUsers({ role: 'Manager', page: 1, pageSize: 100 });
          setManagers(result.items || []);
          if (!isEdit && result.items?.length > 0 && !managerId) {
            setManagerId(result.items[0].id);
          }
        } catch (err) {
          setError('Failed to load managers: ' + err.message);
        } finally {
          setIsLoadingManagers(false);
        }
      }
      loadManagers();
    }
  }, [isOpen, isEdit]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!name.trim()) {
      setError('Team name is required.');
      return;
    }
    if (!managerId) {
      setError('Please select a manager for this team.');
      return;
    }

    setError(null);
    setIsSubmitting(true);

    try {
      if (isEdit) {
        await teamService.updateTeam(initialData.id, {
          name: name.trim(),
          managerId
        });
      } else {
        await teamService.createTeam({
          name: name.trim(),
          managerId
        });
      }

      onSuccess();
      onClose();
    } catch (err) {
      setError(err.message || 'Failed to save team.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={isEdit ? 'Edit Team' : 'Create New Team'} maxWidth="max-w-md">
      <ErrorMessage message={error} onDismiss={() => setError(null)} />

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
            Team Name <span className="text-rose-500">*</span>
          </label>
          <input
            type="text"
            required
            maxLength={100}
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g. Frontend Engineering"
            className="w-full px-3.5 py-2.5 text-sm bg-slate-50 border border-slate-200 rounded-xl text-slate-900 focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
            Assigned Manager <span className="text-rose-500">*</span>
          </label>
          <select
            required
            value={managerId}
            disabled={isLoadingManagers}
            onChange={(e) => setManagerId(e.target.value)}
            className="w-full px-3.5 py-2.5 text-sm bg-slate-50 border border-slate-200 rounded-xl text-slate-900 focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500"
          >
            <option value="">{managers.length === 0 ? 'No managers found' : 'Select Manager'}</option>
            {managers.map((m) => (
              <option key={m.id} value={m.id}>
                {m.name} ({m.email})
              </option>
            ))}
          </select>
          <p className="text-[11px] text-slate-400 mt-1">
            Only users with the Manager role can lead a team.
          </p>
        </div>

        <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-300 rounded-lg hover:bg-slate-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="px-5 py-2 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg shadow-sm disabled:opacity-50"
          >
            {isSubmitting ? 'Saving...' : isEdit ? 'Update Team' : 'Create Team'}
          </button>
        </div>
      </form>
    </Modal>
  );
}

