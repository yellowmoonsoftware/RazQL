using RazQL.Binding;

namespace RazQL.Tests.Mapping;

public class ResultMapBuilderTests
{
    [Fact]
    public void Association_AssignsAChildAndCanClearIt()
    {
        var map = ResultMap<Owner>.Create(owner => owner.Association<Child?>("child_id", value => value.Child));
        var relation = Assert.Single(map.Relations);
        var owner = new Owner();
        var child = new Child();

        Assert.True(relation.ApplyRelation(owner, child));
        Assert.Same(child, owner.Child);
        Assert.True(relation.ApplyRelation(owner, null));
        Assert.Null(owner.Child);
    }

    [Fact]
    public void Collection_AddsToAReadOnlyPropertyWithAnInheritedCollectionInterface()
    {
        var map = ResultMap<Owner>.Create(owner => owner.Collection<Child>("child_id", value => value.Children));
        var owner = new Owner();
        var child = new Child();

        Assert.Single(map.Relations).ApplyRelation(owner, child);

        Assert.Same(child, Assert.Single(owner.Children));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Collection_ControlsWhetherMissingChildrenAreAdded(bool addNullChildren)
    {
        var map = ResultMap<Owner>.Create(owner => owner
            .Collection<Child?>("child_id", value => value.OptionalChildren, addNullChildren));
        var owner = new Owner();

        Assert.True(Assert.Single(map.Relations).ApplyRelation(owner, null));

        if (addNullChildren)
        {
            Assert.Null(Assert.Single(owner.OptionalChildren));
        }
        else
        {
            Assert.Empty(owner.OptionalChildren);
        }
    }

    [Fact]
    public void Build_SnapshotsRelationshipsBeforeSubsequentBuilderChanges()
    {
        var builder = new ResultMapBuilder<Owner>();
        var empty = builder.Build();
        builder.Association<Child?>("child_id", value => value.Child);

        var populated = builder.Build();

        Assert.Empty(empty.Relations);
        Assert.False(empty.HasMappedAssociations);
        Assert.Single(populated.Relations);
        Assert.True(populated.HasMappedAssociations);
        Assert.Equal(typeof(Owner), populated.ResultType);
    }

    [Fact]
    public void Association_RejectsAnExpressionThatDoesNotSelectAProperty()
    {
        Assert.Throws<ArgumentException>(() => ResultMap<Owner>.Create(owner => owner
            .Association<Child>("child_id", _ => new Child())));
    }

    [Fact]
    public void Collection_RejectsAnExpressionThatDoesNotSelectAProperty()
    {
        Assert.Throws<ArgumentException>(() => ResultMap<Owner>.Create(owner => owner
            .Collection<Child>("child_id", value => value.Children.ToList())));
    }

    private sealed class Owner
    {
        public Child? Child { get; set; }
        public IList<Child> Children { get; } = new List<Child>();
        public ICollection<Child?> OptionalChildren { get; } = new List<Child?>();
    }

    private sealed class Child;
}
