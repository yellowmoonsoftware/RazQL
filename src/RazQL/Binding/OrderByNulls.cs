namespace RazQL.Binding;

/// <summary>Specifies where null values appear in an SQL sort.</summary>
public enum OrderByNulls
{
    /// <summary>Place null values before non-null values.</summary>
    First,

    /// <summary>Place null values after non-null values.</summary>
    Last
}
