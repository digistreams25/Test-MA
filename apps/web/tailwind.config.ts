import type { Config } from 'tailwindcss';

const config: Config = {
  content: [
    './app/**/*.{js,ts,jsx,tsx,mdx}',
    './components/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          gold: '#d4a853',
          'gold-light': '#e8c978',
          'gold-dark': '#b08930',
        },
        surface: {
          '0': '#0a0a0f',
          '1': '#12121a',
          '2': '#1a1a25',
          '3': '#222230',
          '4': '#2a2a3a',
        },
        clash: {
          critical: '#ef4444',
          warning: '#f59e0b',
          resolved: '#22c55e',
          info: '#3b82f6',
        },
      },
      fontFamily: {
        mono: ['JetBrains Mono', 'Fira Code', 'monospace'],
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
};
export default config;
