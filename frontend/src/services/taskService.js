import apiClient from "../api/client";

export const taskService = {
  async getTasks(params = {}) {
    const response = await apiClient.get("/tasks", { params });
    return response.data;
  },

  async getTaskById(id) {
    const response = await apiClient.get(`/tasks/${id}`);
    return response.data;
  },

  async createTask(data) {
    const response = await apiClient.post("/tasks", data);
    return response.data;
  },

  async updateTask(id, data) {
    const response = await apiClient.put(`/tasks/${id}`, data);
    return response.data;
  },

  async updateTaskStatus(id, status) {
    const response = await apiClient.patch(`/tasks/${id}/status`, { status });
    return response.data;
  },

  async assignTask(id, assignedToId) {
    const response = await apiClient.patch(`/tasks/${id}/assign`, {
      assignedToId,
    });
    return response.data;
  },

  async deleteTask(id) {
    const response = await apiClient.delete(`/tasks/${id}`);
    return response.data;
  },
};
