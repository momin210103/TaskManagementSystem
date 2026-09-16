import apiClient from "../api/client";

export const userService = {
  async getCurrentUser() {
    const response = await apiClient.get("/users/me");
    return response.data;
  },

  async getUsers(params = {}) {
    const response = await apiClient.get("/users", { params });
    return response.data;
  },

  async getUserById(id) {
    const response = await apiClient.get(`/users/${id}`);
    return response.data;
  },

  async updateUserRole(id, role) {
    const response = await apiClient.patch(`/users/${id}/role`, { role });
    return response.data;
  },

  async updateUserTeam(id, teamId) {
    const response = await apiClient.patch(`/users/${id}/team`, { teamId });
    return response.data;
  },
};
