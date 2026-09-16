import React, { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { Menu, Bell, LogOut } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { notificationService } from '../services/notificationService';
import { getInitials } from '../utils/formatters';

export function Navbar({ onMenuClick }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [unreadCount, setUnreadCount] = useState(0);

  useEffect(() => {
    let isMounted = true;

    async function fetchUnreadCount() {
      try {
        const data = await notificationService.getNotifications({ page: 1, pageSize: 50, isRead: false });
        if (isMounted) {
          setUnreadCount(data.totalCount || 0);
        }
      } catch {
        // Ignore notification badge polling errors
      }
    }

    fetchUnreadCount();
    const interval = setInterval(fetchUnreadCount, 30000);

    return () => {
      isMounted = false;
      clearInterval(interval);
    };
  }, []);

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <header className="sticky top-0 z-30 flex items-center justify-between h-16 px-3 sm:px-6 bg-white border-b border-slate-200 shadow-sm shrink-0">
      <div className="flex items-center gap-2 sm:gap-3 min-w-0">
        <button
          onClick={onMenuClick}
          className="p-2 text-slate-600 rounded-xl lg:hidden hover:text-slate-900 hover:bg-slate-100 transition-colors shrink-0"
          aria-label="Open sidebar menu"
        >
          <Menu className="w-5 h-5" />
        </button>

        <div className="min-w-0">
          <span className="text-xs sm:text-sm font-medium text-slate-500 truncate block">
            Welcome, <span className="font-bold text-slate-900">{user?.name}</span>
          </span>
        </div>
      </div>

      <div className="flex items-center gap-1.5 sm:gap-3 shrink-0">
        {/* Notification Icon */}
        <Link
          to="/notifications"
          className="relative p-2 text-slate-500 hover:text-indigo-600 rounded-xl hover:bg-slate-100 transition-colors"
          title="Notifications"
          aria-label="View notifications"
        >
          <Bell className="w-5 h-5" />
          {unreadCount > 0 && (
            <span className="absolute top-1 right-1 flex items-center justify-center min-w-[18px] h-[18px] px-1 text-[10px] font-bold text-white bg-rose-500 rounded-full border-2 border-white">
              {unreadCount > 99 ? '99+' : unreadCount}
            </span>
          )}
        </Link>

        <div className="h-5 w-px bg-slate-200 mx-0.5 sm:mx-1" />

        {/* User Avatar */}
        <div className="flex items-center gap-2.5">
          <div
            className="flex items-center justify-center w-8 h-8 sm:w-9 sm:h-9 rounded-full bg-indigo-100 text-indigo-700 font-bold text-xs shadow-inner shrink-0"
            title={`${user?.name} (${user?.role})`}
          >
            {getInitials(user?.name)}
          </div>
          <div className="hidden md:block text-left">
            <div className="text-xs sm:text-sm font-bold text-slate-900 leading-tight truncate max-w-[140px]">
              {user?.name}
            </div>
            <div className="text-[11px] text-slate-500 truncate max-w-[140px]">{user?.email}</div>
          </div>
        </div>

        {/* Logout Button */}
        <button
          onClick={handleLogout}
          className="inline-flex items-center gap-1.5 px-2.5 sm:px-3 py-1.5 text-xs font-semibold text-slate-700 hover:text-rose-600 hover:bg-rose-50 border border-slate-200 rounded-xl transition-colors ml-1"
          title="Sign Out"
        >
          <LogOut className="w-4 h-4" />
          <span className="hidden sm:inline">Logout</span>
        </button>
      </div>
    </header>
  );
}
