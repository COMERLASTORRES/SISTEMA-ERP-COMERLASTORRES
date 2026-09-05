/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  safelist: [
    // Gradient colors
    { pattern: /from-(gray|blue)-[0-9]{3}/ },
    { pattern: /via-(gray|blue)-[0-9]{3}/ },
    { pattern: /to-(gray|blue)-[0-9]{3}/ },
    { pattern: /bg-gradient-to-[br]/ },
    // Opacity custom values
    'opacity-[0.15]',
    // Border colors with opacity
    'border-white/5',
    'border-white/3',
    'border-blue-200/20',
    'border-white/10',
    // Custom animation
    'animate-spin-slow',
    // Background utilities
    'bg-white/20',
    'bg-white/80',
    'bg-blue-200/20',
    'bg-blue-500/20',
    // Shadow utilities
    'shadow-2xl',
    'shadow-blue-500/20',
    // Other utilities used
    'backdrop-blur-md',
    'bg-white/95',
    'backdrop-blur-sm',
    'border-gray-200/80',
    'hover:border-blue-300/60',
    'text-blue-600',
    'text-blue-700',
    'hover:text-blue-700',
    'group-hover:scale-110',
    'group-hover:scale-105',
  ],
  theme: {
    extend: {
      colors: {
        sidebar: '#1e293b',
        'sidebar-hover': '#334155',
      },
      keyframes: {
        'spin-slow': {
          from: { transform: 'rotate(0deg)' },
          to: { transform: 'rotate(360deg)' },
        },
      },
      animation: {
        'spin-slow': 'spin-slow 20s linear infinite',
      },
    },
  },
  plugins: [],
};