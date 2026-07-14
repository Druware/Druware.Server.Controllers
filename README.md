# Druware.Server.Controllers

This package implements the Controllers associated with the foundation 
elements within the Druware.Server library. They are however isolated out into
this library in order to allow any user or implementation to replace them with
their own custom implementations if these to not suit the need.

As of version 1.0.8, in addition to supporting PostgreSQL and Microsoft SQL 
Server, Sqlite is now also supported ( with known warnings that schemas are not 
supported during the migration ).

## Documentation

Full documentation is 'slowly' coming together in the 'docs' folder.

## Dependencies

*Requires .net8.0* 

- Druware.Extensions
- Druware.Server
- RESTfulFoundation
- Azure.Communication.Email
- Microsoft.AspNetCore.Identity
- Microsoft.AspNetCore.Identity.EntityFramework
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Configuration.Binder

## Mail Configuration

All outbound mail ( registration confirmation, password reset, and the MFA
code ) is delivered through Azure Communication Services rather than SMTP.

The connection string is a secret and is supplied through the environment, so
that it never lands in a checked in appsettings.json:

```shell
export COMMUNICATION_SERVICES_CONNECTION_STRING="endpoint=https://<resource>.communication.azure.com/;accesskey=<key>"
```

On Azure App Service this is set as an application setting of the same name.
`API__Mail__Azure__ConnectionString` also works, as the configuration provider
maps it onto the `API:Mail:Azure:ConnectionString` key.

The `API:Mail:Azure:ConnectionString` value in appsettings.json is still read
as a last resort for existing deployments, but it is deprecated and should be
removed from appsettings.json in favour of the environment variable. The
environment variable wins when both are present.

The non-secret settings remain in appsettings.json:

```json
{
  "API": {
    "Notification": {
      "From": "DoNotReply@<verified-domain>"
    }
  }
}
```

The `API:Notification:From` address must be a sender address on a domain that
is verified/connected to the Communication Services resource.

## History

See Changelog.md for detailed history

## License

This project is under the GPLv3 license, see the included LICENSE.txt file

```
Copyright 2019-2024 by:
    Andy 'Dru' Satori @ Satori & Associates, Inc.
    All Rights Reserved
```