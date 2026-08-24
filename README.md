# Real Estate 🏠

A real estate management platform built with a layered **Clean Architecture** backend (.NET) and an **Angular** frontend. The system includes secure user authentication, property and lead management, an ML.NET-based price estimator, and a smart chat assistant for finding an apartment from the existing property database.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Key Features](#key-features)
- [The Apartment-Finder Chat](#the-apartment-finder-chat)
- [Project Structure](#project-structure)
- [Setup & Running](#setup--running)
- [Environment Variables](#environment-variables)
- [Contributing](#contributing)

## Overview

The project provides an end-to-end solution for real estate management: secure sign-up/sign-in for users (buyers, renters, and agents), listing and managing properties, tracking leads, ML-based price estimation, and a natural-language chat that helps users find a matching apartment from the existing listings.

## Architecture

The backend follows a layered **Clean Architecture** approach:

- **RealEstate.Domain** – Core entities: `Client`, `Agent`, `Property`, `Lead`.
- **RealEstate.Application** – Business logic using **MediatR** (CQRS), organized into modules: `Auth`, `Chat`, `Dashboard`, `Leads`, `Ml`, `Properties`.
- **RealEstate.Infrastructure** – Repository implementations, SQL Server access via EF Core, JWT service, AI property analysis service, and an ML.NET price-estimation model.
- **RealEstate.Api** – Web API layer (Controllers), JWT authentication and role-based authorization.

The frontend (**RealEstate-Angular**) is an Angular app with matching feature modules: `auth`, `chat`, `dashboard`, `properties`.

## Tech Stack

**Backend:**
- .NET / ASP.NET Core Web API
- Entity Framework Core + SQL Server
- MediatR – CQRS pattern for commands and queries
- JWT Bearer Authentication + role-based authorization
- ML.NET – property price estimation model (`MlNetPriceEstimator`)
- External LLM integration via OpenRouter (e.g. `google/gemini-2.5-flash`) for the chat feature and property description analysis
- Swagger / OpenAPI

**Frontend:**
- Angular
- TypeScript

**Data:**
- `ml-data` – training/evaluation data (`properties.csv`) and data-generation scripts

## Key Features

- 🔐 **User Authentication** – Secure sign-up and login (`AuthController`, `LoginHandler`, `RegisterHandler`) with JWT issuance and role management: `Buyer`, `Renter`, `Agent`.
- 🏘️ **Property Management** – Create, edit, and browse listings, including an embedding computed for each property description (`OpenAiPropertyAnalyst`) at creation time.
- 💬 **Smart Apartment-Finder Chat** – A natural-language conversation that gets translated into a structured search over the listings database (details below).
- 📊 **Price Estimation** – An ML.NET model that estimates a price range based on city, number of rooms, size, floor, property age, parking, and elevator.
- 📋 **Lead Management** – Clients can reach out about a property (`LeadsController`), and agents can view all incoming leads.
- 📈 **Agent Dashboard** – A management screen restricted to the `Agent` role (`[Authorize(Roles = "Agent")]`).

## The Apartment-Finder Chat

The chat (`ChatController` → `ChatHandler`) works as follows:

1. The user's free-text message (e.g. "I'm looking for an apartment in Tel Aviv up to 3 million") is sent to an external LLM (via OpenRouter) with a strict system prompt instructing it to return **structured JSON only**: `reply`, `cityHebrew`, `cityEnglish`, `maxPrice`.
2. Conversation history is cached in memory by `ConversationId` to preserve context across messages (20-minute TTL).
3. Once the AI returns the extracted parameters, the backend **filters the actual property repository** (`IAsyncRepository<Property>`) by address and price — including a city-alias mechanism that matches Hebrew/English names for major Israeli cities (Tel Aviv/תל אביב, Jerusalem/ירושלים, etc.).
4. Up to 3 matching properties are returned to the user along with a friendly text reply.
5. Error handling includes graceful fallbacks if the AI service is unavailable or returns invalid output.

> **Technical note:** This is a natural-language-to-structured-query mechanism, not classic RAG (Retrieval-Augmented Generation) — the LLM never receives the actual property data as context for generating its reply. The `Property` entity does have a `DescriptionVector` field computed at creation time (intended for future semantic search), but it is currently unused in the chat flow.

## Project Structure

```
Real-Estate/
├── RealEstate-Angular/              # Frontend – Angular
│   └── src/app/features/            # auth, chat, dashboard, properties
├── RealEstate.Api/                  # API layer
│   └── Controllers/                 # Auth, Chat, Dashboard, Leads, PriceEstimate, Properties
├── RealEstate.Application/          # Business logic (MediatR)
│   ├── Auth/                        # Login, Register
│   ├── Chat/                        # ChatHandler – apartment-finder chat
│   ├── Dashboard/
│   ├── Leads/
│   ├── Ml/                          # IPriceEstimator
│   └── Properties/
├── RealEstate.Domain/                # Entities: Client, Agent, Property, Lead
├── RealEstate.Infrastructure/        # EF Core, Repositories, JWT, ML.NET, AI Services
├── ml-data/                          # Price-estimation training data
└── RealEstate.sln
```

## Setup & Running

### Prerequisites
- .NET SDK
- Node.js & npm + Angular CLI
- SQL Server (local or cloud)
- An OpenRouter API key (for the chat feature)

### Run the Backend

```bash
git clone https://github.com/shbh8205-lgtm/Real-Estate.git
cd Real-Estate
dotnet restore
dotnet build
dotnet run --project RealEstate.Api
```

### Run the Frontend

```bash
cd RealEstate-Angular
npm install
ng serve
```

The app will be available at `http://localhost:4200`.

## Environment Variables

Set the following in `appsettings.json` (or `appsettings.Development.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;"
  },
  "Jwt": {
    "Issuer": "...",
    "Audience": "...",
    "Key": "..."
  },
  "OpenRouter": {
    "ApiKey": "...",
    "Model": "google/gemini-2.5-flash",
    "BaseUrl": "https://openrouter.ai/api/v1"
  }
}
```

## Contributing

Issues and pull requests are welcome — for example, extending the chat into genuine semantic search (RAG) by leveraging the existing `DescriptionVector` field on the `Property` entity.

---
Built ❤️ with Angular, .NET, and ML.NET
