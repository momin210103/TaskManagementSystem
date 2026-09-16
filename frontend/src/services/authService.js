import apiClient from "../api/client";

export const authService = {
  async login(email, password) {
    const response = await apiClient.post("/auth/login", { email, password });
    return response.data;
  },

  async register(name, email, password) {
    const response = await apiClient.post("/auth/register", {
      name,
      email,
      password,
    });
    return response.data;
  },

  async refreshToken(refreshToken) {
    const response = await apiClient.post("/auth/refresh", { refreshToken });
    return response.data;
  },

  async logout(refreshToken) {
    try {
      const response = await apiClient.post("/auth/logout", { refreshToken });
      return response.data;
    } catch {
      // Logout should still proceed client-side even if backend call fails
      return { message: "Logged out." };
    }
  },
};
