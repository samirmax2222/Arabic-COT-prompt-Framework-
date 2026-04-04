/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        // Unreal Engine dark theme palette
        ue: {
          bg: '#1a1a1a',
          surface: '#242424',
          panel: '#2a2a2a',
          border: '#3a3a3a',
          hover: '#404040',
          text: '#e0e0e0',
          muted: '#888888',
          accent: '#0078d4',
          // Blueprint pin colors
          exec: '#ffffff',
          bool: '#9c0000',
          byte: '#006464',
          int: '#1ca864',
          int64: '#a8e6a8',
          float: '#a8e650',
          double: '#94fc13',
          string: '#f0a0f0',
          text: '#f080a8',
          name: '#b898de',
          vector: '#ffc040',
          rotator: '#99ccff',
          transform: '#f08040',
          color: '#ff8000',
          object: '#00a0f0',
          class: '#6040b0',
          interface: '#b8d0a0',
          struct: '#0088cc',
          enum: '#006464',
          delegate: '#f04040',
          wildcard: '#808080',
          // Node header colors
          'event-red': '#8c1a1a',
          'function-blue': '#1a4a8c',
          'pure-green': '#1a6a3a',
          'macro-gray': '#4a4a5a',
          'flow-gray': '#3a3a4a',
        },
      },
      boxShadow: {
        'node': '0 4px 20px rgba(0, 0, 0, 0.5)',
        'node-selected': '0 0 0 2px #0078d4, 0 4px 20px rgba(0, 120, 212, 0.3)',
        'glow': '0 0 15px rgba(0, 120, 212, 0.4)',
      },
      animation: {
        'flow': 'flowAnimation 2s linear infinite',
        'pulse-glow': 'pulseGlow 2s ease-in-out infinite',
        'fade-in': 'fadeIn 0.2s ease-out',
      },
      keyframes: {
        flowAnimation: {
          '0%': { strokeDashoffset: '24' },
          '100%': { strokeDashoffset: '0' },
        },
        pulseGlow: {
          '0%, 100%': { opacity: '0.5' },
          '50%': { opacity: '1' },
        },
        fadeIn: {
          '0%': { opacity: '0', transform: 'translateY(4px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
      },
    },
  },
  plugins: [],
};
