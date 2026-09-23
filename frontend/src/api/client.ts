let token: string | null = null;
export const sessionExpiredEvent = 'fleet:session-expired';
export function setAccessToken(value: string | null) { token = value; }
export class ApiError extends Error {
  constructor(public readonly status: number, message: string) { super(message); }
}
export async function request<T>(
  path: string, options: RequestInit & { anonymous?: boolean } = {},
): Promise<T> {
  const { anonymous = false, ...init } = options;
  const headers = new Headers(init.headers);
  headers.set('Accept', 'application/json');
  if (init.body) headers.set('Content-Type', 'application/json');
  const sentToken = anonymous ? null : token;
  if (sentToken) headers.set('Authorization', 'Bearer ' + sentToken);
  const response = await fetch((import.meta.env.VITE_API_BASE_URL ?? '') + '/api' + path,
    { ...init, headers, credentials: 'omit' });
  if (response.status === 401 && !anonymous && sentToken === token)
    window.dispatchEvent(new Event(sessionExpiredEvent));
  if (!response.ok) {
    const problem = await response.json().catch(() => ({})) as { title?: string; errors?: Record<string, string[]> };
    const message = problem.errors ? Object.values(problem.errors).flat().join(' ') : problem.title;
    throw new ApiError(response.status, message || 'The request failed. Please try again.');
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}
