import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { authService } from '../services/authService';
import { userService } from '../services/userService';
import { ROLES } from '../utils/constants';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    const saved = localStorage.getItem('tm_user');
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch {
        return null;
      }
    }
    return null;
  });

  const [accessToken, setAccessToken] = useState(() => localStorage.getItem('tm_access_token'));
  const [refreshToken, setRefreshToken] = useState(() => localStorage.getItem('tm_refresh_token'));
  const [isLoading, setIsLoading] = useState(true);

  // Sync state & verify with backend on initial load
  useEffect(() => {
    async function initAuth() {
      const token = localStorage.getItem('tm_access_token');
      if (token) {
        try {
          const profile = await userService.getCurrentUser();
          setUser((prev) => ({ ...prev, ...profile }));
          localStorage.setItem('tm_user', JSON.stringify(profile));
        } catch {
          // If token verification fails, clear auth
          logout();
        }
      }
      setIsLoading(false);
    }

    initAuth();
  }, []);

  const login = async (email, password) => {
    setIsLoading(true);
    try {
      const data = await authService.login(email, password);
      const { accessToken: newAccess, refreshToken: newRefresh, user: authUser } = data;

      localStorage.setItem('tm_access_token', newAccess);
      localStorage.setItem('tm_refresh_token', newRefresh);
      localStorage.setItem('tm_user', JSON.stringify(authUser));

      setAccessToken(newAccess);
      setRefreshToken(newRefresh);
      setUser(authUser);
      return authUser;
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (name, email, password) => {
    setIsLoading(true);
    try {
      const data = await authService.register(name, email, password);
      const { accessToken: newAccess, refreshToken: newRefresh, user: authUser } = data;

      localStorage.setItem('tm_access_token', newAccess);
      localStorage.setItem('tm_refresh_token', newRefresh);
      localStorage.setItem('tm_user', JSON.stringify(authUser));

      setAccessToken(newAccess);
      setRefreshToken(newRefresh);
      setUser(authUser);
      return authUser;
    } finally {
      setIsLoading(false);
    }
  };

  const logout = useCallback(async () => {
    const rToken = localStorage.getItem('tm_refresh_token');
    if (rToken) {
      await authService.logout(rToken);
    }

    localStorage.removeItem('tm_access_token');
    localStorage.removeItem('tm_refresh_token');
    localStorage.removeItem('tm_user');

    setAccessToken(null);
    setRefreshToken(null);
    setUser(null);
  }, []);

  const refreshProfile = async () => {
    try {
      const profile = await userService.getCurrentUser();
      setUser((prev) => ({ ...prev, ...profile }));
      localStorage.setItem('tm_user', JSON.stringify(profile));
      return profile;
    } catch (err) {
      console.error('Failed to refresh profile', err);
    }
  };

  const isAuthenticated = !!accessToken && !!user;
  const isAdmin = user?.role?.toLowerCase() === ROLES.ADMIN.toLowerCase();
  const isManager = user?.role?.toLowerCase() === ROLES.MANAGER.toLowerCase();
  const isUser = user?.role?.toLowerCase() === ROLES.USER.toLowerCase();

  const value = {
    user,
    accessToken,
    refreshToken,
    isAuthenticated,
    isLoading,
    isAdmin,
    isManager,
    isUser,
    login,
    register,
    logout,
    refreshProfile
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}

