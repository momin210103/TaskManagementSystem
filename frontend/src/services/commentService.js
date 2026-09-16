import apiClient from "../api/client";

export const commentService = {
  async getComments(taskId) {
    const response = await apiClient.get(`/tasks/${taskId}/comments`);
    return response.data;
  },

  async createComment(taskId, content) {
    const response = await apiClient.post(`/tasks/${taskId}/comments`, {
      content,
    });
    return response.data;
  },

  async deleteComment(taskId, commentId) {
    const response = await apiClient.delete(
      `/tasks/${taskId}/comments/${commentId}`,
    );
    return response.data;
  },
};
