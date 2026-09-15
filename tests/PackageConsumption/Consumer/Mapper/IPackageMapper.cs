using RazQL;

namespace RazQL.PackageConsumption.Mapper;

[RazQLMapper]
public interface IPackageMapper
{
    Task<int> FindAsync(int criteria, CancellationToken cancellationToken = default);
}
