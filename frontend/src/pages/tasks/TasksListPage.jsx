import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Plus,
  Filter,
  CheckSquare,
  Edit2,
  Trash2,
  Calendar,
  User,
  Users2,
  ArrowRight
} from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { taskService } from '../../services/taskService';
import { StatusBadge } from '../../components/common/StatusBadge';
import { PriorityBadge } from '../../components/common/PriorityBadge';
import { SkeletonTable } from '../../components/common/SkeletonLoader';
import { ErrorMessage } from '../../components/common/ErrorMessage';
import { EmptyState } from '../../components/common/EmptyState';
import { Pagination } from '../../components/common/Pagination';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { TaskFormModal } from './TaskFormModal';
import { formatDate } from '../../utils/formatters';
import { STATUS_OPTIONS, PRIORITY_OPTIONS } from '../../utils/constants';

export function TasksListPage() {
  const { user, isAdmin, isManager } = useAuth();
  const navigate = useNavigate();

  const [tasksResult, setTasksResult] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filters
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [statusFilter, setStatusFilter] = useState('');
  const [priorityFilter, setPriorityFilter] = useState('');
  const [sortBy, setSortBy] = useState('Deadline');
  const [sortDirection, setSortDirection] = useState('asc');

  // Modals state
  const [isFormModalOpen, setIsFormModalOpen] = useState(false);
  const [editingTask, setEditingTask] = useState(null);
  const [deletingTaskId, setDeletingTaskId] = useState(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const canManageTasks = isAdmin || isManager;

  const fetchTasks = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await taskService.getTasks({
        page,
        pageSize,
        status: statusFilter || undefined,
        priority: priorityFilter || undefined,
        sortBy,
        sortDirection
      });
      setTasksResult(data);
    } catch (err) {
      setError(err.message || 'Failed to load tasks.');
    } finally {
      setIsLoading(false);
    }
  }, [page, pageSize, statusFilter, priorityFilter, sortBy, sortDirection]);

  useEffect(() => {
    fetchTasks();
  }, [fetchTasks]);

  const handleStatusChange = async (taskId, newStatus) => {
    try {
      await taskService.updateTaskStatus(taskId, newStatus);
      fetchTasks();
    } catch (err) {
      setError(err.message || 'Failed to update task status.');
    }
  };

  const handleDelete = async () => {
    if (!deletingTaskId) return;
    setIsDeleting(true);
    try {
      await taskService.deleteTask(deletingTaskId);
      setDeletingTaskId(null);
      fetchTasks();
    } catch (err) {
      setError(err.message || 'Failed to delete task.');
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3.5">
        <div>
          <h1 className="text-xl sm:text-2xl lg:text-3xl font-bold text-slate-900 tracking-tight">Tasks</h1>
          <p className="text-xs sm:text-sm text-slate-500 mt-0.5">
            {isAdmin && 'All organization tasks across all teams'}
            {isManager && 'Tasks managed within your team'}
            {!isAdmin && !isManager && 'Your assigned tasks and deliverables'}
          </p>
        </div>

        {canManageTasks && (
          <button
            onClick={() => {
              setEditingTask(null);
              setIsFormModalOpen(true);
            }}
            className="inline-flex items-center justify-center gap-2 px-4 py-2.5 text-xs sm:text-sm font-bold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-sm transition-colors w-full sm:w-auto"
          >
            <Plus className="w-4 h-4" />
            Create Task
          </button>
        )}
      </div>

      <ErrorMessage message={error} onDismiss={() => setError(null)} retry={fetchTasks} />

      {/* Main Container */}
      <div className="bg-white border border-slate-200/80 rounded-2xl shadow-sm overflow-hidden">
        {/* Filters Bar */}
        <div className="p-4 sm:p-5 border-b border-slate-200 flex flex-wrap items-center justify-between gap-3 bg-slate-50/50">
          <div className="flex flex-wrap items-center gap-2">
            <div className="flex items-center gap-1 text-xs font-semibold text-slate-500">
              <Filter className="w-3.5 h-3.5" />
              <span>Filter:</span>
            </div>

            {/* Status Filter */}
            <select
              value={statusFilter}
              onChange={(e) => {
                setStatusFilter(e.target.value);
                setPage(1);
              }}
              className="text-xs border border-slate-200 rounded-lg px-2.5 py-1.5 bg-white text-slate-700 focus:ring-1 focus:ring-indigo-500"
            >
              <option value="">All Statuses</option>
              {STATUS_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>

            {/* Priority Filter */}
            <select
              value={priorityFilter}
              onChange={(e) => {
                setPriorityFilter(e.target.value);
                setPage(1);
              }}
              className="text-xs border border-slate-200 rounded-lg px-2.5 py-1.5 bg-white text-slate-700 focus:ring-1 focus:ring-indigo-500"
            >
              <option value="">All Priorities</option>
              {PRIORITY_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>

            {/* Sort Filter */}
            <select
              value={`${sortBy}-${sortDirection}`}
              onChange={(e) => {
                const [sb, sd] = e.target.value.split('-');
                setSortBy(sb);
                setSortDirection(sd);
                setPage(1);
              }}
              className="text-xs border border-slate-200 rounded-lg px-2.5 py-1.5 bg-white text-slate-700 focus:ring-1 focus:ring-indigo-500"
            >
              <option value="Deadline-asc">Deadline (Earliest)</option>
              <option value="Deadline-desc">Deadline (Latest)</option>
              <option value="CreatedAt-desc">Created Date (Newest)</option>
              <option value="Priority-desc">Priority</option>
              <option value="Title-asc">Title (A-Z)</option>
            </select>
          </div>

          <span className="text-xs font-bold px-2.5 py-1 rounded-lg bg-slate-200/80 text-slate-700">
            {tasksResult?.totalCount ?? 0} Tasks
          </span>
        </div>

        {/* Content */}
        {isLoading ? (
          <SkeletonTable rows={6} />
        ) : tasksResult?.items?.length === 0 ? (
          <EmptyState
            icon={CheckSquare}
            title="No tasks found"
            description="You don't have any tasks matching the current filters."
            actionLabel={canManageTasks ? 'Create First Task' : undefined}
            onAction={
              canManageTasks
                ? () => {
                  setEditingTask(null);
                  setIsFormModalOpen(true);
                }
                : undefined
            }
          />
        ) : (
          <>
            {/* Desktop / Tablet Table (>= 768px) */}
            <div className="hidden md:block overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="bg-slate-50/75 border-b border-slate-200 text-xs font-bold text-slate-500 uppercase tracking-wider">
                    <th className="py-3.5 px-6">Title</th>
                    <th className="py-3.5 px-4">Status</th>
                    <th className="py-3.5 px-4">Priority</th>
                    <th className="py-3.5 px-4">Deadline</th>
                    <th className="py-3.5 px-4">Assignee</th>
                    <th className="py-3.5 px-4">Team</th>
                    <th className="py-3.5 px-6 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-sm">
                  {tasksResult?.items?.map((task) => (
                    <tr key={task.id} className="hover:bg-slate-50/80 transition-colors">
                      <td
                        onClick={() => navigate(`/tasks/${task.id}`)}
                        className="py-4 px-6 font-semibold text-slate-900 cursor-pointer max-w-xs hover:text-indigo-600"
                      >
                        <div className="truncate">{task.title}</div>
                        {task.description && (
                          <div className="text-xs text-slate-500 truncate mt-0.5">{task.description}</div>
                        )}
                      </td>
                      <td className="py-4 px-4">
                        <select
                          value={task.status}
                          onChange={(e) => handleStatusChange(task.id, e.target.value)}
                          className="text-xs font-bold rounded-lg px-2 py-1 bg-white border border-slate-200 text-slate-700 hover:border-slate-300 focus:ring-1 focus:ring-indigo-500"
                        >
                          {STATUS_OPTIONS.map((opt) => (
                            <option key={opt.value} value={opt.value}>
                              {opt.label}
                            </option>
                          ))}
                        </select>
                      </td>
                      <td className="py-4 px-4">
                        <PriorityBadge priority={task.priority} />
                      </td>
                      <td className="py-4 px-4 text-slate-600">{formatDate(task.deadline)}</td>
                      <td className="py-4 px-4 text-slate-700 font-medium">{task.assignedToName || 'Unassigned'}</td>
                      <td className="py-4 px-4 text-slate-500">{task.teamName || '—'}</td>
                      <td className="py-4 px-6 text-right">
                        <div className="inline-flex items-center gap-1.5">
                          <button
                            onClick={() => navigate(`/tasks/${task.id}`)}
                            className="p-1.5 text-slate-400 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors"
                            title="View Details"
                          >
                            <CheckSquare className="w-4 h-4" />
                          </button>

                          {canManageTasks && (
                            <>
                              <button
                                onClick={() => {
                                  setEditingTask(task);
                                  setIsFormModalOpen(true);
                                }}
                                className="p-1.5 text-slate-400 hover:text-amber-600 hover:bg-amber-50 rounded-lg transition-colors"
                                title="Edit Task"
                              >
                                <Edit2 className="w-4 h-4" />
                              </button>

                              <button
                                onClick={() => setDeletingTaskId(task.id)}
                                className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors"
                                title="Delete Task"
                              >
                                <Trash2 className="w-4 h-4" />
                              </button>
                            </>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Mobile Adaptive Cards (< 768px) */}
            <div className="md:hidden divide-y divide-slate-100">
              {tasksResult?.items?.map((task) => (
                <div key={task.id} className="p-4 space-y-3 hover:bg-slate-50/80 transition-colors">
                  <div
                    onClick={() => navigate(`/tasks/${task.id}`)}
                    className="cursor-pointer space-y-1"
                  >
                    <div className="flex items-start justify-between gap-2">
                      <h3 className="font-bold text-slate-900 text-sm leading-snug break-words flex-1">
                        {task.title}
                      </h3>
                      <PriorityBadge priority={task.priority} />
                    </div>
                    {task.description && (
                      <p className="text-xs text-slate-500 line-clamp-2">{task.description}</p>
                    )}
                  </div>

                  {/* Status & Deadline */}
                  <div className="flex flex-wrap items-center justify-between gap-2 pt-1">
                    <div className="flex items-center gap-1.5">
                      <span className="text-xs font-semibold text-slate-500">Status:</span>
                      <select
                        value={task.status}
                        onChange={(e) => handleStatusChange(task.id, e.target.value)}
                        className="text-xs font-bold rounded-lg px-2 py-1 bg-white border border-slate-200 text-slate-800 focus:ring-1 focus:ring-indigo-500"
                      >
                        {STATUS_OPTIONS.map((opt) => (
                          <option key={opt.value} value={opt.value}>
                            {opt.label}
                          </option>
                        ))}
                      </select>
                    </div>

                    <div className="flex items-center gap-1 text-xs text-slate-600">
                      <Calendar className="w-3.5 h-3.5 text-slate-400" />
                      <span>{formatDate(task.deadline)}</span>
                    </div>
                  </div>

                  {/* Team, Assignee & Actions */}
                  <div className="flex items-center justify-between pt-2 border-t border-slate-100 text-xs">
                    <div className="flex items-center gap-2 truncate text-slate-600">
                      <User className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                      <span className="truncate font-medium">{task.assignedToName || 'Unassigned'}</span>
                    </div>

                    <div className="flex items-center gap-2 shrink-0 ml-2">
                      <button
                        onClick={() => navigate(`/tasks/${task.id}`)}
                        className="inline-flex items-center gap-1 text-xs font-bold text-indigo-600 hover:text-indigo-800"
                      >
                        Details <ArrowRight className="w-3 h-3" />
                      </button>

                      {canManageTasks && (
                        <div className="flex items-center gap-1 ml-1 pl-1 border-l border-slate-200">
                          <button
                            onClick={() => {
                              setEditingTask(task);
                              setIsFormModalOpen(true);
                            }}
                            className="p-1 text-slate-400 hover:text-amber-600"
                            title="Edit"
                          >
                            <Edit2 className="w-3.5 h-3.5" />
                          </button>
                          <button
                            onClick={() => setDeletingTaskId(task.id)}
                            className="p-1 text-slate-400 hover:text-rose-600"
                            title="Delete"
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>

            {/* Pagination */}
            <div className="px-4 sm:px-6">
              <Pagination
                currentPage={page}
                pageSize={pageSize}
                totalCount={tasksResult?.totalCount || 0}
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

      {/* Task Create/Edit Modal */}
      {isFormModalOpen && (
        <TaskFormModal
          isOpen={isFormModalOpen}
          onClose={() => {
            setIsFormModalOpen(false);
            setEditingTask(null);
          }}
          onSuccess={fetchTasks}
          initialData={editingTask}
        />
      )}

      {/* Delete Confirmation Dialog */}
      {deletingTaskId && (
        <ConfirmDialog
          isOpen={!!deletingTaskId}
          onClose={() => setDeletingTaskId(null)}
          onConfirm={handleDelete}
          title="Delete Task"
          message="Are you sure you want to delete this task? All comments and updates associated with this task will be permanently deleted."
          confirmLabel="Delete Task"
          isDestructive
          isLoading={isDeleting}
        />
      )}
    </div>
  );
}
