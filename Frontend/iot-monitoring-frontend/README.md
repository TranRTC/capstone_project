# IoT Dashboard — Frontend

React TypeScript frontend for the IoT Device Real-Time Monitoring System capstone project.

## Prerequisites

- Node.js 18+ and npm
- Backend API running on http://localhost:5000

## Installation

```bash
npm install --legacy-peer-deps
```

## Development

```bash
npm start
```

Runs the app in development mode at http://localhost:3000

API and SignalR URLs default to `http://localhost:5000` (see `src/config/runtimeConfig.ts`). Override with:

```
REACT_APP_API_BASE_URL=http://localhost:5000/api/v1
REACT_APP_SIGNALR_HUB_URL=http://localhost:5000/monitoringhub
```

## Build

```bash
npm run build
```

Builds the app for production to the `build` folder.

## Project Structure

```
src/
├── components/     # Reusable components
├── pages/         # Page components
├── services/      # API and SignalR services
├── config/        # Runtime API/SignalR URL config
├── types/         # TypeScript types
├── utils/         # Utility functions
└── App.tsx        # Main app component
```

See the root [README.md](../../README.md) for full project setup and documentation.
