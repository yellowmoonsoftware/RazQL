using RazQL.Binding;
using RazQL.Dapper.Mapping;

namespace RazQL.Dapper.Tests.Mapping;

public class ResultMapExtensionsTests
{
    [Fact]
    public void ToCorrelatedSequentialResultMap_PreservesDepthFirstGroupsAndTheirActualOwners()
    {
        var map = ResultMap<Root>.Create(root => root
            .Identity(value => value.Id)
            .Collection<Branch>("branch_id", value => value.Branches, branch => branch
                .Collection<Leaf>("leaf_id", value => value.Leaves, leaf => leaf.Identity(value => value.Id))
                .Association<Detail?>("detail_id", value => value.Detail))
            .Association<Leaf?>("primary_id", value => value.Primary));

        var sequential = map.ToCorrelatedSequentialResultMap();

        Assert.Equal([typeof(Root), typeof(Branch), typeof(Leaf), typeof(Detail), typeof(Leaf)], sequential.Types);
        Assert.Equal("branch_id,leaf_id,detail_id,primary_id", sequential.SplitOn);
        Assert.Equal([(1, 0), (2, 1), (3, 1), (4, 0)],
            sequential.CorrelatedRelations.Select(value => (value.Index, value.ParentIndex)));
        Assert.Same(map.IdentitySelectors, sequential.IdentitySelectors);
    }

    private sealed class Root
    {
        public int Id { get; init; }
        public ICollection<Branch> Branches { get; } = new List<Branch>();
        public Leaf? Primary { get; set; }
    }

    private sealed class Branch
    {
        public ICollection<Leaf> Leaves { get; } = new List<Leaf>();
        public Detail? Detail { get; set; }
    }

    private sealed class Leaf
    {
        public int Id { get; init; }
    }

    private sealed class Detail;
}
