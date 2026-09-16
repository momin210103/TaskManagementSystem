import apiClient from "../api/client";

export const dashboardService = {
  async getSummary() {
    const response = await apiClient.get("/dashboard/summary");
    return response.data;
  },

  async getTasks(params = {}) {
    const response = await apiClient.get("/dashboard/tasks", { params });
    return response.data;
  },
};
