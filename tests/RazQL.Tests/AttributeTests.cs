using System.Reflection;
using RazQL.Template;

namespace RazQL.Tests;

public class AttributeTests
{
    [Fact]
    public void TemplateSourceLoaderAttribute_IsAbstractCommonBase()
    {
        Assert.True(typeof(TemplateSourceLoaderAttribute).IsAbstract);
        Assert.True(typeof(TemplateSourceLoaderAttribute)
            .IsAssignableFrom(typeof(RazQLTemplateSourceAttribute)));
        Assert.True(typeof(TemplateSourceLoaderAttribute)
            .IsAssignableFrom(typeof(RazQLQueryAttribute)));
        Assert.Null(typeof(TemplateSourceLoaderAttribute).GetProperty("TemplateLocation"));
        Assert.Null(typeof(TemplateSourceLoaderAttribute).GetProperty("TemplateName"));
    }

    [Fact]
    public void MapperAttribute_StoresOptionalName()
    {
        var attribute = new RazQLMapperAttribute { Name = "Artists" };

        Assert.Equal("Artists", attribute.Name);
    }

    [Theory]
    [InlineData(typeof(RazQLMapperAttribute))]
    [InlineData(typeof(RazQLQueryAttribute))]
    [InlineData(typeof(TemplateSourceLoaderAttribute))]
    [InlineData(typeof(RazQLTemplateSourceAttribute))]
    [InlineData(typeof(RazQLQueryTemplateSourceAttribute))]
    public void MappingAttribute_IsNotInherited(Type attributeType)
    {
        var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        Assert.NotNull(usage);
        Assert.False(usage.Inherited);
    }

    [Fact]
    public void QueryAttribute_StoresQueryAndSelectsAttributeLoader()
    {
        var attribute = new RazQLQueryAttribute("select 1");

        Assert.Equal("select 1", attribute.Query);
        Assert.Equal(typeof(QueryAttributeTemplateSourceLoader), attribute.LoaderType);
    }

    [Fact]
    public void TemplateSourceAttribute_ExposesLoaderTypeAndLocation()
    {
        var attribute = new RazQLTemplateSourceAttribute(typeof(FileSystemTemplateSourceLoader))
        {
            TemplateLocation = "Shared"
        };

        Assert.Equal(typeof(FileSystemTemplateSourceLoader), attribute.LoaderType);
        Assert.Equal("Shared", attribute.TemplateLocation);
    }

    [Fact]
    public void TemplateSourceAttribute_AllowsLocationWithoutLoaderType()
    {
        var attribute = new RazQLTemplateSourceAttribute
        {
            TemplateLocation = "Shared"
        };

        Assert.Null(attribute.LoaderType);
        Assert.Equal("Shared", attribute.TemplateLocation);
    }

    [Fact]
    public void TemplateSourceAttribute_RejectsInvalidLoaderType()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new RazQLTemplateSourceAttribute(typeof(string)));

        Assert.Equal("loaderType", exception.ParamName);
    }

    [Fact]
    public void TemplateSourceAttribute_IsLimitedToInterfaces()
    {
        var usage = typeof(RazQLTemplateSourceAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Interface, usage.ValidOn);
    }

    [Fact]
    public void QueryTemplateSourceAttribute_ExposesMethodTemplateConfiguration()
    {
        var attribute = new RazQLQueryTemplateSourceAttribute(typeof(FileSystemTemplateSourceLoader))
        {
            TemplateLocation = "Shared",
            TemplateName = "Find"
        };

        Assert.Equal(typeof(FileSystemTemplateSourceLoader), attribute.LoaderType);
        Assert.Equal("Shared", attribute.TemplateLocation);
        Assert.Equal("Find", attribute.TemplateName);
        Assert.True(typeof(RazQLTemplateSourceAttribute)
            .IsAssignableFrom(typeof(RazQLQueryTemplateSourceAttribute)));
    }

    [Fact]
    public void QueryTemplateSourceAttribute_IsLimitedToMethods()
    {
        var usage = typeof(RazQLQueryTemplateSourceAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Method, usage.ValidOn);
    }

    [Fact]
    public void QueryAttribute_IsLimitedToMethods()
    {
        var usage = typeof(RazQLQueryAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Method, usage.ValidOn);
        Assert.Null(typeof(RazQLQueryAttribute).GetProperty("TemplateLocation"));
        Assert.Null(typeof(RazQLQueryAttribute).GetProperty("TemplateName"));
    }

    [Fact]
    public void MapperImplementationAttribute_StoresMapperAndImplementationTypes()
    {
        var attribute = new RazQLMapperImplementationAttribute(
            typeof(IAttributedMapper),
            typeof(AttributedMapper));

        Assert.Equal(typeof(IAttributedMapper), attribute.MapperType);
        Assert.Equal(typeof(AttributedMapper), attribute.ImplementationType);
    }

    [Fact]
    public void MapperImplementationAttribute_AllowsMultipleAssemblyRegistrations()
    {
        var usage = typeof(RazQLMapperImplementationAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Assembly, usage.ValidOn);
        Assert.True(usage.AllowMultiple);
    }
}

public interface IAttributedMapper;

public sealed class AttributedMapper : IAttributedMapper;
