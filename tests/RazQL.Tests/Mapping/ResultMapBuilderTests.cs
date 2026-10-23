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

    [Fact]
    public void Collection_CreatesAndAssignsACollectionOncePerOwner()
    {
        var calls = 0;
        var map = ResultMap<UninitializedOwner>.Create(owner => owner
            .Collection<Child>("child_id", value => value.Children, collectionFactory: () =>
            {
                calls++;
                return new List<Child>();
            }));
        var relation = Assert.Single(map.Relations);
        var first = new UninitializedOwner();
        var second = new UninitializedOwner();
        var child = new Child();
        Assert.Equal(0, calls);

        Assert.True(relation.ApplyRelation(first, child));
        var collection = first.Children;
        relation.ApplyRelation(first, new Child());
        relation.ApplyRelation(second, child);

        Assert.NotNull(collection);
        Assert.Same(collection, first.Children);
        Assert.Equal(2, collection.Count);
        Assert.NotNull(second.Children);
        Assert.NotSame(collection, second.Children);
        Assert.Same(child, Assert.Single(second.Children));
        Assert.Equal(2, calls);
    }

    [Fact]
    public void Collection_DoesNotInvokeAFactoryForAnInitializedReadOnlyProperty()
    {
        var map = ResultMap<Owner>.Create(owner => owner
            .Collection<Child>("child_id", value => value.Children,
                collectionFactory: () => throw new InvalidOperationException("Must not be invoked.")));
        var owner = new Owner();
        var child = new Child();

        Assert.Single(map.Relations).ApplyRelation(owner, child);

        Assert.Same(child, Assert.Single(owner.Children));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Collection_ThrowsForANullPropertyWithoutAFactory(bool missingChild)
    {
        var map = ResultMap<UninitializedOwner>.Create(owner => owner
            .Collection<Child>("child_id", value => value.Children));
        var relation = Assert.Single(map.Relations);

        var error = Assert.Throws<InvalidOperationException>(() =>
            relation.ApplyRelation(new UninitializedOwner(), missingChild ? null : new Child()));

        Assert.Contains(typeof(UninitializedOwner).FullName!, error.Message);
        Assert.Contains(nameof(UninitializedOwner.Children), error.Message);
        Assert.Contains("factory", error.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Collection_InitializesForMissingChildrenAndHonorsNullChildConfiguration(bool addNullChildren)
    {
        var map = ResultMap<UninitializedOwner>.Create(owner => owner
            .Collection<Child?>("child_id", value => value.OptionalChildren, addNullChildren,
                collectionFactory: () => new List<Child?>()));
        var owner = new UninitializedOwner();

        Assert.True(Assert.Single(map.Relations).ApplyRelation(owner, null));

        Assert.NotNull(owner.OptionalChildren);
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
    public void Collection_ThrowsWhenAFactoryReturnsNull()
    {
        var map = ResultMap<UninitializedOwner>.Create(owner => owner
            .Collection<Child>("child_id", value => value.Children, collectionFactory: () => null!));

        var error = Assert.Throws<InvalidOperationException>(() =>
            Assert.Single(map.Relations).ApplyRelation(new UninitializedOwner(), new Child()));

        Assert.Contains(nameof(UninitializedOwner.Children), error.Message);
        Assert.Contains("non-null", error.Message);
    }

    [Fact]
    public void Collection_ThrowsWhenAFactoryCannotBeAssignedToTheDeclaredPropertyType()
    {
        var map = ResultMap<UninitializedOwner>.Create(owner => owner
            .Collection<Child>("child_id", value => value.Children, collectionFactory: () => new HashSet<Child>()));

        var error = Assert.Throws<InvalidOperationException>(() =>
            Assert.Single(map.Relations).ApplyRelation(new UninitializedOwner(), new Child()));

        Assert.Contains(nameof(UninitializedOwner.Children), error.Message);
        Assert.Contains("assignable", error.Message);
    }

    [Fact]
    public void Collection_ThrowsWhenANullPropertyHasNoSetterForTheFactoryResult()
    {
        var map = ResultMap<UninitializedOwner>.Create(owner => owner
            .Collection<Child>("child_id", value => value.ReadOnlyChildren, collectionFactory: () => new List<Child>()));

        var error = Assert.Throws<InvalidOperationException>(() =>
            Assert.Single(map.Relations).ApplyRelation(new UninitializedOwner(), new Child()));

        Assert.Contains(nameof(UninitializedOwner.ReadOnlyChildren), error.Message);
        Assert.Contains("setter", error.Message);
    }

    [Fact]
    public void Collection_PropagatesFactoryFailures()
    {
        var expected = new InvalidOperationException("Factory failed.");
        var map = ResultMap<UninitializedOwner>.Create(owner => owner
            .Collection<Child>("child_id", value => value.Children, collectionFactory: () => throw expected));

        var actual = Assert.Throws<InvalidOperationException>(() =>
            Assert.Single(map.Relations).ApplyRelation(new UninitializedOwner(), new Child()));

        Assert.Same(expected, actual);
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

    private sealed class UninitializedOwner
    {
        public List<Child>? Children { get; set; }
        public ICollection<Child?>? OptionalChildren { get; set; }
        public ICollection<Child>? ReadOnlyChildren => null;
    }
}
