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
  CheckSquare,
  ArrowRight,
  User,
  Users2
} from 'lucide-react';
import { dashboardService } from '../../services/dashboardService';
import { StatusBadge } from '../../components/common/StatusBadge';
import { PriorityBadge } from '../../components/common/PriorityBadge';
import { SkeletonStatCard, SkeletonTable } from '../../components/common/SkeletonLoader';
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
      color: 'bg-rose-50 text-rose-700 border-rose-200'
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
    <div className="space-y-6 sm:space-y-8 animate-fade-in">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3.5">
        <div>
          <h1 className="text-xl sm:text-2xl lg:text-3xl font-bold text-slate-900 tracking-tight">Dashboard</h1>
          <p className="text-xs sm:text-sm text-slate-500 mt-0.5">Overview of team progress, task metrics, and deadlines</p>
        </div>
        <Link
          to="/tasks"
          className="inline-flex items-center justify-center gap-2 px-4 py-2.5 text-xs sm:text-sm font-bold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-sm transition-colors w-full sm:w-auto"
        >
          <CheckSquare className="w-4 h-4" />
          Manage Tasks
        </Link>
      </div>

      <ErrorMessage message={error} onDismiss={() => setError(null)} retry={fetchData} />

      {/* Summary Cards Grid */}
      <div className="grid grid-cols-2 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3 sm:gap-4">
        {isLoading
          ? Array.from({ length: 8 }).map((_, i) => <SkeletonStatCard key={i} />)
          : statCards.map((card) => {
            const Icon = card.icon;
            return (
              <div
                key={card.title}
                className="p-3.5 sm:p-5 bg-white border border-slate-200/80 rounded-2xl shadow-sm hover:shadow-md transition-all flex items-center justify-between gap-2"
              >
                <div className="min-w-0">
                  <p className="text-[10px] sm:text-xs font-bold text-slate-500 uppercase tracking-wider truncate">
                    {card.title}
                  </p>
                  <p className="text-lg sm:text-2xl font-bold text-slate-900 mt-0.5">{card.value}</p>
                </div>
                <div className={`p-2 sm:p-3 rounded-xl border shrink-0 ${card.color}`}>
                  <Icon className="w-4 h-4 sm:w-5 sm:h-5" />
                </div>
              </div>
            );
          })}
      </div>

      {/* Task Overview Section */}
      <div className="bg-white border border-slate-200/80 rounded-2xl shadow-sm overflow-hidden">
        {/* Section Header & Filters */}
        <div className="p-4 sm:p-5 border-b border-slate-200 flex flex-col md:flex-row md:items-center justify-between gap-3 bg-slate-50/50">
          <div className="flex items-center gap-2">
            <h2 className="text-base sm:text-lg font-bold text-slate-900">Task Overview</h2>
            <span className="text-xs font-bold px-2 py-0.5 rounded-full bg-slate-200/80 text-slate-700">
              {tasksResult?.totalCount ?? 0}
            </span>
          </div>

          {/* Filters Bar */}
          <div className="flex flex-wrap items-center gap-2">
            <div className="flex items-center gap-1 text-xs font-semibold text-slate-500">
              <Filter className="w-3.5 h-3.5" />
              <span>Filters:</span>
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
        </div>

        {/* Content */}
        {isLoading ? (
          <SkeletonTable rows={5} />
        ) : tasksResult?.items?.length === 0 ? (
          <EmptyState
            icon={CheckSquare}
            title="No tasks match your filters"
            description="Try adjusting your status or priority filters."
          />
        ) : (
          <>
            {/* Desktop & Tablet Table (>= 768px) */}
            <div className="hidden md:block overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="bg-slate-50/75 border-b border-slate-200 text-xs font-bold text-slate-500 uppercase tracking-wider">
                    <th className="py-3.5 px-6">Task</th>
                    <th className="py-3.5 px-4">Status</th>
                    <th className="py-3.5 px-4">Priority</th>
                    <th className="py-3.5 px-4">Deadline</th>
                    <th className="py-3.5 px-4">Assignee</th>
                    <th className="py-3.5 px-4">Team</th>
                    <th className="py-3.5 px-6 text-right">Action</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-sm">
                  {tasksResult?.items?.map((task) => (
                    <tr
                      key={task.id}
                      onClick={() => navigate(`/tasks/${task.id}`)}
                      className="hover:bg-slate-50/80 cursor-pointer transition-colors"
                    >
                      <td className="py-4 px-6 font-semibold text-slate-900 max-w-xs truncate">
                        {task.title}
                      </td>
                      <td className="py-4 px-4">
                        <StatusBadge status={task.status} />
                      </td>
                      <td className="py-4 px-4">
                        <PriorityBadge priority={task.priority} />
                      </td>
                      <td className="py-4 px-4 text-slate-600">{formatDate(task.deadline)}</td>
                      <td className="py-4 px-4 text-slate-700 font-medium">{task.assignedToName || 'Unassigned'}</td>
                      <td className="py-4 px-4 text-slate-500">{task.teamName || '—'}</td>
                      <td className="py-4 px-6 text-right">
                        <span className="text-xs font-bold text-indigo-600 hover:text-indigo-800">
                          Details &rarr;
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Mobile Card List (< 768px) */}
            <div className="md:hidden divide-y divide-slate-100">
              {tasksResult?.items?.map((task) => (
                <div
                  key={task.id}
                  onClick={() => navigate(`/tasks/${task.id}`)}
                  className="p-4 hover:bg-slate-50/80 cursor-pointer transition-colors space-y-2.5"
                >
                  <div className="flex items-start justify-between gap-2">
                    <h3 className="font-bold text-slate-900 text-sm leading-snug break-words flex-1">
                      {task.title}
                    </h3>
                    <PriorityBadge priority={task.priority} />
                  </div>

                  <div className="flex items-center gap-2">
                    <StatusBadge status={task.status} />
                    <span className="text-xs text-slate-500">
                      Due {formatDate(task.deadline)}
                    </span>
                  </div>

                  <div className="flex items-center justify-between text-xs text-slate-600 pt-1">
                    <div className="flex items-center gap-1 truncate">
                      <User className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                      <span className="truncate">{task.assignedToName || 'Unassigned'}</span>
                    </div>

                    {task.teamName && (
                      <div className="flex items-center gap-1 text-slate-500 truncate ml-2">
                        <Users2 className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                        <span className="truncate">{task.teamName}</span>
                      </div>
                    )}
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
    </div>
  );
}
