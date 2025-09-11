# WNAB Development Guide

## Getting Started

This document provides instructions for setting up and running the WNAB MVP locally.

## Prerequisites

- .NET 9.0 SDK
- SQL Server LocalDB (usually installed with Visual Studio)
- Your favorite IDE (Visual Studio, VS Code, Rider, etc.)

## Project Structure

```
/src
  /WNAB.Core            # Shared business logic and models
  /WNAB.Data            # Entity Framework context and data models
  /WNAB.API             # ASP.NET Core Web API backend
  /WNAB.Web             # Blazor web application
  /WNAB.Plaid           # Plaid integration (placeholder for future)
  /WNAB.AppHost         # .NET Aspire orchestration host
  /WNAB.ServiceDefaults # Shared Aspire service configuration
```

## Running the Application

### Option 1: Using .NET Aspire (Recommended)

The easiest way to run the application is using .NET Aspire orchestration:

```bash
cd src/WNAB.AppHost
dotnet run
```

This will:
- Start the Aspire dashboard at `http://localhost:15888`
- Automatically start SQL Server in a container
- Start the API with service discovery enabled
- Start the Web application with automatic API discovery
- Provide monitoring, logging, and health checks

Access the application at the URL shown in the Aspire dashboard.

### Option 2: Manual Startup (For Development)

#### 1. Start the API Server

```bash
cd src/WNAB.API
dotnet run
```

The API will be available at `https://localhost:7299`

#### 2. Start the Web Application

Open a new terminal window:

```bash
cd src/WNAB.Web  
dotnet run
```

The web application will be available at `https://localhost:5001`

#### 3. Database Setup

The database will be automatically created when you first run the API. When using Aspire, SQL Server runs in a container. For manual startup, it uses SQL Server LocalDB.

## Features Implemented in MVP

### ✅ Core Features
- [x] User registration and authentication (JWT-based)
- [x] Account management (create, read, update, delete)
- [x] Transaction management (CRUD operations with automatic balance updates)
- [x] Category system for organizing transactions
- [x] RESTful API endpoints for all functionality
- [x] Basic Blazor web interface

### ✅ Technical Features
- [x] .NET 9 with Entity Framework Core
- [x] JWT authentication and authorization
- [x] SQL Server database with proper relationships
- [x] Clean architecture with separate projects
- [x] Blazor Server with Bootstrap UI
- [x] .NET Aspire orchestration with service discovery
- [x] Containerized SQL Server via Aspire
- [x] Integrated monitoring and health checks
- [x] Centralized logging and telemetry

## API Endpoints

### Authentication
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login

### Accounts
- `GET /api/accounts` - List user accounts
- `GET /api/accounts/{id}` - Get account details
- `POST /api/accounts` - Create new account
- `PUT /api/accounts/{id}` - Update account
- `DELETE /api/accounts/{id}` - Delete account

### Categories
- `GET /api/categories` - List user categories
- `GET /api/categories/{id}` - Get category details
- `POST /api/categories` - Create new category
- `PUT /api/categories/{id}` - Update category
- `DELETE /api/categories/{id}` - Delete category

### Transactions
- `GET /api/transactions` - List transactions (with filtering)
- `GET /api/transactions/{id}` - Get transaction details
- `POST /api/transactions` - Create new transaction
- `PUT /api/transactions/{id}` - Update transaction
- `DELETE /api/transactions/{id}` - Delete transaction

## Testing the API

You can test the API using tools like:
- Postman
- curl
- The Swagger UI at `https://localhost:7299/swagger` (in development)

## Next Steps for Full Application

1. **Plaid Integration** - Connect real bank accounts
2. **Advanced UI** - Rich client-side interactions
3. **Reporting** - Charts and financial insights
4. **Mobile App** - MAUI implementation
5. **Cloud Deployment** - Production hosting
6. **Additional Features** - Bill reminders, budgeting tools, etc.

## Troubleshooting

### Database Issues
If you encounter database connection issues:
1. Ensure SQL Server LocalDB is installed
2. Check the connection string in `appsettings.json`
3. Delete the database and let it recreate automatically

### Port Conflicts
If the default ports are in use:
1. Update `launchSettings.json` in both projects
2. Update the API URL in the web app's `appsettings.json`

### Build Issues
If the solution doesn't build:
1. Run `dotnet restore` in the root directory
2. Ensure you have .NET 9.0 SDK installed
3. Check for any missing package references