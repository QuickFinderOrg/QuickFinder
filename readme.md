# Group Finder: QuickFinder

production configuration is stored in `appsettings.Production.json` and is copied to the server.

## Disable scheduler

The scheduler can be disabled for debugging by setting the configration variable "DisableScheduler" to "true". To avoid accidentaly commiting this option while pushing new code, it is recommended you use the dotnet secrets manager to set it locally:

```bash
dotnet user-secrets set "DisableScheduler" "true"
```
