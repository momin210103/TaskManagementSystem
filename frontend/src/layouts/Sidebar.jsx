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
  const { user } = useAuth();

  const navItems = [
    {
      label: 'Dashboard',
      path: '/dashboard',
      icon: LayoutDashboard,
      roles: ['Admin', 'Manager', 'User']
    },
    {
      label: 'Users',
      path: '/users',
      icon: UserCheck,
      roles: ['Admin']
    },
    {
      label: 'Teams',
      path: '/teams',
      icon: Users2,
      roles: ['Admin', 'Manager']
    },
    {
      label: 'Tasks',
      path: '/tasks',
      icon: CheckSquare,
      roles: ['Admin', 'Manager', 'User']
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
          className="fixed inset-0 z-40 bg-slate-900/50 backdrop-blur-sm lg:hidden transition-opacity duration-300"
          aria-hidden="true"
        />
      )}

      {/* Sidebar Container */}
      <aside
        className={`fixed top-0 bottom-0 left-0 z-50 w-64 bg-slate-900 text-slate-300 flex flex-col transition-transform duration-300 ease-in-out lg:translate-x-0 ${isOpen ? 'translate-x-0 shadow-2xl' : '-translate-x-full'
          }`}
        aria-label="Main Navigation"
      >
        {/* Brand Header */}
        <div className="flex items-center justify-between h-16 px-5 bg-slate-950 border-b border-slate-800 shrink-0">
          <div className="flex items-center gap-3">
            <div className="flex items-center justify-center w-9 h-9 rounded-xl bg-indigo-600 text-white shadow-md shadow-indigo-500/20">
              <Layers className="w-5 h-5" />
            </div>
            <div>
              <span className="text-base font-bold tracking-tight text-white block leading-none">TaskFlow</span>
              <span className="text-[10px] font-bold text-indigo-400 uppercase tracking-wider block mt-1">
                Workspace
              </span>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 lg:hidden transition-colors"
            aria-label="Close navigation"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Navigation Links */}
        <nav className="flex-1 px-3.5 py-5 space-y-1.5 overflow-y-auto">
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
                <Icon className="w-4 h-4 shrink-0" />
                <span>{item.label}</span>
              </NavLink>
            );
          })}
        </nav>

        {/* User Account & Role Badge Footer */}
        <div className="p-3.5 m-3 rounded-xl bg-slate-800/60 border border-slate-700/50 shrink-0">
          <div className="text-[11px] text-slate-400 uppercase font-semibold tracking-wider">Signed in as</div>
          <div className="text-sm font-bold text-white truncate mt-0.5">{user?.name || 'User'}</div>
          <div className="flex items-center justify-between mt-2 pt-2 border-t border-slate-700/40">
            <span className="text-xs text-slate-400">Role</span>
            <span
              className={`inline-block px-2 py-0.5 rounded text-[11px] font-bold tracking-wider uppercase border ${user?.role === 'Admin'
                  ? 'bg-purple-950 text-purple-300 border-purple-800/60'
                  : user?.role === 'Manager'
                    ? 'bg-indigo-950 text-indigo-300 border-indigo-700/60'
                    : 'bg-slate-800 text-slate-300 border-slate-700'
                }`}
            >
              {user?.role || 'User'}
            </span>
          </div>
        </div>
      </aside>
    </>
  );
}
