import { defineConfig } from 'vitest/config';

// Avoid worker startup timeouts when API, PostgreSQL and Angular run together locally.
export default defineConfig({ test: { maxWorkers: 1, fileParallelism: false } });
