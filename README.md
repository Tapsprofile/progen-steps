# ProgenSteps

A Windows-based web application built with ASP.NET Core and Vue 3, demonstrating modern full-stack development.

## Technology Stack

### Backend
- **ASP.NET Core 10** - Web API framework
- **C#** - Primary programming language
- **OpenAPI** - API documentation

### Frontend
- **Vue 3** - Progressive JavaScript framework
- **TypeScript** - Type-safe JavaScript
- **Vue Router** - Client-side routing
- **Pinia** - State management
- **Vite** - Build tool and dev server

## Project Structure

```
progen-steps/
├── src/
│   ├── ProgenSteps.API/       # ASP.NET Core Web API
│   │   ├── Program.cs          # Application entry point
│   │   ├── Properties/         # Launch settings
│   │   └── appsettings.json    # Configuration
│   └── ProgenSteps.UI/         # Vue 3 frontend
│       ├── src/
│       │   ├── assets/         # Static assets
│       │   ├── components/     # Vue components
│       │   ├── router/         # Routing configuration
│       │   ├── stores/         # Pinia stores
│       │   ├── views/          # Page components
│       │   ├── App.vue         # Root component
│       │   └── main.ts         # Application entry
│       ├── index.html          # HTML template
│       ├── vite.config.ts      # Vite configuration
│       └── package.json        # Node dependencies
└── ProgenSteps.sln             # Solution file

```

## Prerequisites

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Node.js 18+** - [Download](https://nodejs.org/)
- **npm** or **yarn** - Package manager (included with Node.js)

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/Tapsprofile/progen-steps.git
cd progen-steps
```

### 2. Setup Backend

```bash
# Restore dependencies
dotnet restore

# Run the API
cd src/ProgenSteps.API
dotnet run
```

The API will start at `http://localhost:5000`

### 3. Setup Frontend

```bash
# Navigate to UI directory
cd src/ProgenSteps.UI

# Install dependencies
npm install

# Start development server
npm run dev
```

The Vue app will start at `http://localhost:5173`

## Development

### Backend Development

The ASP.NET API includes:
- CORS configuration for Vue frontend
- Sample Weather Forecast endpoint at `/api/weatherforecast`
- OpenAPI documentation in development mode

```bash
# Build the solution
dotnet build

# Run tests (when added)
dotnet test
```

### Frontend Development

The Vue 3 app includes:
- TypeScript support for type safety
- Hot Module Replacement (HMR)
- Vue Router for navigation
- Pinia for state management
- Proxy configuration to connect to the API

```bash
# Type check
npm run type-check

# Build for production
npm run build

# Preview production build
npm run preview
```

## API Endpoints

- `GET /api/weatherforecast` - Returns sample weather forecast data

## Configuration

### Backend Configuration

Edit `src/ProgenSteps.API/appsettings.json` for application settings.

CORS is configured in `Program.cs` to allow requests from `http://localhost:5173`.

### Frontend Configuration

Edit `src/ProgenSteps.UI/vite.config.ts`:
- Change API proxy settings
- Modify dev server port
- Configure build options

## Building for Production

### Backend

```bash
dotnet publish -c Release -o ./publish
```

### Frontend

```bash
cd src/ProgenSteps.UI
npm run build
```

The built files will be in `src/ProgenSteps.UI/dist/`

## Deployment

For Windows deployment:
1. Publish the ASP.NET API to IIS or as a Windows Service
2. Build the Vue frontend and serve from the API's wwwroot folder or a separate web server
3. Update CORS settings for production URLs

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License.
