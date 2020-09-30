# Rebus.Outbox
Provides a transactional outbox abstraction to use with [Rebus](https://github.com/rebus-org/Rebus).

![Build](https://github.com/rsivanov/Rebus.Outbox/workflows/Build%20&%20test%20&%20publish%20Nuget/badge.svg?branch=master)
[![NuGet](https://img.shields.io/nuget/dt/Rebus.Outbox)](https://www.nuget.org/packages/Rebus.Outbox) 
[![NuGet](https://img.shields.io/nuget/v/Rebus.Outbox)](https://www.nuget.org/packages/Rebus.Outbox)

Why?
===
[Rebus](https://github.com/rebus-org/Rebus) doesn't currently include an abstraction to implement [Transactional outbox](https://github.com/canton7/RestEase#query-parameters).

How to use
===
This is just an abstraction that should be implemented to use a specific storage. For example a possible [SQL Server outbox](https://github.com/rsivanov/Rebus.SqlServer.Outbox) configuration would be something like:

```csharp
Configure.With(...)
	.(...)
	.Outbox(o => o.UseSqlServer(...))
	.Start();
```