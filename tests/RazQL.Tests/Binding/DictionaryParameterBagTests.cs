using RazQL.Binding;

namespace RazQL.Tests.Binding;

public class DictionaryParameterBagTests
{
    [Fact]
    public void Constructor_RejectsNullDictionary()
    {
        Assert.Throws<ArgumentNullException>(() => new DictionaryParameterBag(null!));
    }

    [Fact]
    public void AddIfAbsent_EvaluatesOnlyFirstFactoryAndPreservesValue()
    {
        var bag = new DictionaryParameterBag();
        var calls = 0;
        int CreateValue(int value)
        {
            calls++;
            return value;
        }

        Assert.True(bag.AddIfAbsent("id", CreateValue, 17));
        Assert.False(bag.AddIfAbsent("id", CreateValue, 99));

        Assert.Equal(1, calls);
        var parameter = Assert.Single(bag.GetParameters());
        Assert.Equal("id", parameter.Key);
        Assert.Equal(17, parameter.Value);
    }

    [Fact]
    public void Add_PopulatesSuppliedDictionaryAndRejectsDuplicateNames()
    {
        var values = new Dictionary<string, object?>();
        IParameterBag bag = new DictionaryParameterBag(values);

        bag.Add("first", (int value) => value + 1, 1);

        Assert.Equal(2, values["first"]);
        Assert.Throws<ArgumentException>(() => bag.Add("first", 3));
    }

    [Fact]
    public void ConvenienceOverloads_PreserveNullAndArrayValues()
    {
        IParameterBag bag = new DictionaryParameterBag();
        long[] ids = [1, 2];

        Assert.True(bag.AddIfAbsent("name", () => (string?)null));
        Assert.False(bag.AddIfAbsent("name", "ignored"));
        bag.Add("ids", ids);
        bag.Add("count", () => 2);

        var values = bag.GetParameters().ToDictionary(pair => pair.Key, pair => pair.Value);
        Assert.Null(values["name"]);
        Assert.Same(ids, values["ids"]);
        Assert.Equal(2, values["count"]);
    }

    [Fact]
    public void AddMethods_RejectBlankNamesAndNullFactories()
    {
        var bag = new DictionaryParameterBag();

        Assert.Throws<ArgumentException>(() => bag.AddIfAbsent(" ", value => value, 1));
        Assert.Throws<ArgumentNullException>(() => bag.AddIfAbsent<int, int>("id", null!, 1));
        Assert.Throws<ArgumentException>(() => bag.Add(" ", value => value, 1));
        Assert.Throws<ArgumentNullException>(() => bag.Add<int, int>("id", null!, 1));
    }
}
