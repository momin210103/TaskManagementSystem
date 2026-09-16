import apiClient from "../api/client";

export const teamService = {
  async getTeams() {
    const response = await apiClient.get("/teams");
    return response.data;
  },

  async getTeamById(id) {
    const response = await apiClient.get(`/teams/${id}`);
    return response.data;
  },

  async createTeam(data) {
    const response = await apiClient.post("/teams", data);
    return response.data;
  },

  async updateTeam(id, data) {
    const response = await apiClient.put(`/teams/${id}`, data);
    return response.data;
  },

  async addMember(teamId, userId) {
    const response = await apiClient.post(`/teams/${teamId}/members`, {
      userId,
    });
    return response.data;
  },

  async removeMember(teamId, userId) {
    const response = await apiClient.delete(
      `/teams/${teamId}/members/${userId}`,
    );
    return response.data;
  },
};
