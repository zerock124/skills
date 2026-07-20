import { startLoading, stopLoading } from '@/composables/useLoading';
import { clearAuthStorage, getToken } from '@/util/localstorage';
import axios, { type AxiosError, type AxiosRequestConfig } from 'axios';

let unauthorizedHandler: (() => void) | null = null;

/**
 * 由 main.ts 注入全域「登入過期」處理流程
 */
export function setUnauthorizedHandler(handler: () => void): void {
  unauthorizedHandler = handler;
}

/**
 * API 錯誤。訊息取自後端 BaseResponse 的 Message 欄位
 */
export class ApiError extends Error {
  public readonly data: unknown;
  public readonly status: number;

  constructor(data: unknown, status: number, statusText: string) {
    super(
      data && typeof data === 'object' && 'Message' in data
        ? String((data as { Message: unknown }).Message)
        : statusText,
    );
    this.data = data;
    this.status = status;
  }
}

export function getApiErrorMessage(error: unknown): string | undefined {
  return error instanceof ApiError ? error.message : undefined;
}

// 統一在此讀取 API 基礎位址環境變數，並建立共用的 axios 實例
const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '',
});

axiosInstance.interceptors.request.use((config) => {
  startLoading();
  const token = getToken();
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`);
  }
  return config;
});

axiosInstance.interceptors.response.use(
  (response) => {
    stopLoading();
    return response;
  },
  (error: AxiosError) => {
    stopLoading();
    if (error.response?.status === 401) {
      clearAuthStorage();
      unauthorizedHandler?.();
      // 交由全域「登入過期」流程處理，回傳永不 settle 的 promise，
      // 避免各呼叫端的 catch 再彈出額外的錯誤通知
      return new Promise<never>(() => {});
    }
    return Promise.reject(
      new ApiError(
        error.response?.data,
        error.response?.status ?? 0,
        error.response?.statusText ?? error.message,
      ),
    );
  },
);

/**
 * orval 產生的所有 API 函式共用的 mutator
 */
export function orvalFetch<T>(config: AxiosRequestConfig, options?: AxiosRequestConfig): Promise<T> {
  return axiosInstance({ ...config, ...options }).then((response) => response.data as T);
}
