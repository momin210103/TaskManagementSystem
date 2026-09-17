import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  LayoutDashboard,
  CheckSquare,
  Users2,
  UserCheck,
  Bell,
  X,
  Layers,
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';

export function Sidebar({ isOpen, onClose }) {
  const { user } = useAuth();

  const navItems = [
    {
      label: 'Dashboard',
      path: '/dashboard',
      icon: LayoutDashboard,
      roles: ['Admin', 'Manager', 'User'],
    },
    {
      label: 'Users',
      path: '/users',
      icon: UserCheck,
      roles: ['Admin'],
    },
    {
      label: 'Teams',
      path: '/teams',
      icon: Users2,
      roles: ['Admin', 'Manager'],
    },
    {
      label: 'Tasks',
      path: '/tasks',
      icon: CheckSquare,
      roles: ['Admin', 'Manager', 'User'],
    },
    {
      label: 'Notifications',
      path: '/notifications',
      icon: Bell,
      roles: ['Admin', 'Manager', 'User'],
    },
  ];

  const filteredNavItems = navItems.filter((item) => {
    if (!item.roles) return true;

    return item.roles.some(
      (role) =>
        role.toLowerCase() === user?.role?.toLowerCase()
    );
  });

  return (
    <>
      {/* Mobile / Tablet Overlay */}
      {isOpen && (
        <div
          className="
            fixed inset-0
            z-40
            bg-slate-900/50
            backdrop-blur-sm
            lg:hidden
          "
          onClick={onClose}
          aria-hidden="true"
        />
      )}

      {/* Sidebar */}
      <aside
        className={`
          fixed
          top-0
          bottom-0
          left-0
          z-50
          w-64
          bg-slate-900
          text-slate-300
          flex
          flex-col
          shadow-xl
          transition-transform
          duration-300
          ease-in-out

          /* Mobile / Tablet */
          -translate-x-full

          /* Desktop */
          lg:translate-x-0

          /* Open state for Mobile / Tablet */
          ${isOpen ? 'translate-x-0' : ''}
        `}
        aria-label="Main Navigation"
      >
        {/* =========================
            Logo / Brand
        ========================== */}
        <div className="h-16 flex items-center justify-between px-5 border-b border-slate-800">
          <NavLink
            to="/dashboard"
            className="flex items-center gap-3"
            onClick={onClose}
          >
            <div className="w-9 h-9 rounded-lg bg-indigo-600 flex items-center justify-center">
              <Layers className="w-5 h-5 text-white" />
            </div>

            <div>
              <h1 className="text-white font-bold text-lg leading-none">
                TaskFlow
              </h1>

              <p className="text-xs text-slate-500 mt-1">
                Management System
              </p>
            </div>
          </NavLink>

          {/* Close button only visible on mobile/tablet */}
          <button
            type="button"
            onClick={onClose}
            className="
              lg:hidden
              p-2
              rounded-lg
              text-slate-400
              hover:text-white
              hover:bg-slate-800
              transition-colors
            "
            aria-label="Close navigation"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* =========================
            Navigation
        ========================== */}
        <nav className="flex-1 px-3 py-6 overflow-y-auto">
          <p className="px-3 mb-3 text-xs font-semibold uppercase tracking-wider text-slate-500">
            Navigation
          </p>

          <div className="space-y-1">
            {filteredNavItems.map((item) => {
              const Icon = item.icon;

              return (
                <NavLink
                  key={item.path}
                  to={item.path}
                  onClick={() => {
                    // Close only matters on mobile/tablet.
                    // Desktop sidebar remains visible because
                    // lg:translate-x-0 always applies.
                    onClose?.();
                  }}
                  className={({ isActive }) =>
                    `
                    group
                    flex
                    items-center
                    gap-3
                    px-3
                    py-2.5
                    rounded-lg
                    text-sm
                    font-medium
                    transition-all
                    duration-200

                    ${
                      isActive
                        ? `
                          bg-indigo-600
                          text-white
                          shadow-lg
                          shadow-indigo-900/20
                        `
                        : `
                          text-slate-400
                          hover:bg-slate-800
                          hover:text-white
                        `
                    }
                  `
                  }
                >
                  <Icon className="w-5 h-5 flex-shrink-0" />

                  <span>{item.label}</span>
                </NavLink>
              );
            })}
          </div>
        </nav>

        {/* =========================
            User Information
        ========================== */}
        <div className="border-t border-slate-800 p-4">
          <div className="flex items-center gap-3">
            {/* Avatar */}
            <div
              className="
                w-9
                h-9
                rounded-full
                bg-indigo-600
                flex
                items-center
                justify-center
                text-white
                font-semibold
                text-sm
                flex-shrink-0
              "
            >
              {user?.email?.charAt(0)?.toUpperCase() || 'U'}
            </div>

            {/* User Info */}
            <div className="min-w-0 flex-1">
              <p className="text-sm font-medium text-white truncate">
                {user?.email || 'User'}
              </p>

              <p className="text-xs text-slate-500 truncate">
                {user?.role || 'User'}
              </p>
            </div>
          </div>
        </div>
      </aside>
    </>
  );
}