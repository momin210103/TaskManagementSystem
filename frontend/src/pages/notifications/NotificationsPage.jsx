import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Bell, CheckCheck, CheckSquare, Clock, Filter, ArrowRight } from 'lucide-react';
import { notificationService } from '../../services/notificationService';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import { ErrorMessage } from '../../components/common/ErrorMessage';
import { EmptyState } from '../../components/common/EmptyState';
import { Pagination } from '../../components/common/Pagination';
import { formatDateTime } from '../../utils/formatters';

export function NotificationsPage() {
  const navigate = useNavigate();

  const [notificationsResult, setNotificationsResult] = useState(null);
  const [filterRead, setFilterRead] = useState(''); // '' for all, 'false' for unread, 'true' for read
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [isLoading, setIsLoading] = useState(true);
  const [isMarkingAll, setIsMarkingAll] = useState(false);
  const [error, setError] = useState(null);

  const fetchNotifications = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await notificationService.getNotifications({
        page,
        pageSize,
        isRead: filterRead === '' ? undefined : filterRead === 'true'
      });
      setNotificationsResult(data);
    } catch (err) {
      setError(err.message || 'Failed to load notifications.');
    } finally {
      setIsLoading(false);
    }
  }, [page, pageSize, filterRead]);

  useEffect(() => {
    fetchNotifications();
  }, [fetchNotifications]);

  const handleMarkAsRead = async (notification) => {
    try {
      if (!notification.isRead) {
        await notificationService.markAsRead(notification.id);
        fetchNotifications();
      }
      if (notification.taskId) {
        navigate(`/tasks/${notification.taskId}`);
      }
    } catch (err) {
      setError(err.message || 'Failed to mark notification as read.');
    }
  };

  const handleMarkAllAsRead = async () => {
    setIsMarkingAll(true);
    try {
      await notificationService.markAllAsRead();
      fetchNotifications();
    } catch (err) {
      setError(err.message || 'Failed to mark all as read.');
    } finally {
      setIsMarkingAll(false);
    }
  };

  return (
    <div className="space-y-6 max-w-4xl mx-auto animate-fade-in">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-slate-900 tracking-tight">Notifications</h1>
          <p className="text-sm text-slate-500 mt-1">Updates on your assigned tasks, status changes, and assignments</p>
        </div>

        <button
          onClick={handleMarkAllAsRead}
          disabled={isMarkingAll || notificationsResult?.totalCount === 0}
          className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold text-slate-700 bg-white border border-slate-200 rounded-xl hover:bg-slate-50 transition-colors shadow-sm disabled:opacity-50"
        >
          <CheckCheck className="w-4 h-4 text-indigo-600" />
          {isMarkingAll ? 'Marking all...' : 'Mark All as Read'}
        </button>
      </div>

      <ErrorMessage message={error} onDismiss={() => setError(null)} retry={fetchNotifications} />

      {/* Main Container */}
      <div className="bg-white border border-slate-200/80 rounded-2xl shadow-sm overflow-hidden">
        {/* Filters */}
        <div className="p-4 border-b border-slate-200 flex items-center justify-between bg-slate-50/50">
          <div className="flex items-center gap-2">
            <Filter className="w-3.5 h-3.5 text-slate-400" />
            <select
              value={filterRead}
              onChange={(e) => {
                setFilterRead(e.target.value);
                setPage(1);
              }}
              className="text-xs border border-slate-200 rounded-lg px-2.5 py-1.5 bg-white text-slate-700 focus:ring-1 focus:ring-indigo-500"
            >
              <option value="">All Notifications</option>
              <option value="false">Unread Only</option>
              <option value="true">Read Only</option>
            </select>
          </div>

          <span className="text-xs font-semibold text-slate-500">
            {notificationsResult?.totalCount ?? 0} Total
          </span>
        </div>

        {/* List */}
        {isLoading ? (
          <LoadingSpinner text="Loading notifications..." />
        ) : notificationsResult?.items?.length === 0 ? (
          <EmptyState
            icon={Bell}
            title="No notifications"
            description="You're all caught up! No notifications match the current filter."
          />
        ) : (
          <>
            <div className="divide-y divide-slate-100">
              {notificationsResult?.items?.map((item) => (
                <div
                  key={item.id}
                  onClick={() => handleMarkAsRead(item)}
                  className={`p-4 sm:p-5 flex items-start justify-between gap-4 cursor-pointer transition-colors ${item.isRead ? 'bg-white hover:bg-slate-50/80' : 'bg-indigo-50/30 hover:bg-indigo-50/50'
                    }`}
                >
                  <div className="flex items-start gap-3.5">
                    <div
                      className={`p-2.5 rounded-xl shrink-0 mt-0.5 ${item.type === 'Assignment'
                          ? 'bg-indigo-100 text-indigo-700'
                          : 'bg-amber-100 text-amber-700'
                        }`}
                    >
                      <CheckSquare className="w-5 h-5" />
                    </div>

                    <div className="space-y-1">
                      <div className="flex items-center gap-2">
                        <span
                          className={`text-xs font-semibold uppercase tracking-wider ${item.type === 'Assignment' ? 'text-indigo-600' : 'text-amber-600'
                            }`}
                        >
                          {item.type === 'Assignment' ? 'New Assignment' : 'Status Update'}
                        </span>
                        {!item.isRead && (
                          <span className="w-2 h-2 rounded-full bg-indigo-600 inline-block" title="Unread" />
                        )}
                      </div>

                      <p className={`text-sm ${item.isRead ? 'text-slate-700' : 'font-semibold text-slate-900'}`}>
                        {item.message}
                      </p>

                      <div className="flex items-center gap-1 text-xs text-slate-400 pt-1">
                        <Clock className="w-3.5 h-3.5" />
                        <span>{formatDateTime(item.createdAt)}</span>
                      </div>
                    </div>
                  </div>

                  {item.taskId && (
                    <div className="flex items-center text-xs font-medium text-indigo-600 hover:text-indigo-800 shrink-0 pt-1">
                      <span>View Task</span>
                      <ArrowRight className="w-3.5 h-3.5 ml-1" />
                    </div>
                  )}
                </div>
              ))}
            </div>

            <div className="px-4 sm:px-6">
              <Pagination
                currentPage={page}
                pageSize={pageSize}
                totalCount={notificationsResult?.totalCount || 0}
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

