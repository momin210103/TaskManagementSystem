import React, { useState, useEffect } from 'react';
import { Modal } from '../../components/common/Modal';
import { ErrorMessage } from '../../components/common/ErrorMessage';
import { userService } from '../../services/userService';
import { ROLE_OPTIONS } from '../../utils/constants';

export function UserRoleModal({ isOpen, onClose, user, onSuccess }) {
  const [role, setRole] = useState('User');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (user) {
      setRole(user.role || 'User');
    }
    setError(null);
  }, [user, isOpen]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!user) return;

    setIsSubmitting(true);
    setError(null);
    try {
      await userService.updateUserRole(user.id, role);
      onSuccess();
      onClose();
    } catch (err) {
      setError(err.message || 'Failed to update user role.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`Change Role: ${user?.name}`} maxWidth="max-w-md">
      <ErrorMessage message={error} onDismiss={() => setError(null)} />

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-xs font-bold text-slate-700 uppercase tracking-wider mb-2">
            Select Role
          </label>
          <select
            value={role}
            onChange={(e) => setRole(e.target.value)}
            className="w-full px-3.5 py-2.5 text-xs sm:text-sm bg-slate-50 border border-slate-200 rounded-xl text-slate-900 focus:bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all"
          >
            {ROLE_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
          <p className="text-[11px] text-slate-400 mt-2">
            Admins have full access; Managers lead assigned teams; Users work on assigned tasks.
          </p>
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
            {isSubmitting ? 'Updating...' : 'Save Role'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
