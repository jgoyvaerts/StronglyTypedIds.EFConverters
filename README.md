# StronglyTypedIds.EFConverters

[![NuGet](https://img.shields.io/nuget/v/StronglyTypedIds.EFConverters.svg)](https://www.nuget.org/packages/StronglyTypedIds.EFConverters/)

EF Core value converters for strongly typed IDs, automatically generated at compile time.

## Description

StronglyTypedIds.EFConverters is a source generator that automatically creates Entity Framework Core value converters for your strongly typed ID types. It eliminates the need to manually write converters for each ID type, making your EF Core integration cleaner and more maintainable.

## Installation

```csharp
// Via .NET CLI
dotnet add package StronglyTypedIds.EFConverters

// Via Package Manager
Install-Package StronglyTypedIds.EFConverters
```

## Usage

### 1. Define your strongly typed ID

First, ensure your strongly typed ID has the `StronglyTypedIdAttribute` or appropriate generated code attribute:

```csharp
[StronglyTypedId(Template.Int)]
public partial struct UserId{}
```

### 2. Configure EF Core to use the generated converters

In your DbContext configuration, simply call the extension method:

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    base.ConfigureConventions(configurationBuilder);
    
    // This will register all your strongly typed ID converters
    configurationBuilder.UseStronglyTypedIdConverters();
}
```

### 3. Use your strongly typed IDs in entity models

```csharp
public class User
{
    public UserId Id { get; set; }
    public string Name { get; set; }
    // Other properties...
}
```

EF Core will now seamlessly convert between your strongly typed ID and its underlying value when reading from or writing to the database.

## How It Works

The source generator scans your project and referenced assemblies for types marked with `StronglyTypedIdAttribute` and automatically generates:

1. A value converter for each strongly typed ID type
2. An extension method for easy registration with EF Core

This happens at compile time, so there's no runtime performance penalty.

## Requirements

- .NET Standard 2.0 or later
- Entity Framework Core

## License

MIT
