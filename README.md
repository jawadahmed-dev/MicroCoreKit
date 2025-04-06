# 🚀 MicroCoreKit

MicroCoreKit is a .NET 6+ utility library designed for Modular Monolithic and Microservice-based architectures. It provides reusable base classes, helpers, middleware, and services to standardize, simplify, and accelerate API development across independently hosted projects that share a common database.

⚠️ Note: This library is currently under active development. More utility classes and features will be added soon.

## 📦 About

MicroCoreKit is a highly reusable and extensible .NET library built for modular environments. Whether you're building a microservice ecosystem or a modular monolith, this package offers a foundation of generic and shared components — making it easy to reduce code duplication and promote consistency across your solutions.

You can either:
- Clone and directly reference this as a project in your solution.
- Host it privately or publicly and consume it as a NuGet package.

## 🗂️ Project Structure

| Folder/Namespace           | Description                                                                 |
|----------------------------|-----------------------------------------------------------------------------|
| Base/                      | Contains base classes for controllers, entities, validators, etc.          |
| Database/                  | Common database-related classes like Contexts, Entities, Migrations, etc.  |
| Extensions/                | Helpful extension methods for common .NET operations.                      |
| Helpers/                   | Utility classes for logging, caching, and object manipulation.             |
| Options/                   | Configuration classes for structured application settings.                 |
| Pipelines/                 | Middleware and request pipeline behavior (e.g., exception handling).       |
| Services/HttpService/      | Reusable HTTP client wrapper for making external API calls.                |
| Repositories/              | Generic repository pattern with interfaces and base implementations.       |

## 🛠 Usage

Add this as a project reference if you're following a modular architecture:

    dotnet add reference ../MicroCoreKit/MicroCoreKit.csproj

Or, if hosted as a NuGet package:

    dotnet add package MicroCoreKit --version x.y.z

## 📌 Roadmap

- [x] Base controllers and validators  
- [x] HTTP service wrapper  
- [x] Middleware for exception and validation handling  
- [ ] Logging and caching helpers (coming soon)  
- [ ] Authorization utilities  
- [ ] More extension methods and repository enhancements  

## 🤝 Contribution

Feel free to clone, customize, and contribute. PRs are welcome!
