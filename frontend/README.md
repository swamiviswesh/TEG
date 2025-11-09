# Frontend — TEG Events Calendar

Simple React + Vite frontend that renders an events calendar using the backend's enriched events API.

## Requirements
- Node.js 18+ (or compatible)
- npm (or yarn/pnpm)
- Browser with HTTPS support for local API calls to https://localhost:7128

## Quick start
1. Install dependencies
   - npm: `npm install`

2. Run dev server (Vite, default port 9000)
   - npm: `npm run dev`
   - Open: http://localhost:9000

3. Build for production
   - `npm run build`
   - Preview production build:
     - `npm run preview`

4. Lint
   - `npm run lint`

## Configuration
- API URL used by the app:
  - Default located in `frontend/src/App.tsx`:
    - `const API_URL = 'https://localhost:7128/api/events/enriched';`
  - If your backend is served on a different port or host, update that constant or refactor to use an environment variable (e.g., `import.meta.env.VITE_API_URL`) before building.

## Notes
- Dev server runs on port 9000. Ensure the backend CORS policy allows this origin (see backend README).
- The UI displays small circle indicators in the calendar grid; click a circle to open the event details modal.
