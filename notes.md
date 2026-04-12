# ReachLog — Dev Notes

## Concepts

**Solution (.sln)** — a container that groups multiple projects together. Doesn't contain code itself, just tells .NET which projects belong together.

**Class Library** — a C# project with no entry point, meaning it can't run on its own. Designed to be referenced by other projects.

**Web API project** — an ASP.NET Core project that runs a web server and handles HTTP requests. This is the entry point of the backend.

**Clean Architecture** — splitting the backend into 4 layers so each layer only knows about the layers below it. Makes the code easier to maintain and test.
- Domain — core data models, no dependencies
- Application — business logic, depends on Domain
- Infrastructure — database + external services, depends on Application + Domain
- API — entry point, depends on Application + Infrastructure

**Entity** — a C# class that represents a real-world object in the system. Maps directly to a database table.

**Enum** — a fixed set of named values. Used when a field can only be one of a known list of options (e.g. outreach status: Sent, Opened, Replied).

**NuGet** — the package manager for .NET. Same idea as npm for Node.js.

**EF Core (Entity Framework Core)** — an ORM (Object Relational Mapper). Lets you interact with the database using C# code instead of writing raw SQL.

**Migration** — a file that describes a change to the database schema. EF Core generates these automatically from your entities.

**JWT (JSON Web Token)** — a token issued after login that proves who you are. Sent with every API request so the server knows which user is making the request.

**Serilog** — a structured logging library. Instead of plain text logs, it writes logs as structured data you can search and filter.

**FluentValidation** — a library for validating incoming data (e.g. making sure an email field is actually an email before it hits your database).

## Git Workflow

**main** — protected branch, never push directly to it
**feature/name** — new functionality
**fix/name** — bug fixes
**chore/name** — config, tooling, non-code changes
**docs/name** — documentation only

Every piece of work = new branch → commit → push → Pull Request → merge into main.

**Conventional Commits**
- feat: new feature
- fix: bug fix
- chore: tooling/config
- docs: documentation


**SSR (Server-Side Rendering)** — rendering the Angular app on the server instead of 
the browser. Useful for SEO and initial load performance. We don't need it for ReachLog 
since it's a private dashboard, not a public website.

**SCSS** — a superset of CSS that adds features like variables, nesting, and mixins. 
Compiles down to regular CSS. We use it instead of plain CSS for cleaner styling.

**Standalone Components** — Angular 17+ feature where components don't need to be 
declared in a NgModule. Simpler, more modern way to build Angular apps.

**Routing** — Angular's system for navigating between pages without a full page reload. 
Each URL maps to a component (e.g. /login → LoginComponent, /dashboard → DashboardComponent).

**HttpClient** — Angular's built-in service for making HTTP requests to your API. 
Replaces the native fetch API with observables and interceptors.

**Observable** — a stream of data over time. Used heavily in Angular instead of Promises. 
Think of it like a Promise that can emit multiple values and can be cancelled.

**Interceptor** — middleware for Angular's HttpClient. Every HTTP request passes through it. 
We'll use one to automatically attach the JWT token to every request so you don't have 
to add it manually each time.

**Service** — an Angular class that holds business logic and data, shared across components. 
Components should be thin — they display data. Services do the work.

**Component** — the building block of an Angular app. Each component = one piece of UI. 
It has three parts: a TypeScript class (logic), an HTML template (structure), 
and SCSS styles (appearance).

**Guard** — protects routes from unauthorized access. If a user isn't logged in and tries 
to access /dashboard, the guard redirects them to /login automatically.