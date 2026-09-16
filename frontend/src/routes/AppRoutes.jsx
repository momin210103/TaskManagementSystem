import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { RoleRoute } from './RoleRoute';
import { AppLayout } from '../layouts/AppLayout';

import { LoginPage } from '../pages/auth/LoginPage';
import { RegisterPage } from '../pages/auth/RegisterPage';
import { DashboardPage } from '../pages/dashboard/DashboardPage';
import { TasksListPage } from '../pages/tasks/TasksListPage';
import { TaskDetailsPage } from '../pages/tasks/TaskDetailsPage';
import { TeamsListPage } from '../pages/teams/TeamsListPage';
import { UsersListPage } from '../pages/users/UsersListPage';
import { NotificationsPage } from '../pages/notifications/NotificationsPage';
import { NotFoundPage } from '../pages/NotFoundPage';
import { ForbiddenPage } from '../pages/ForbiddenPage';

export function AppRoutes() {
  return (
    <Routes>
      {/* Public Auth Routes */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/forbidden" element={<ForbiddenPage />} />

      {/* Protected App Routes */}
      <Route
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/tasks" element={<TasksListPage />} />
        <Route path="/tasks/:id" element={<TaskDetailsPage />} />
        <Route path="/teams" element={<TeamsListPage />} />
        <Route
          path="/users"
          element={
            <RoleRoute allowedRoles={['Admin', 'Manager']}>
              <UsersListPage />
            </RoleRoute>
          }
        />
        <Route path="/notifications" element={<NotificationsPage />} />
      </Route>

      {/* 404 Catch All */}
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

