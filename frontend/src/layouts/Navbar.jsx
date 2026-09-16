import React, { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { Menu, Bell, LogOut, User } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { notificationService } from '../services/notificationService';
import { getInitials } from '../utils/formatters';

export function Navbar({ onMenuClick }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [unreadCount, setUnreadCount] = useState(0);

  useEffect(() => {
    async function fetchUnreadCount() {
      try {
        const data = await notificationService.getNotifications({ page: 1, pageSize: 50, isRead: false });
        setUnreadCount(data.totalCount || 0);
      } catch {
        // Ignore background notification counter errors
      }
    }

    fetchUnreadCount();
    const interval = setInterval(fetchUnreadCount, 30000);
    return () => clearInterval(interval);
  }, []);

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <header className="sticky top-0 z-30 flex items-center justify-between h-16 px-4 sm:px-6 bg-white border-b border-slate-200 shadow-sm">
      <div className="flex items-center gap-3">
        <button
          onClick={onMenuClick}
          className="p-2 text-slate-500 rounded-lg lg:hidden hover:text-slate-700 hover:bg-slate-100"
          aria-label="Open sidebar"
        >
          <Menu className="w-5 h-5" />
        </button>
        <span className="text-sm font-medium text-slate-500 hidden sm:inline-block">
          Welcome back, <span className="font-semibold text-slate-800">{user?.name}</span>
        </span>
      </div>

      <div className="flex items-center gap-2 sm:gap-4">
        {/* Notification Icon */}
        <Link
          to="/notifications"
          className="relative p-2 text-slate-500 hover:text-indigo-600 rounded-lg hover:bg-slate-100 transition-colors"
          title="View Notifications"
        >
          <Bell className="w-5 h-5" />
          {unreadCount > 0 && (
            <span className="absolute top-1 right-1 flex items-center justify-center min-w-[18px] h-[18px] px-1 text-[10px] font-bold text-white bg-rose-500 rounded-full border-2 border-white">
              {unreadCount > 99 ? '99+' : unreadCount}
            </span>
          )}
        </Link>

        <div className="h-6 w-px bg-slate-200 mx-1" />

        {/* User Info & Avatar */}
        <div className="flex items-center gap-3">
          <div className="flex items-center justify-center w-9 h-9 rounded-full bg-indigo-100 text-indigo-700 font-bold text-xs shadow-inner">
            {getInitials(user?.name)}
          </div>
          <div className="hidden md:block text-left">
            <div className="text-sm font-semibold text-slate-900 leading-tight">{user?.name}</div>
            <div className="text-xs text-slate-500">{user?.email}</div>
          </div>
        </div>

        {/* Logout Button */}
        <button
          onClick={handleLogout}
          className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-slate-700 hover:text-rose-600 hover:bg-rose-50 border border-slate-200 rounded-lg transition-colors ml-2"
          title="Sign Out"
        >
          <LogOut className="w-4 h-4" />
          <span className="hidden sm:inline">Logout</span>
        </button>
      </div>
    </header>
  );
}

