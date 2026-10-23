using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using RazQL.Binding;
using RazQL.Execution;

namespace RazQL.Dapper.Tests.Mapping;

public class DapperIdentityExecutionTests
{
    private readonly DapperExecutionAdapter _adapter = new();

    [Fact]
    public async Task QueryAsync_PreservesRowsAndIgnoresIdentityWithoutRelationships()
    {
        using var connection = new TableConnection(CreateTable(1, 1, 2));
        var query = Query(ResultMap<Album>.Create(album => album.Identity<int>(_ =>
            throw new NotSupportedException("Flat queries must not resolve identities."))));

        var results = (await _adapter.QueryAsync(connection, query, default)).ToList();

        Assert.Equal([1, 1, 2], results.Select(value => value.Id));
        Assert.NotSame(results[0], results[1]);
    }

    [Fact]
    public async Task QueryAsync_PreservesRowsWithoutRelationshipsOrIdentityConfiguration()
    {
        using var connection = new TableConnection(CreateTable(1, 1));
        var query = Query(ResultMap<Album>.Create(_ => { }));

        var results = (await _adapter.QueryAsync(connection, query, default)).ToList();

        Assert.Equal(2, results.Count);
        Assert.NotSame(results[0], results[1]);
    }

    [Fact]
    public async Task QueryAsync_PreservesDefaultValueTypeRoots()
    {
        using var connection = new TableConnection(CreateTable(1, 1, 0));
        var query = Query(ResultMap<Number>.Create(number => number.Identity(value => value.Id)));

        var results = (await _adapter.QueryAsync(connection, query, default)).ToList();

        Assert.Equal([1, 1, 0], results.Select(value => value.Id));
    }

    [Fact]
    public async Task QueryAsync_PreservesDefaultValueTypeRootsWithRelationships()
    {
        var table = CreateArtistTable();
        table.Rows.Clear();
        table.Rows.Add(0, DBNull.Value, DBNull.Value);
        table.Rows.Add(0, DBNull.Value, DBNull.Value);
        using var connection = new TableConnection(table);
        var query = Query(ResultMap<NumberWithArtist>.Create(number => number
            .Identity(value => value.Id)
            .Association<Artist?>("ArtistId", value => value.Artist)));

        var result = Assert.Single(await _adapter.QueryAsync(connection, query, default));

        Assert.Equal(default, result);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_AssemblesAllChildrenOfOneRoot()
    {
        using var connection = new TableConnection(CreateArtistTable());
        var query = AlbumWithArtistsQuery();

        var result = await _adapter.QuerySingleOrDefaultAsync(connection, query, default);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal([10, 20], result.Artists.Select(value => value.ArtistId));
    }

    [Fact]
    public async Task QueryAsync_UsesDtoEqualityForChildrenWithoutIdentitySelectors()
    {
        using var connection = new TableConnection(CreateArtistTable());

        var result = Assert.Single(await _adapter.QueryAsync(connection, AlbumWithArtistsQuery(), default));

        Assert.Equal([10, 20], result.Artists.Select(value => value.ArtistId));
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_ThrowsForRepeatedRowsWithoutRelationships()
    {
        using var connection = new TableConnection(CreateTable(1, 1));
        var query = Query(ResultMap<Album>.Create(album => album.Identity<int>(_ =>
            throw new NotSupportedException("Flat queries must not resolve identities."))));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _adapter.QuerySingleOrDefaultAsync(connection, query, default));
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_ReturnsNullForNoRoots()
    {
        var table = CreateArtistTable();
        table.Rows.Clear();
        using var connection = new TableConnection(table);
        var query = AlbumWithArtistsQuery();

        var result = await _adapter.QuerySingleOrDefaultAsync(connection, query, default);

        Assert.Null(result);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_ThrowsForMultipleDistinctRoots()
    {
        var table = CreateArtistTable();
        table.Rows.Add(2, 30, "third");
        using var connection = new TableConnection(table);
        var query = AlbumWithArtistsQuery();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _adapter.QuerySingleOrDefaultAsync(connection, query, default));
    }

    private static ParameterizedQueryResult<Album> AlbumWithArtistsQuery() => Query(
        ResultMap<Album>.Create(album => album
            .Identity(value => value.Id)
            .Collection<Artist>("ArtistId", value => value.Artists)));

    private static ParameterizedQueryResult<TResult> Query<TResult>(ResultMap<TResult> map)
    {
        var parameters = Substitute.For<IParameterBag>();
        parameters.GetParameters().Returns([]);
        return new ParameterizedQueryResult<TResult>("identity-test", parameters, map);
    }

    private static DataTable CreateTable(params int[] ids)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        foreach (var id in ids)
        {
            table.Rows.Add(id);
        }

        return table;
    }

    private static DataTable CreateArtistTable()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("ArtistId", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add(1, 10, "first");
        table.Rows.Add(1, 10, "first");
        table.Rows.Add(1, 20, "second");
        return table;
    }

    public sealed class Album
    {
        public int Id { get; set; }
        public ICollection<Artist> Artists { get; } = new List<Artist>();
    }

    public sealed record Artist(int ArtistId, string Name);
    public readonly record struct Number(int Id);

    public record struct NumberWithArtist
    {
        public int Id { get; set; }
        public Artist? Artist { get; set; }
    }

    private sealed class TableConnection(DataTable table) : DbConnection
    {
        [AllowNull]
        public override string ConnectionString { get; set; } = "identity-tests";
        public override string Database => "identity-tests";
        public override string DataSource => "identity-tests";
        public override string ServerVersion => "1";
        public override ConnectionState State => ConnectionState.Open;
        public override void Open() { }
        public override void Close() { }
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new TableCommand(this, table);
    }

    private sealed class TableCommand(DbConnection connection, DataTable table) : DbCommand
    {
        [AllowNull]
        public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; } = connection;
        protected override DbTransaction? DbTransaction { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; } = new EmptyParameters();
        public override void Cancel() { }
        public override void Prepare() { }
        public override int ExecuteNonQuery() => throw new NotSupportedException();
        public override object? ExecuteScalar() => throw new NotSupportedException();
        protected override DbParameter CreateDbParameter() => throw new NotSupportedException();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => table.CreateDataReader();
    }

    private sealed class EmptyParameters : DbParameterCollection
    {
        public override int Count => 0;
        public override object SyncRoot => this;
        public override int Add(object value) => throw new NotSupportedException();
        public override void AddRange(Array values) => throw new NotSupportedException();
        public override void Clear() { }
        public override bool Contains(object value) => false;
        public override bool Contains(string value) => false;
        public override void CopyTo(Array array, int index) { }
        public override IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();
        public override int IndexOf(object value) => -1;
        public override int IndexOf(string parameterName) => -1;
        public override void Insert(int index, object value) => throw new NotSupportedException();
        public override void Remove(object value) => throw new NotSupportedException();
        public override void RemoveAt(int index) => throw new NotSupportedException();
        public override void RemoveAt(string parameterName) => throw new NotSupportedException();
        protected override DbParameter GetParameter(int index) => throw new NotSupportedException();
        protected override DbParameter GetParameter(string parameterName) => throw new NotSupportedException();
        protected override void SetParameter(int index, DbParameter value) => throw new NotSupportedException();
        protected override void SetParameter(string parameterName, DbParameter value) => throw new NotSupportedException();
    }
}
