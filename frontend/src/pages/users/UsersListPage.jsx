import React, { useState, useEffect, useCallback } from 'react';
import { UserCheck, Shield, Users2, Filter, Edit3, UserPlus } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { userService } from '../../services/userService';
import { teamService } from '../../services/teamService';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { ErrorMessage } from '../../components/common/ErrorMessage';
import { EmptyState } from '../../components/common/EmptyState';
import { Pagination } from '../../components/common/Pagination';
import { UserRoleModal } from './UserRoleModal';
import { UserTeamModal } from './UserTeamModal';
import { formatDate, getInitials } from '../../utils/formatters';
import { ROLE_OPTIONS } from '../../utils/constants';

export function UsersListPage() {
  const { user: currentUser, isAdmin, isManager } = useAuth();

  const [usersResult, setUsersResult] = useState(null);
  const [teamsMap, setTeamsMap] = useState({});
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filters
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [roleFilter, setRoleFilter] = useState('');

  // Modals
  const [roleModalUser, setRoleModalUser] = useState(null);
  const [teamModalUser, setTeamModalUser] = useState(null);

  const fetchUsers = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [usersData, teamsData] = await Promise.all([
        userService.getUsers({
          page,
          pageSize,
          role: roleFilter || undefined
        }),
        teamService.getTeams()
      ]);

      setUsersResult(usersData);

      const map = {};
      teamsData?.forEach((t) => {
        map[t.id] = t.name;
      });
      setTeamsMap(map);
    } catch (err) {
      setError(err.message || 'Failed to load organization users.');
    } finally {
      setIsLoading(false);
    }
  }, [page, pageSize, roleFilter]);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-slate-900 tracking-tight">Users</h1>
          <p className="text-sm text-slate-500 mt-1">
            {isAdmin && 'Manage user accounts, system roles, and team assignments'}
            {isManager && 'Members belonging to your managed team'}
          </p>
        </div>
      </div>

      <ErrorMessage message={error} onDismiss={() => setError(null)} retry={fetchUsers} />

      {/* Main Container */}
      <div className="bg-white border border-slate-200/80 rounded-2xl shadow-sm overflow-hidden">
        {/* Filters Header */}
        <div className="p-4 sm:p-5 border-b border-slate-200 flex flex-wrap items-center justify-between gap-3 bg-slate-50/50">
          <div className="flex items-center gap-3">
            <div className="flex items-center gap-1.5 text-xs font-semibold text-slate-500 uppercase tracking-wider">
              <Filter className="w-3.5 h-3.5" />
              Role:
            </div>
            <select
              value={roleFilter}
              onChange={(e) => {
                setRoleFilter(e.target.value);
                setPage(1);
              }}
              className="text-xs border border-slate-200 rounded-lg px-2.5 py-1.5 bg-white text-slate-700 focus:ring-1 focus:ring-indigo-500"
            >
              <option value="">All Roles</option>
              {ROLE_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>

          <span className="text-xs font-semibold px-2.5 py-1 rounded-lg bg-slate-200/70 text-slate-700">
            {usersResult?.totalCount ?? 0} Users
          </span>
        </div>

        {/* Content Table */}
        {isLoading ? (
          <LoadingSpinner text="Loading users..." />
        ) : usersResult?.items?.length === 0 ? (
          <EmptyState
            icon={UserCheck}
            title="No users found"
            description="No users found matching your selected filters."
          />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="bg-slate-50/75 border-b border-slate-200 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                    <th className="py-3.5 px-4 sm:px-6">User</th>
                    <th className="py-3.5 px-4">Role</th>
                    <th className="py-3.5 px-4">Team</th>
                    <th className="py-3.5 px-4">Joined Date</th>
                    {isAdmin && <th className="py-3.5 px-4 text-right">Actions</th>}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-sm">
                  {usersResult?.items?.map((u) => {
                    const isSelf = u.id?.toLowerCase() === currentUser?.id?.toLowerCase();
                    const teamName = u.teamId && teamsMap[u.teamId] ? teamsMap[u.teamId] : 'Unassigned';

                    return (
                      <tr key={u.id} className="hover:bg-slate-50/80 transition-colors">
                        <td className="py-4 px-4 sm:px-6">
                          <div className="flex items-center gap-3">
                            <div className="w-9 h-9 rounded-full bg-indigo-100 text-indigo-700 font-bold text-xs flex items-center justify-center">
                              {getInitials(u.name)}
                            </div>
                            <div>
                              <div className="font-semibold text-slate-900 flex items-center gap-1.5">
                                {u.name}
                                {isSelf && (
                                  <span className="text-[10px] font-semibold px-1.5 py-0.5 rounded bg-emerald-50 text-emerald-700 border border-emerald-200">
                                    You
                                  </span>
                                )}
                              </div>
                              <div className="text-xs text-slate-500">{u.email}</div>
                            </div>
                          </div>
                        </td>

                        <td className="py-4 px-4">
                          <span
                            className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold border ${u.role === 'Admin'
                                ? 'bg-purple-50 text-purple-700 border-purple-200'
                                : u.role === 'Manager'
                                  ? 'bg-indigo-50 text-indigo-700 border-indigo-200'
                                  : 'bg-slate-50 text-slate-700 border-slate-200'
                              }`}
                          >
                            {u.role}
                          </span>
                        </td>

                        <td className="py-4 px-4">
                          <span className={`text-xs font-medium ${u.teamId ? 'text-slate-800' : 'text-slate-400 italic'}`}>
                            {teamName}
                          </span>
                        </td>

                        <td className="py-4 px-4 text-slate-500 text-xs">{formatDate(u.createdAt)}</td>

                        {isAdmin && (
                          <td className="py-4 px-4 text-right">
                            <div className="inline-flex items-center gap-1.5">
                              <button
                                onClick={() => setRoleModalUser(u)}
                                className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium text-slate-700 hover:text-indigo-600 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition-colors"
                                title="Change Role"
                              >
                                <Edit3 className="w-3 h-3" /> Role
                              </button>
                              <button
                                onClick={() => setTeamModalUser(u)}
                                className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium text-slate-700 hover:text-indigo-600 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition-colors"
                                title="Assign Team"
                              >
                                <Users2 className="w-3 h-3" /> Team
                              </button>
                            </div>
                          </td>
                        )}
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            <div className="px-4 sm:px-6">
              <Pagination
                currentPage={page}
                pageSize={pageSize}
                totalCount={usersResult?.totalCount || 0}
                onPageChange={setPage}
                onPageSizeChange={(newSize) => {
                  setPageSize(newSize);
                  setPage(1);
                }}
              />
            </div>
          </>
        )}
      </div>

      {/* Change Role Modal */}
      {roleModalUser && (
        <UserRoleModal
          isOpen={!!roleModalUser}
          onClose={() => setRoleModalUser(null)}
          user={roleModalUser}
          onSuccess={fetchUsers}
        />
      )}

      {/* Assign Team Modal */}
      {teamModalUser && (
        <UserTeamModal
          isOpen={!!teamModalUser}
          onClose={() => setTeamModalUser(null)}
          user={teamModalUser}
          onSuccess={fetchUsers}
        />
      )}
    </div>
  );
}

