import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { OverviewPage } from './OverviewPage';
describe('Workspace connection', () => {
  it('shows loading while the health request is pending', () => {
    vi.stubGlobal('fetch', vi.fn(() => new Promise(() => {})));
    render(<OverviewPage/>);
    expect(screen.getByText('Checking API…')).toBeInTheDocument();
  });
  it('shows a successful connection from the API response', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('Healthy')));
    render(<OverviewPage/>);
    expect(await screen.findByText('API connected')).toBeInTheDocument();
  });
  it('allows retry after a connection failure', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValueOnce(new Error('offline')).mockResolvedValueOnce(new Response('Healthy')));
    render(<OverviewPage/>);
    expect(await screen.findByText('API unavailable')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Retry' }));
    expect(await screen.findByText('API connected')).toBeInTheDocument();
  });
});
