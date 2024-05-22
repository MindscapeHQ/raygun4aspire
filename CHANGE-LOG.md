# Change Log for Raygun4Aspire

### v1.0.1
- Fixed a bug where on some machines the crash reports will fail to be saved locally due to the invalid "|" character being used in the file name. This character has now been replaced with "-". You may want to manually rename previously persisted crash reports as such in the "raygun-data" Docker volume.
- Fixed a bug where the date times would not be displayed in the locally running Raygun app depending on your local date time formatting options.

### v1.0.0
- Initial release - up to par with the Raygun4Net.AspNetCore package.
- When running in the local development environment, Raygun crash reports are sent to a locally running Raygun app that's fetched from [Docker Hub](https://hub.docker.com/r/raygunowner/raygun-aspire-portal).
- Optionally providing a Raygun application API Key allows crash reports to be sent to your Raygun cloud service account when your Aspire app is running in production.