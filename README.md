# TaskFlow

A modern, full-stack task management system for organizing projects, tasks, and team collaboration with real-time updates.

## Overview

TaskFlow is a comprehensive task management application built with a clean architecture approach, featuring a .NET backend API and a Next.js frontend. The system supports project organization, task tracking, comments, and real-time collaboration through SignalR.

## Architecture

The project follows a layered architecture pattern:

### Backend (.NET 10)
- **TaskFlow.API** - RESTful API with JWT authentication, Swagger documentation, and SignalR hubs
- **TaskFlow.Application** - Business logic and application services
- **TaskFlow.Core** - Domain models and interfaces
- **TaskFlow.Infrastructure** - Data access, external services, and infrastructure concerns

### Frontend (Next.js 16)
- **taskflow-ui** - Modern React-based UI with TypeScript, TailwindCSS, and real-time updates

## Tech Stack

### Backend
- **.NET 10** - Latest .NET framework
- **Entity Framework Core 9** - ORM for PostgreSQL
- **PostgreSQL 16** - Primary relational database
- **MongoDB 7** - NoSQL database for flexible data storage
- **Redis 7** - Caching and session management
- **SignalR** - Real-time communication
- **JWT Authentication** - Secure authentication
- **Serilog** - Structured logging
- **Swagger/OpenAPI** - API documentation
- **AspNetCoreRateLimit** - API rate limiting

### Frontend
- **Next.js 16** - React framework with App Router
- **React 19** - Latest React version
- **TypeScript** - Type-safe development
- **TailwindCSS** - Utility-first CSS framework
- **React Query (TanStack Query)** - Server state management
- **Zustand** - Client state management
- **React Hook Form** - Form management
- **Zod** - Schema validation
- **Axios** - HTTP client
- **@microsoft/signalr** - Real-time communication client
- **Headless UI** - Accessible UI components
- **Heroicons** - Icon library
- **date-fns** - Date utility library

## Prerequisites

Before you begin, ensure you have the following installed:

- **Node.js** 20+ and npm
- **.NET 10 SDK**
- **Docker** and **Docker Compose** (for running databases)
- **Git**

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd TaskFlow
```

### 2. Start Infrastructure Services

Start PostgreSQL, MongoDB, and Redis using Docker Compose:

```bash
docker-compose up -d
```

This will start:
- PostgreSQL on `localhost:5432`
- MongoDB on `localhost:27017`
- Redis on `localhost:6379`

Default credentials:
- PostgreSQL: `taskflow` / `dev_password`
- MongoDB: `taskflow` / `dev_password`

### 3. Backend Setup

#### Configure Connection Strings

Create or update `appsettings.Development.json` in `src/TaskFlow.API/`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=taskflow;Username=taskflow;Password=dev_password",
    "MongoDb": "mongodb://taskflow:dev_password@localhost:27017",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Key": "your-secret-key-min-32-characters-long",
    "Issuer": "TaskFlow",
    "Audience": "TaskFlow",
    "ExpiryInMinutes": 60
  }
}
```

#### Restore Dependencies

```bash
cd src/TaskFlow.API
dotnet restore
```

#### Apply Database Migrations

```bash
dotnet ef database update
```

If migrations don't exist, create them:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

#### Run the API

```bash
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:5001/swagger`

### 4. Frontend Setup

#### Install Dependencies

```bash
cd taskflow-ui
npm install
```

#### Configure Environment Variables

Create `.env.local` in the `taskflow-ui` directory:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000
NEXT_PUBLIC_SIGNALR_HUB_URL=http://localhost:5000/hubs
```

#### Run the Development Server

```bash
npm run dev
```

The frontend will be available at `http://localhost:3000`

## Development

### Backend Development

#### Running Tests

```bash
# Unit tests
dotnet test tests/TaskFlow.UnitTests

# Integration tests
dotnet test tests/TaskFlow.IntegrationTests

# All tests
dotnet test
```

#### Project Structure

```
src/
├── TaskFlow.API/              # Web API layer
│   ├── Controllers/           # API controllers
│   ├── Hubs/                  # SignalR hubs
│   └── Middleware/            # Custom middleware
├── TaskFlow.Application/      # Application logic
│   ├── Services/              # Business services
│   └── DTOs/                  # Data transfer objects
├── TaskFlow.Core/             # Domain layer
│   ├── Entities/              # Domain entities
│   └── Interfaces/            # Repository interfaces
└── TaskFlow.Infrastructure/   # Infrastructure layer
    ├── Data/                  # EF Core DbContext
    ├── Repositories/          # Data access
    └── Services/              # External services
```

### Frontend Development

#### Development Scripts

```bash
npm run dev        # Start development server with Turbopack
npm run build      # Build for production
npm run start      # Start production server
npm run lint       # Run ESLint
```

#### Project Structure

```
taskflow-ui/
├── src/
│   ├── app/                   # Next.js App Router
│   │   ├── dashboard/         # Dashboard pages
│   │   ├── login/             # Authentication pages
│   │   └── register/
│   ├── components/            # React components
│   │   ├── auth/              # Authentication components
│   │   ├── comments/          # Comment components
│   │   ├── common/            # Reusable components
│   │   ├── layout/            # Layout components
│   │   ├── projects/          # Project components
│   │   ├── providers/         # Context providers
│   │   └── tasks/             # Task components
│   └── lib/                   # Utilities and hooks
│       └── hooks/             # Custom React hooks
```

## Features

- **User Authentication** - JWT-based secure authentication
- **Project Management** - Create and organize projects
- **Task Management** - Full CRUD operations for tasks
- **Real-time Updates** - Live updates using SignalR
- **Comments** - Collaborative commenting on tasks
- **Responsive Design** - Mobile-friendly interface
- **Type Safety** - End-to-end TypeScript support
- **Form Validation** - Client and server-side validation
- **Rate Limiting** - API protection against abuse
- **Structured Logging** - Comprehensive logging with Serilog

## API Documentation

Once the backend is running, access the Swagger UI at:
- `https://localhost:5001/swagger` (Development)

## Database Migrations

### Create a New Migration

```bash
cd src/TaskFlow.API
dotnet ef migrations add <MigrationName>
```

### Apply Migrations

```bash
dotnet ef database update
```

### Rollback Migration

```bash
dotnet ef database update <PreviousMigrationName>
```

### Remove Last Migration

```bash
dotnet ef migrations remove
```

## Building for Production

### Backend

```bash
cd src/TaskFlow.API
dotnet publish -c Release -o ./publish
```

### Frontend

```bash
cd taskflow-ui
npm run build
npm run start
```

## Environment Variables

### Backend (appsettings.json)

- `ConnectionStrings:DefaultConnection` - PostgreSQL connection string
- `ConnectionStrings:MongoDb` - MongoDB connection string
- `ConnectionStrings:Redis` - Redis connection string
- `Jwt:Key` - JWT signing key
- `Jwt:Issuer` - JWT issuer
- `Jwt:Audience` - JWT audience
- `Jwt:ExpiryInMinutes` - Token expiry duration

### Frontend (.env.local)

- `NEXT_PUBLIC_API_URL` - Backend API URL
- `NEXT_PUBLIC_SIGNALR_HUB_URL` - SignalR hub URL

## Troubleshooting

### Database Connection Issues

Ensure Docker containers are running:
```bash
docker-compose ps
```

Restart containers if needed:
```bash
docker-compose restart
```

### Port Conflicts

If default ports are in use, update `docker-compose.yml` and configuration files accordingly.

### EF Core Tools Issues

Install or update EF Core tools:
```bash
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License.

## Support

For issues and questions, please create an issue in the GitHub repository.
