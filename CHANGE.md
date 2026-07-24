# Change Log

## 2026-07-24 - dependency advisories

* Removed the unused `Microsoft.AspNetCore.Server.Kestrel` reference, source of
  the Critical `Kestrel.Core` advisory. Nothing here referenced it - a web host
  supplies its own server.
* Bumped `Microsoft.AspNetCore.Identity` and `Microsoft.AspNetCore.Mvc.Core`
  from 2.2.x to 2.3.x.
* The MailKit, MimeKit, Pkcs, SQLitePCLRaw and Cryptography.Xml advisories are
  cleared by Druware.Server 1.1.16, which this package floats to.

`dotnet list package --vulnerable --include-transitive` now reports nothing.

`Microsoft.AspNetCore.Server.IISIntegration` is deliberately left in place: no
advisory, and not in the Kestrel dependency chain.

## 2026-07-24

* **Breaking:** `RegistrationController` and `UserController` no longer take an
  `IMapper` constructor parameter. Applications resolving them through DI need
  no change; anything constructing them directly must drop the argument.
* Replaced `_mapper.Map<User>(model)` with `UserRegistrationModel.ToUser()`
  from Druware.Server, and removed the `AutoMapper` package reference. This
  clears CVE-2026-32933 (uncontrolled recursion / DoS), which the package
  carried into every consumer.
* Side effect worth noting: these controllers previously relied on the hosting
  application registering a `CreateMap<UserRegistrationModel, User>()` profile,
  an undocumented requirement that silently produced a user with no `UserName`
  if it was missing. The mapping is now self-contained.

## 2026-07-20 - Release 1.1.20

* routed registration, administrator-created account, password reset, and MFA
  email through `Druware.Server.Email.IEmailSender`
* added `IRegistrationConfirmationEmailFactory` so applications can provide
  branded registration messages without sending a second email
* added the original plain-text confirmation message as the default factory
* converted `AzureMailHelper` to an obsolete compatibility adapter over the
  shared email sender
* moved the Azure SDK dependency into Druware.Server and updated the dependency
  to Druware.Server 1.1.12

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
