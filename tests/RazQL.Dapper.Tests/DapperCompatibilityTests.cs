using System.Data;
using System.Reflection;
using Dapper;

namespace RazQL.Dapper.Tests;

public class DapperCompatibilityTests
{
    [Fact]
    public void FindMultiMapAsyncMethod_BindsTheLoadedDapperImplementation()
    {
        var version = typeof(SqlMapper).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        Assert.NotNull(version);
        Assert.Equal("2.1.89", version.Split('+')[0]);
        var method = DapperExtensions.FindMultiMapAsyncMethod(typeof(SqlMapper));

        var callback = method.MakeGenericMethod(typeof(int))
            .CreateDelegate<Func<IDbConnection, CommandDefinition, Type[], Func<object[], int>, string, Task<IEnumerable<int>>>>();

        Assert.Equal(typeof(SqlMapper), callback.Method.DeclaringType);
    }

    [Fact]
    public async Task FindMultiMapAsyncMethod_IgnoresUnrelatedOverloadsAndBindsTheExactSignature()
    {
        var method = DapperExtensions.FindMultiMapAsyncMethod(typeof(CompatibleMapper));
        var callback = method.MakeGenericMethod(typeof(int))
            .CreateDelegate<Func<IDbConnection, CommandDefinition, Type[], Func<object[], int>, string, Task<IEnumerable<int>>>>();

        var result = await callback(null!, default, [typeof(int)], values => (int)values[0], "id");

        Assert.Equal(42, Assert.Single(result));
    }

    [Theory]
    [InlineData(typeof(MissingMapper))]
    [InlineData(typeof(NonGenericCallbackMapper))]
    [InlineData(typeof(WrongCallbackArgumentMapper))]
    [InlineData(typeof(WrongReturnMapper))]
    [InlineData(typeof(ConstrainedResultMapper))]
    [InlineData(typeof(WrongGenericArityMapper))]
    public void FindMultiMapAsyncMethod_ReportsIncompatibleSignaturesWithVersionAndRecoveryDetails(Type mapperType)
    {
        var error = Assert.Throws<NotSupportedException>(() => DapperExtensions.FindMultiMapAsyncMethod(mapperType));
        var assembly = mapperType.Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        Assert.Contains("association/collection multi-mapping", error.Message);
        Assert.Contains(assembly.GetName().Name!, error.Message);
        Assert.Contains(assembly.GetName().Version!.ToString(), error.Message);
        Assert.NotNull(version);
        Assert.Contains(version, error.Message);
        Assert.Contains("found 0", error.Message);
        Assert.Contains("Task<IEnumerable<TResult>> MultiMapAsync<TResult>", error.Message);
        Assert.Contains("Dapper 2.1.89", error.Message);
        Assert.Contains("Pin", error.Message);
        Assert.Contains("Queries without mapped relationships", error.Message);
    }

    [Fact]
    public void FindMultiMapAsyncMethod_ReportsAmbiguousSignatures()
    {
        var error = Assert.Throws<NotSupportedException>(() =>
            DapperExtensions.FindMultiMapAsyncMethod(new AmbiguousMapperType()));

        Assert.Contains("found 2", error.Message);
        Assert.Contains("Dapper 2.1.89", error.Message);
    }

    private sealed class AmbiguousMapperType() : TypeDelegator(typeof(CompatibleMapper))
    {
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            var methods = base.GetMethods(bindingAttr);
            return [.. methods, .. methods];
        }
    }

    private static class CompatibleMapper
    {
        private static Task<IEnumerable<TResult>> MultiMapAsync<TResult>(IDbConnection connection,
            CommandDefinition command, Type[] types, Func<object[], TResult> map, string splitOn) =>
            Task.FromResult<IEnumerable<TResult>>([map([42])]);

        private static Task<IEnumerable<TResult>> MultiMapAsync<TResult>(IDbConnection connection,
            CommandDefinition command, Type[] types, object map, string splitOn) => throw new NotSupportedException();
    }

    private static class MissingMapper;

    private static class NonGenericCallbackMapper
    {
        private static Task<IEnumerable<TResult>> MultiMapAsync<TResult>(IDbConnection connection,
            CommandDefinition command, Type[] types, object map, string splitOn) => throw new NotSupportedException();
    }

    private static class WrongCallbackArgumentMapper
    {
        private static Task<IEnumerable<TResult>> MultiMapAsync<TResult>(IDbConnection connection,
            CommandDefinition command, Type[] types, Func<string[], TResult> map, string splitOn) => throw new NotSupportedException();
    }

    private static class WrongReturnMapper
    {
        private static Task<TResult> MultiMapAsync<TResult>(IDbConnection connection,
            CommandDefinition command, Type[] types, Func<object[], TResult> map, string splitOn) => throw new NotSupportedException();
    }

    private static class ConstrainedResultMapper
    {
        private static Task<IEnumerable<TResult>> MultiMapAsync<TResult>(IDbConnection connection,
            CommandDefinition command, Type[] types, Func<object[], TResult> map, string splitOn) where TResult : class =>
            throw new NotSupportedException();
    }

    private static class WrongGenericArityMapper
    {
        private static Task<IEnumerable<TResult>> MultiMapAsync<TResult, TOther>(IDbConnection connection,
            CommandDefinition command, Type[] types, Func<object[], TResult> map, string splitOn) => throw new NotSupportedException();
    }
}
