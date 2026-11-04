using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Persistence;

namespace Starter.Api.Tests.Architecture;

public sealed class ArchitectureTests
{
    private static readonly Assembly Production = typeof(Program).Assembly;

    private static IEnumerable<Type> ProductionTypes() =>
        Production.GetTypes().Where(type => !type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false));

    private static bool IsPersistenceType(Type type) =>
        type.Namespace is not null && type.Namespace.StartsWith("Starter.Api.Persistence", StringComparison.Ordinal);

    private static bool IsDataAccess(Type parameter) =>
        parameter == typeof(AppDbContext) ||
        parameter == typeof(IDbContextFactory<AppDbContext>) ||
        (parameter.IsGenericType && parameter.GetGenericTypeDefinition() == typeof(UserManager<>));

    private static bool IsRepository(Type type) => type.Name.EndsWith("Repository", StringComparison.Ordinal);

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Starter.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    [Fact]
    public void No_type_outside_Repositories_and_Persistence_receives_AppDbContext_or_UserManager()
    {
        var offenders = ProductionTypes()
            .Where(type => !IsRepository(type) && !IsPersistenceType(type))
            .Where(type => type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(constructor => constructor.GetParameters().Any(parameter => IsDataAccess(parameter.ParameterType))))
            .Select(type => type.FullName)
            .ToList();

        offenders.Should().BeEmpty();
    }

    [Fact]
    public void No_module_receives_repository_or_data_access()
    {
        var offenders = Production.GetTypes()
            .Where(type => type.Namespace == "Starter.Api.Modules")
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.GetParameters().Any(parameter => IsRepository(parameter.ParameterType) || IsDataAccess(parameter.ParameterType)))
                .Select(method => $"{type.FullName}.{method.Name}"))
            .ToList();

        offenders.Should().BeEmpty();
    }

    [Fact]
    public void Every_service_implementation_ends_with_Service_and_lives_in_Services()
    {
        var implementations = ProductionTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetInterfaces().Any(contract =>
                contract.Assembly == Production && contract.Name.StartsWith('I') && contract.Name.EndsWith("Service", StringComparison.Ordinal)))
            .ToList();

        implementations.Should().NotBeEmpty();
        implementations.Should().OnlyContain(type => type.Name.EndsWith("Service", StringComparison.Ordinal));

        var sourceRoot = Path.Combine(RepositoryRoot(), "src", "Starter.Api");
        foreach (var type in implementations)
        {
            var files = Directory.GetFiles(sourceRoot, $"{type.Name}.cs", SearchOption.AllDirectories);
            files.Should().ContainSingle();
            Path.GetFileName(Path.GetDirectoryName(files[0])).Should().Be("Services");
        }
    }
}
