# Change Log

## 2026-07-13

* replaced all SMTP/MailKit mail sending with Azure Communication Services
  ( new AzureMailHelper )
* the mail connection string is now taken from the
  COMMUNICATION_SERVICES_CONNECTION_STRING environment variable so that the
  secret can be kept out of appsettings.json. The API:Mail:Azure:ConnectionString
  setting is still honoured as a deprecated fallback, but the environment
  variable takes precedence.
* dropped the direct MailKit dependency

## 2026-01-02

* removed old .nuspec model
* migrated to .csproj
* updated nuget packages
* fixed a bug in profileController->mfa handling
* updated build script tp push to satori and nuget

## 2025/09/19—Release 1.1.1

* Rearranged Project Layout
* Added UnitTest Stub
* Updated Nuget Package
* Created GitHub Release
* Altered Utility/Encrypt to support new Encryption in Druware.Extensions

## 2024-Dec-10 - Release 1.0.8

* Migrated everything to .net8
* Removed legacy non-'API' prefixed routing
* Added limited documentation

## 2024-Jun-17

* Rebuilt and published to nuget.