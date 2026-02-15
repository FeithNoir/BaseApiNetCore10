# BaseApiNet10

This project is a base API for role-based authentication, built with .NET 10. It serves as a foundational template for creating other APIs, providing a robust and secure starting point for authentication and authorization functionalities.

## Features

*   **Role-Based Authentication:** Secure your API endpoints based on user roles.
*   **JWT (JSON Web Token) Support:** Implement token-based authentication for stateless API security.
*   **Email Services:** Integrated email functionality for features like password reset and email confirmation.
*   **User Management:** Basic user registration and management.
*   **Clean Architecture:** Structured for maintainability and scalability.

## Getting Started

### Prerequisites

*   .NET 10 SDK
*   A database (SQLite is used by default in this template)

### Configuration

Sensitive information like JWT secrets and SMTP settings are managed using .NET User Secrets for development.

1.  Navigate to the `BaseApiNet10.Api` directory.
2.  Set your user secrets using the `dotnet user-secrets` command:

    ```bash
    dotnet user-secrets set "JwtConfig:Secret" "YOUR_JWT_SECRET_KEY"
    dotnet user-secrets set "SmtpSettings:Server" "YOUR_SMTP_SERVER"
    dotnet user-secrets set "SmtpSettings:Port" "YOUR_SMTP_PORT"
    dotnet user-secrets set "SmtpSettings:SenderName" "YOUR_SENDER_NAME"
    dotnet user-secrets set "SmtpSettings:SenderEmail" "YOUR_SENDER_EMAIL"
    dotnet user-secrets set "SmtpSettings:UserName" "YOUR_SMTP_USERNAME"
    dotnet user-secrets set "SmtpSettings:Password" "YOUR_SMTP_PASSWORD"
    ```

    **Note:** Replace the placeholder values with your actual secret keys and credentials.

### Running the Application

1.  Navigate to the `BaseApiNet10.Api` directory.
2.  Run the application:

    ```bash
    dotnet run
    ```

    The API will be accessible at the configured address (e.g., `https://localhost:5001`).

### Database Migrations

To apply database migrations:

1.  Navigate to the `BaseApiNet10.Api` directory.
2.  Run the following commands:

    ```bash
    dotnet ef database update --project ../BaseApiNet10.Data/BaseApiNet10.Data.csproj
    ```

## Project Structure

*   **BaseApiNet10.Api:** The main API project, containing controllers and startup configuration.
*   **BaseApiNet10.Business:** Business logic, services, and interfaces.
*   **BaseApiNet10.Core:** Core entities, DTOs, and common extensions.
*   **BaseApiNet10.Data:** Data access layer, including DbContext and migrations.

## Technologies Used

*   .NET 10
*   ASP.NET Core
*   Entity Framework Core
*   JWT Authentication
*   Swagger/OpenAPI
*   MailKit (for email services)
