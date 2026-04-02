import { AuditraksApiClient } from './api-client.js';

export function createMockApiClient(): AuditraksApiClient & {
  _lastCall: { method: string; path: string; body?: unknown } | null;
  _mockResponse: unknown;
  _setMockResponse: (response: unknown) => void;
} {
  let lastCall: { method: string; path: string; body?: unknown } | null = null;
  let mockResponse: unknown = {};

  const mock = {
    _lastCall: null as { method: string; path: string; body?: unknown } | null,
    _mockResponse: {} as unknown,
    _setMockResponse(response: unknown) {
      mockResponse = response;
      mock._mockResponse = response;
    },
    async get<T>(path: string): Promise<T> {
      lastCall = { method: 'GET', path };
      mock._lastCall = lastCall;
      return mockResponse as T;
    },
    async post<T>(path: string, body?: unknown): Promise<T> {
      lastCall = { method: 'POST', path, body };
      mock._lastCall = lastCall;
      return mockResponse as T;
    },
    async patch<T>(path: string, body?: unknown): Promise<T> {
      lastCall = { method: 'PATCH', path, body };
      mock._lastCall = lastCall;
      return mockResponse as T;
    },
    async delete<T>(path: string): Promise<T> {
      lastCall = { method: 'DELETE', path };
      mock._lastCall = lastCall;
      return mockResponse as T;
    },
    async login() {},
    async request<T>(): Promise<T> { return mockResponse as T; },
  } as unknown as AuditraksApiClient & {
    _lastCall: { method: string; path: string; body?: unknown } | null;
    _mockResponse: unknown;
    _setMockResponse: (response: unknown) => void;
  };

  return mock;
}
