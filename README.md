# BookNest

ASP.NET Core MVC (.NET 8) app for book lovers.

## Local setup

API keys are **never** stored in the repo. Configure them with User Secrets
(Visual Studio: right-click the `BookNest` project -> **Manage User Secrets**, or use the CLI):

```
cd BookNest
dotnet user-secrets set "Lyra:Groq:ApiKey" "<your Groq key>"
dotnet user-secrets set "Lyra:Tavily:ApiKey" "<your Tavily key>"
dotnet user-secrets set "Email:Brevo:ApiKey" "<your Brevo key>"
dotnet user-secrets set "Email:Brevo:SenderEmail" "<verified sender address>"
```

Keys: https://console.groq.com/keys, https://tavily.com, https://app.brevo.com/settings/keys/api

## Production

Set the same keys as environment variables / App Service settings, using `__` as the separator:
`Lyra__Groq__ApiKey`, `Lyra__Tavily__ApiKey`, `Email__Brevo__ApiKey`, `Email__Brevo__SenderEmail`.
