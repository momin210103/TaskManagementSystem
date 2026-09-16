import React, { useState, useEffect } from 'react';
import { Modal } from '../../components/common/Modal';
import { ErrorMessage } from '../../components/common/ErrorMessage';
import { taskService } from '../../services/taskService';
import { teamService } from '../../services/teamService';
import { STATUS_OPTIONS, PRIORITY_OPTIONS } from '../../utils/constants';

export function TaskFormModal({ isOpen, onClose, onSuccess, initialData = null }) {
  const isEdit = !!initialData;

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [status, setStatus] = useState('ToDo');
  const [priority, setPriority] = useState('Medium');
  const [deadline, setDeadline] = useState('');
  const [teamId, setTeamId] = useState('');
  const [assignedToId, setAssignedToId] = useState('');

  const [teams, setTeams] = useState([]);
  const [teamMembers, setTeamMembers] = useState([]);
  const [isLoadingTeams, setIsLoadingTeams] = useState(false);
  const [isLoadingMembers, setIsLoadingMembers] = useState(false);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (initialData) {
      setTitle(initialData.title || '');
      setDescription(initialData.description || '');
      setStatus(initialData.status || 'ToDo');
      setPriority(initialData.priority || 'Medium');
      setDeadline(initialData.deadline ? initialData.deadline.substring(0, 10) : '');
      setTeamId(initialData.teamId || '');
      setAssignedToId(initialData.assignedToId || '');
    } else {
      setTitle('');
      setDescription('');
      setStatus('ToDo');
      setPriority('Medium');
      const d = new Date();
      d.setDate(d.getDate() + 7);
      setDeadline(d.toISOString().split('T')[0]);
      setTeamId('');
      setAssignedToId('');
    }
    setError(null);
  }, [initialData, isOpen]);

  useEffect(() => {
    if (isOpen) {
      async function loadTeams() {
        setIsLoadingTeams(true);
        try {
          const list = await teamService.getTeams();
          setTeams(list || []);
          if (!isEdit && list?.length > 0 && !teamId) {
            setTeamId(list[0].id);
          }
        } catch (err) {
          setError('Failed to load teams: ' + err.message);
        } finally {
          setIsLoadingTeams(false);
        }
      }
      loadTeams();
    }
  }, [isOpen, isEdit]);

  useEffect(() => {
    if (teamId) {
      async function loadMembers() {
        setIsLoadingMembers(true);
        try {
          const details = await teamService.getTeamById(teamId);
          setTeamMembers(details.members || []);
          if (!isEdit && details.members?.length > 0 && !assignedToId) {
            setAssignedToId(details.members[0].id);
          }
        } catch {
          setTeamMembers([]);
        } finally {
          setIsLoadingMembers(false);
        }
      }
      loadMembers();
    } else {
      setTeamMembers([]);
    }
  }, [teamId, isEdit]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!title.trim()) {
      setError('Task title is required.');
      return;
    }
    if (!deadline) {
      setError('Deadline is required.');
      return;
    }

    if (!isEdit && (!teamId || !assignedToId)) {
      setError('Please select both a Team and an Assigned Team Member.');
      return;
    }

    setError(null);
    setIsSubmitting(true);

    try {
      if (isEdit) {
        await taskService.updateTask(initialData.id, {
          title: title.trim(),
          description: description.trim() || null,
          priority,
          deadline
        });
      } else {
        await taskService.createTask({
          title: title.trim(),
          description: description.trim() || null,
          status,
          priority,
          deadline,
          teamId,
          assignedToId
        });
      }

      onSuccess();
      onClose();
    } catch (err) {
      setError(err.message || 'Failed to save task.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={isEdit ? 'Edit Task' : 'Create New Task'}
      maxWidth="max-w-xl"
    >
      <ErrorMessage message={error} onDismiss={() => setError(null)} />

      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Title */}
        <div>
          <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
            Task Title <span className="text-rose-500">*</span>
          </label>
          <input
            type="text"
            required
            maxLength={150}
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="e.g. Implement user authentication"
            className="w-full px-3.5 py-2.5 text-xs sm:text-sm bg-slate-50 border border-slate-200 rounded-xl text-slate-900 placeholder:text-slate-400 focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all"
          />
        </div>

        {/* Description */}
        <div>
          <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
            Description
          </label>
          <textarea
            rows={3}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Provide task scope, acceptance criteria, or context..."
            className="w-full px-3.5 py-2.5 text-xs sm:text-sm bg-slate-50 border border-slate-200 rounded-xl text-slate-900 placeholder:text-slate-400 focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all"
          />
        </div>

        {/* Priority & Deadline */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
              Priority <span className="text-rose-500">*</span>
            </label>
            <select
              value={priority}
              onChange={(e) => setPriority(e.target.value)}
              className="w-full px-3.5 py-2.5 text-xs sm:text-sm bg-slate-50 border border-slate-200 rounded-xl text-slate-900 focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all"
            >
              {PRIORITY_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
              Deadline <span className="text-rose-500">*</span>
            </label>
            <input
              type="date"
              required
              value={deadline}
              onChange={(e) => setDeadline(e.target.value)}
              className="w-full px-3.5 py-2.5 text-xs sm:text-sm bg-slate-50 border border-slate-200 rounded-xl text-slate-900 focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all"
            />
          </div>
        </div>

        {!isEdit && (
          <>
            <div>
              <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
                Initial Status
              </label>
              <select
                value={status}
                onChange={(e) => setStatus(e.target.value)}
                className="w-full px-3.5 py-2.5 text-xs sm:text-sm bg-slate-50 border border-slate-200 rounded-xl text-slate-900 focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all"
              >
                {STATUS_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {opt.label}
                  </option>
                ))}
              </select>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
                  Team <span className="text-rose-500">*</span>
                </label>
                <select
                  required
                  value={teamId}
                  disabled={isLoadingTeams}
                  onChange={(e) => {
                    setTeamId(e.target.value);
                    setAssignedToId('');
                  }}
                  className="w-full px-3.5 py-2.5 text-xs sm:text-sm bg-slate-50 border border-slate-200 rounded-xl text-slate-900 focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all"
                >
                  <option value="">Select Team</option>
                  {teams.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.name}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-1.5">
                  Assign To <span className="text-rose-500">*</span>
                </label>
                <select
                  required
                  value={assignedToId}
                  disabled={isLoadingMembers || !teamId}
                  onChange={(e) => setAssignedToId(e.target.value)}
                  className="w-full px-3.5 py-2.5 text-xs sm:text-sm bg-slate-50 border border-slate-200 rounded-xl text-slate-900 focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all disabled:opacity-50"
                >
                  <option value="">{teamMembers.length === 0 ? 'No members in team' : 'Select Member'}</option>
                  {teamMembers.map((m) => (
                    <option key={m.id} value={m.id}>
                      {m.name} ({m.role})
                    </option>
                  ))}
                </select>
              </div>
            </div>
          </>
        )}

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
            className="px-5 py-2 text-xs sm:text-sm font-bold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-sm transition-colors focus:ring-2 focus:ring-indigo-500 disabled:opacity-50"
          >
            {isSubmitting ? 'Saving...' : isEdit ? 'Update Task' : 'Create Task'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
