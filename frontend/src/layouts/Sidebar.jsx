import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  LayoutDashboard,
  CheckSquare,
  Users2,
  UserCheck,
  Bell,
  X,
  Layers
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';

export function Sidebar({ isOpen, onClose }) {
  const { user, isAdmin, isManager } = useAuth();

  const navItems = [
    {
      label: 'Dashboard',
      path: '/dashboard',
      icon: LayoutDashboard,
      roles: ['Admin', 'Manager', 'User']
    },
    {
      label: 'Tasks',
      path: '/tasks',
      icon: CheckSquare,
      roles: ['Admin', 'Manager', 'User']
    },
    {
      label: 'Teams',
      path: '/teams',
      icon: Users2,
      roles: ['Admin', 'Manager', 'User']
    },
    {
      label: 'Users',
      path: '/users',
      icon: UserCheck,
      roles: ['Admin', 'Manager']
    },
    {
      label: 'Notifications',
      path: '/notifications',
      icon: Bell,
      roles: ['Admin', 'Manager', 'User']
    }
  ];

  const filteredNavItems = navItems.filter(
    (item) => !item.roles || item.roles.some((r) => r.toLowerCase() === user?.role?.toLowerCase())
  );

  return (
    <>
      {/* Mobile Backdrop */}
      {isOpen && (
        <div
          onClick={onClose}
          className="fixed inset-0 z-40 bg-slate-900/40 backdrop-blur-sm lg:hidden transition-opacity"
        />
      )}

      {/* Sidebar Container */}
      <aside
        className={`fixed top-0 bottom-0 left-0 z-40 w-64 bg-slate-900 text-slate-300 flex flex-col transition-transform duration-300 ease-in-out lg:translate-x-0 ${isOpen ? 'translate-x-0' : '-translate-x-full'
          }`}
      >
        {/* Brand Header */}
        <div className="flex items-center justify-between h-16 px-6 bg-slate-950 border-b border-slate-800">
          <div className="flex items-center gap-3">
            <div className="flex items-center justify-center w-9 h-9 rounded-lg bg-indigo-600 text-white shadow-md shadow-indigo-500/20">
              <Layers className="w-5 h-5" />
            </div>
            <div>
              <span className="text-base font-bold tracking-tight text-white">TaskFlow</span>
              <span className="block text-[10px] font-semibold text-indigo-400 uppercase tracking-wider">
                Management
              </span>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1 text-slate-400 hover:text-white rounded-lg lg:hidden"
            aria-label="Close sidebar"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Navigation Links */}
        <nav className="flex-1 px-4 py-6 space-y-1.5 overflow-y-auto">
          {filteredNavItems.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.path}
                to={item.path}
                onClick={onClose}
                className={({ isActive }) =>
                  `flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-sm font-medium transition-all ${isActive
                    ? 'bg-indigo-600 text-white shadow-sm shadow-indigo-600/30'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
                  }`
                }
              >
                <Icon className="w-5 h-5 shrink-0" />
                <span>{item.label}</span>
              </NavLink>
            );
          })}
        </nav>

        {/* Role & User Footer */}
        <div className="p-4 m-4 rounded-xl bg-slate-800/60 border border-slate-700/50">
          <div className="text-xs text-slate-400">Logged in as</div>
          <div className="text-sm font-semibold text-white truncate mt-0.5">{user?.name}</div>
          <div className="inline-block mt-2 px-2 py-0.5 rounded text-[11px] font-semibold bg-indigo-950 text-indigo-300 border border-indigo-700/50">
            {user?.role}
          </div>
        </div>
      </aside>
    </>
  );
}

