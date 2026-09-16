import apiClient from "../api/client";

export const notificationService = {
  async getNotifications(params = {}) {
    const response = await apiClient.get("/notifications", { params });
    return response.data;
  },

  async markAsRead(id) {
    const response = await apiClient.patch(`/notifications/${id}/read`);
    return response.data;
  },

  async markAllAsRead() {
    const response = await apiClient.patch("/notifications/read-all");
    return response.data;
  },
};
