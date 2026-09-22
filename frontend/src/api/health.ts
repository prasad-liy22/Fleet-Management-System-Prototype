export async function checkHealth(signal?: AbortSignal): Promise<void> {
  const response = await fetch(`${import.meta.env.VITE_API_BASE_URL ?? ''}/health`, { signal });
  if (!response.ok || (await response.text()).trim() !== 'Healthy') throw new Error('API unavailable');
}
