namespace RazQL.Binding;

/// <summary>Specifies optional direction and null placement for an SQL sort.</summary>
/// <param name="Direction">The requested sort direction, or <see langword="null"/> to omit it.</param>
/// <param name="NullsOrder">The requested null placement, or <see langword="null"/> to omit it.</param>
public record OrderSortAndNulls(OrderByDirection? Direction, OrderByNulls? NullsOrder);

/// <summary>Identifies a permitted sort target and its optional ordering directives.</summary>
/// <typeparam name="TEnum">The enum used by the template to map the target to trusted SQL.</typeparam>
/// <param name="Column">The logical column or expression identifier.</param>
/// <param name="Direction">The requested sort direction, or <see langword="null"/> to omit it.</param>
/// <param name="NullsOrder">The requested null placement, or <see langword="null"/> to omit it.</param>
public sealed record OrderSpec<TEnum>(TEnum Column, OrderByDirection? Direction = null, OrderByNulls? NullsOrder = null)
    : OrderSortAndNulls(Direction, NullsOrder) where TEnum : struct, Enum;
