import { loadEnv } from 'vite';
import { defineConfig, mergeConfig } from 'vitest/config';
import config from './vite.config';
export default mergeConfig(config, defineConfig({
  test: { provide: { livePassword: loadEnv('test', '.', '').FMS_LIVE_TEST_PASSWORD ?? '' }, include: ['src/**/*.live.tsx'], testTimeout: 60000, maxWorkers: 1 },
}));
