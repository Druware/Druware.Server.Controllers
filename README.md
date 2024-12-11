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
- MailKit
- Microsoft.AspNetCore.Identity
- Microsoft.AspNetCore.Identity.EntityFramework
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Configuration.Binder

## History

See Changelog.md for detailed history

## License

This project is under the GPLv3 license, see the included LICENSE.txt file

```
Copyright 2019-2024 by:
    Andy 'Dru' Satori @ Satori & Associates, Inc.
    All Rights Reserved
```