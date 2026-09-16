import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import {
  CheckCircle2,
  Clock,
  AlertCircle,
  Calendar,
  Layers,
  Flame,
  ArrowUpRight,
  Filter,
  CheckSquare
} from 'lucide-react';
import { dashboardService } from '../../services/dashboardService';
import { StatusBadge } from '../../components/common/StatusBadge';
import { PriorityBadge } from '../../components/common/PriorityBadge';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { ErrorMessage } from '../../components/common/ErrorMessage';
import { EmptyState } from '../../components/common/EmptyState';
import { Pagination } from '../../components/common/Pagination';
import { formatDate } from '../../utils/formatters';
import { STATUS_OPTIONS, PRIORITY_OPTIONS } from '../../utils/constants';

export function DashboardPage() {
  const navigate = useNavigate();

  const [summary, setSummary] = useState(null);
  const [tasksResult, setTasksResult] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);

  // Filter & Pagination state
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [statusFilter, setStatusFilter] = useState('');
  const [priorityFilter, setPriorityFilter] = useState('');
  const [sortBy, setSortBy] = useState('Deadline');
  const [sortDirection, setSortDirection] = useState('asc');

  const fetchData = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [summaryData, tasksData] = await Promise.all([
        dashboardService.getSummary(),
        dashboardService.getTasks({
          page,
          pageSize,
          status: statusFilter || undefined,
          priority: priorityFilter || undefined,
          sortBy,
          sortDirection
        })
      ]);

      setSummary(summaryData);
      setTasksResult(tasksData);
    } catch (err) {
      setError(err.message || 'Failed to load dashboard data.');
    } finally {
      setIsLoading(false);
    }
  }, [page, pageSize, statusFilter, priorityFilter, sortBy, sortDirection]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const statCards = [
    {
      title: 'Total Tasks',
      value: summary?.totalTasks ?? 0,
      icon: Layers,
      color: 'bg-indigo-50 text-indigo-700 border-indigo-200'
    },
    {
      title: 'To Do',
      value: summary?.toDoCount ?? 0,
      icon: Clock,
      color: 'bg-blue-50 text-blue-700 border-blue-200'
    },
    {
      title: 'In Progress',
      value: summary?.inProgressCount ?? 0,
      icon: CheckSquare,
      color: 'bg-amber-50 text-amber-700 border-amber-200'
    },
    {
      title: 'Completed',
      value: summary?.doneCount ?? 0,
      icon: CheckCircle2,
      color: 'bg-emerald-50 text-emerald-700 border-emerald-200'
    },
    {
      title: 'High Priority',
      value: summary?.highPriorityCount ?? 0,
      icon: Flame,
      color: 'bg-rose-50 text-rose-700 border-rose-200'
    },
    {
      title: 'Overdue',
      value: summary?.overdueCount ?? 0,
      icon: AlertCircle,
      color: 'bg-red-50 text-red-700 border-red-200'
    },
    {
      title: 'Due Today',
      value: summary?.dueTodayCount ?? 0,
      icon: Calendar,
      color: 'bg-purple-50 text-purple-700 border-purple-200'
    },
    {
      title: 'Upcoming',
      value: summary?.upcomingCount ?? 0,
      icon: ArrowUpRight,
      color: 'bg-teal-50 text-teal-700 border-teal-200'
    }
  ];

  return (
    <div className="space-y-8 animate-fade-in">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-slate-900 tracking-tight">Dashboard</h1>
          <p className="text-sm text-slate-500 mt-1">Overview of team tasks, metrics, and progress</p>
        </div>
        <Link
          to="/tasks"
          className="inline-flex items-center gap-2 px-4 py-2 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-sm transition-colors"
        >
          <CheckSquare className="w-4 h-4" />
          Manage Tasks
        </Link>
      </div>

      <ErrorMessage message={error} onDismiss={() => setError(null)} retry={fetchData} />

      {/* Summary Cards Grid */}
      <div className="grid grid-cols-2 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {statCards.map((card) => {
          const Icon = card.icon;
          return (
            <div
              key={card.title}
              className="p-5 bg-white border border-slate-200/80 rounded-2xl shadow-sm hover:shadow-md transition-shadow flex items-center justify-between"
            >
              <div>
                <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">{card.title}</p>
                <p className="text-2xl font-bold text-slate-900 mt-1">{isLoading ? '—' : card.value}</p>
              </div>
              <div className={`p-3 rounded-xl border ${card.color}`}>
                <Icon className="w-6 h-6" />
              </div>
            </div>
          );
        })}
      </div>

      {/* Tasks Table Section */}
      <div className="bg-white border border-slate-200/80 rounded-2xl shadow-sm overflow-hidden">
        <div className="p-5 border-b border-slate-200 flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-center gap-2">
            <h2 className="text-lg font-bold text-slate-900">Task Overview</h2>
            <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-slate-100 text-slate-600">
              {tasksResult?.totalCount ?? 0} Total
            </span>
          </div>

          {/* Filters */}
          <div className="flex flex-wrap items-center gap-2.5">
            <div className="flex items-center gap-1.5 text-xs text-slate-500 font-medium">
              <Filter className="w-3.5 h-3.5" />
              Filters:
            </div>

            {/* Status Filter */}
            <select
              value={statusFilter}
              onChange={(e) => {
                setStatusFilter(e.target.value);
                setPage(1);
              }}
              className="text-xs border border-slate-200 rounded-lg px-2.5 py-1.5 bg-slate-50 text-slate-700 hover:bg-white focus:bg-white focus:ring-1 focus:ring-indigo-500"
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
              className="text-xs border border-slate-200 rounded-lg px-2.5 py-1.5 bg-slate-50 text-slate-700 hover:bg-white focus:bg-white focus:ring-1 focus:ring-indigo-500"
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
              className="text-xs border border-slate-200 rounded-lg px-2.5 py-1.5 bg-slate-50 text-slate-700 hover:bg-white focus:bg-white focus:ring-1 focus:ring-indigo-500"
            >
              <option value="Deadline-asc">Deadline (Earliest first)</option>
              <option value="Deadline-desc">Deadline (Latest first)</option>
              <option value="CreatedAt-desc">Created Date (Newest)</option>
              <option value="Priority-desc">Priority</option>
              <option value="Title-asc">Title (A-Z)</option>
            </select>
          </div>
        </div>

        {/* Content / Table */}
        {isLoading ? (
          <LoadingSpinner text="Fetching tasks..." />
        ) : tasksResult?.items?.length === 0 ? (
          <EmptyState
            title="No tasks match your filters"
            description="Try resetting your filters or create a new task to get started."
          />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="bg-slate-50/75 border-b border-slate-200 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                    <th className="py-3.5 px-4 sm:px-6">Task</th>
                    <th className="py-3.5 px-4">Status</th>
                    <th className="py-3.5 px-4">Priority</th>
                    <th className="py-3.5 px-4">Deadline</th>
                    <th className="py-3.5 px-4">Assignee</th>
                    <th className="py-3.5 px-4">Team</th>
                    <th className="py-3.5 px-4 text-right">Action</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-sm">
                  {tasksResult?.items?.map((task) => (
                    <tr
                      key={task.id}
                      onClick={() => navigate(`/tasks/${task.id}`)}
                      className="hover:bg-slate-50/80 cursor-pointer transition-colors"
                    >
                      <td className="py-4 px-4 sm:px-6 font-medium text-slate-900 max-w-xs truncate">
                        {task.title}
                      </td>
                      <td className="py-4 px-4">
                        <StatusBadge status={task.status} />
                      </td>
                      <td className="py-4 px-4">
                        <PriorityBadge priority={task.priority} />
                      </td>
                      <td className="py-4 px-4 text-slate-600">{formatDate(task.deadline)}</td>
                      <td className="py-4 px-4 text-slate-700">{task.assignedToName || 'Unassigned'}</td>
                      <td className="py-4 px-4 text-slate-500">{task.teamName || '—'}</td>
                      <td className="py-4 px-4 text-right">
                        <span className="text-xs font-semibold text-indigo-600 hover:text-indigo-800">
                          Details &rarr;
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

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
    </div>
  );
}

