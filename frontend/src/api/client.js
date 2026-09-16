import axios from "axios";

const baseURL =
  import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api";

const apiClient = axios.create({
  baseURL,
  headers: {
    "Content-Type": "application/json",
  },
});

let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("tm_access_token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Handle 401 Unauthorized
    if (
      error.response?.status === 401 &&
      !originalRequest._retry &&
      !originalRequest.url?.includes("/auth/login") &&
      !originalRequest.url?.includes("/auth/register")
    ) {
      if (originalRequest.url?.includes("/auth/refresh")) {
        // Refresh token itself failed/expired
        localStorage.removeItem("tm_access_token");
        localStorage.removeItem("tm_refresh_token");
        localStorage.removeItem("tm_user");
        window.location.href = "/login";
        return Promise.reject(error);
      }

      const refreshToken = localStorage.getItem("tm_refresh_token");
      if (!refreshToken) {
        localStorage.removeItem("tm_access_token");
        localStorage.removeItem("tm_user");
        return Promise.reject(error);
      }

      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return apiClient(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const response = await axios.post(`${baseURL}/auth/refresh`, {
          refreshToken,
        });

        const {
          accessToken,
          refreshToken: newRefreshToken,
          user,
        } = response.data;
        localStorage.setItem("tm_access_token", accessToken);
        if (newRefreshToken) {
          localStorage.setItem("tm_refresh_token", newRefreshToken);
        }
        if (user) {
          localStorage.setItem("tm_user", JSON.stringify(user));
        }

        apiClient.defaults.headers.common.Authorization = `Bearer ${accessToken}`;
        processQueue(null, accessToken);

        originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError, null);
        localStorage.removeItem("tm_access_token");
        localStorage.removeItem("tm_refresh_token");
        localStorage.removeItem("tm_user");
        window.location.href = "/login";
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    // Normalize error messages from RFC 7807 ProblemDetails
    const problemDetail = error.response?.data;
    let errorMessage = "An unexpected error occurred. Please try again.";

    if (problemDetail) {
      if (problemDetail.detail) {
        errorMessage = problemDetail.detail;
      } else if (problemDetail.title) {
        errorMessage = problemDetail.title;
      } else if (typeof problemDetail === "string") {
        errorMessage = problemDetail;
      }
    } else if (error.message) {
      errorMessage = error.message;
    }

    const enhancedError = new Error(errorMessage);
    enhancedError.status = error.response?.status;
    enhancedError.data = problemDetail;
    enhancedError.originalError = error;

    return Promise.reject(enhancedError);
  },
);

export default apiClient;
