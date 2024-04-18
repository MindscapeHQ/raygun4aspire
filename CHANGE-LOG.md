# Change Log for Raygun4Aspire

### v1.0.0
- Initial release - up to par with the Raygun4Net.AspNetCore package.
- When running in the local development environment, Raygun crash reports are sent to a locally running Raygun app that's fetched from [Docker Hub](https://hub.docker.com/r/raygunowner/raygun-aspire-portal).
- Optionally providing a Raygun application API Key allows crash reports to be sent to your Raygun cloud service account when your Aspire app is running in production.