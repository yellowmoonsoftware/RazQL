using NSubstitute;
using RazorEngineCore;
using RazQL.Binding;
using RazQL.Dapper.Mapping;

namespace RazQL.Dapper.Tests.Mapping;

public class ResultMapIdentityTests
{
    [Fact]
    public void ApplyResultRow_RestoresAnEarlierAssociationWhileDeduplicatingCollectionChildren()
    {
        var map = ResultMap<Album>.Create(album => album
            .Identity(value => value.Id)
            .Association<Artist?>("PrimaryArtistId", value => value.PrimaryArtist, artist => artist
                .Identity(value => value!.Id))
            .Collection<Artist>("ArtistId", value => value.Artists));
        var mapper = map.ToCorrelatedSequentialResultMap().CreateResultMapper<Album>();
        var firstArtist = new Artist { Id = 10 };
        var secondArtist = new Artist { Id = 20 };

        var (album, _) = mapper.ApplyResultRow([new Album { Id = 1 }, firstArtist, new Artist { Id = 10 }]);
        Assert.Same(firstArtist, album.PrimaryArtist);

        mapper.ApplyResultRow([new Album { Id = 1 }, secondArtist, new Artist { Id = 20 }]);
        Assert.Same(secondArtist, album.PrimaryArtist);

        mapper.ApplyResultRow([new Album { Id = 1 }, new Artist { Id = 10 }, new Artist { Id = 10 }]);

        Assert.Same(firstArtist, album.PrimaryArtist);
        Assert.Collection(album.Artists,
            value => Assert.Same(firstArtist, value),
            value => Assert.Same(secondArtist, value));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyResultRow_AssociationsUseTheLastValueIncludingRepeatedNulls(bool nullFirst)
    {
        var map = ResultMap<Album>.Create(album => album
            .Identity(value => value.Id)
            .Association<Artist?>("PrimaryArtistId", value => value.PrimaryArtist, artist => artist
                .Identity(value => value!.Id)));
        var mapper = map.ToCorrelatedSequentialResultMap().CreateResultMapper<Album>();
        var artist = new Artist { Id = 10 };
        var original = new Album { Id = 1, PrimaryArtist = new Artist { Id = -1 } };

        var (album, _) = mapper.ApplyResultRow([original, nullFirst ? null! : artist]);
        Assert.Same(nullFirst ? null : artist, album.PrimaryArtist);

        mapper.ApplyResultRow([new Album { Id = 1 }, nullFirst ? artist : null!]);
        Assert.Same(nullFirst ? artist : null, album.PrimaryArtist);

        mapper.ApplyResultRow([new Album { Id = 1 }, nullFirst ? null! : new Artist { Id = 10 }]);

        Assert.Same(original, album);
        Assert.Same(nullFirst ? null : artist, album.PrimaryArtist);
    }

    [Fact]
    public void ApplyResultRow_CreatesCollectionsPerOwnerAndPropertyWhileSharingChildren()
    {
        var calls = 0;
        ICollection<Artist?> CreateCollection()
        {
            calls++;
            return new List<Artist?>();
        }

        var map = ResultMap<OptionalArtists>.Create(album => album
            .Identity(value => value.Id)
            .Collection<Artist?>("ArtistId", value => value.Artists, artist => artist.Identity(value => value!.Id),
                collectionFactory: CreateCollection)
            .Collection<Artist?>("GuestArtistId", value => value.GuestArtists, collectionFactory: CreateCollection));
        var mapper = map.ToCorrelatedSequentialResultMap().CreateResultMapper<OptionalArtists>();
        var artist = new Artist { Id = 10 };

        var (first, _) = mapper.ApplyResultRow(
            [new OptionalArtists { Id = 1, Artists = null, GuestArtists = null }, artist, new Artist { Id = 10 }]);
        mapper.ApplyResultRow(
            [new OptionalArtists { Id = 1, Artists = null, GuestArtists = null }, new Artist { Id = 10 }, new Artist { Id = 10 }]);
        var (second, _) = mapper.ApplyResultRow(
            [new OptionalArtists { Id = 2, Artists = null, GuestArtists = null }, new Artist { Id = 10 }, new Artist { Id = 10 }]);

        Assert.NotNull(first.Artists);
        Assert.NotNull(first.GuestArtists);
        Assert.NotNull(second.Artists);
        Assert.NotNull(second.GuestArtists);
        Assert.Same(artist, Assert.Single(first.Artists));
        Assert.Same(artist, Assert.Single(first.GuestArtists));
        Assert.Same(artist, Assert.Single(second.Artists));
        Assert.Same(artist, Assert.Single(second.GuestArtists));
        Assert.NotSame(first.Artists, first.GuestArtists);
        Assert.NotSame(first.Artists, second.Artists);
        Assert.Equal(4, calls);
    }

    [Fact]
    public void ApplyResultRow_AssignsTheSameChildToTwoAssociationsOnOneOwner()
    {
        var map = ResultMap<Album>.Create(album => album
            .Identity(value => value.Id)
            .Association<Artist?>("PrimaryArtistId", value => value.PrimaryArtist, artist => artist
                .Identity(value => value!.Id))
            .Association<Artist?>("SecondaryArtistId", value => value.SecondaryArtist));
        var mapper = map.ToCorrelatedSequentialResultMap().CreateResultMapper<Album>();
        var artist = new Artist { Id = 10 };

        var (album, _) = mapper.ApplyResultRow([new Album { Id = 1 }, artist, new Artist { Id = 10 }]);
        mapper.ApplyResultRow([new Album { Id = 1 }, new Artist { Id = 10 }, new Artist { Id = 10 }]);

        Assert.Same(artist, album.PrimaryArtist);
        Assert.Same(artist, album.SecondaryArtist);
    }

    [Fact]
    public void ApplyResultRow_AddsTheSameChildOncePerCollectionAndOwner()
    {
        var map = ResultMap<Album>.Create(album => album
            .Identity(value => value.Id)
            .Collection<Artist>("ArtistId", value => value.Artists, artist => artist
                .Identity(value => value.Id))
            .Collection<Artist>("GuestArtistId", value => value.GuestArtists));
        var mapper = map.ToCorrelatedSequentialResultMap().CreateResultMapper<Album>();
        var artist = new Artist { Id = 10 };

        var (first, _) = mapper.ApplyResultRow([new Album { Id = 1 }, artist, new Artist { Id = 10 }]);
        mapper.ApplyResultRow([new Album { Id = 1 }, new Artist { Id = 10 }, new Artist { Id = 10 }]);
        var (second, _) = mapper.ApplyResultRow([new Album { Id = 2 }, new Artist { Id = 10 }, new Artist { Id = 10 }]);

        Assert.Same(artist, Assert.Single(first.Artists));
        Assert.Same(artist, Assert.Single(first.GuestArtists));
        Assert.Same(artist, Assert.Single(second.Artists));
        Assert.Same(artist, Assert.Single(second.GuestArtists));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyResultRow_SharesAChildBetweenAnAssociationAndCollection(bool collectionFirst)
    {
        var map = ResultMap<Album>.Create(album =>
        {
            album.Identity(value => value.Id);
            if (collectionFirst)
            {
                album.Collection<Artist>("ArtistId", value => value.Artists);
            }

            album.Association<Artist?>("PrimaryArtistId", value => value.PrimaryArtist, artist => artist
                .Identity(value => value!.Id));
            if (!collectionFirst)
            {
                album.Collection<Artist>("ArtistId", value => value.Artists);
            }
        });
        var mapper = map.ToCorrelatedSequentialResultMap().CreateResultMapper<Album>();
        var artist = new Artist { Id = 10 };

        var (album, _) = mapper.ApplyResultRow([new Album { Id = 1 }, artist, new Artist { Id = 10 }]);
        mapper.ApplyResultRow([new Album { Id = 1 }, new Artist { Id = 10 }, new Artist { Id = 10 }]);

        Assert.Same(artist, album.PrimaryArtist);
        Assert.Same(artist, Assert.Single(album.Artists));
    }

    [Fact]
    public void ApplyResultRow_DeduplicatesTheSameOwnerPropertyReachedThroughDifferentPaths()
    {
        var map = ResultMap<Album>.Create(album => album
            .Identity(value => value.Id)
            .Association<Song?>("FeaturedSongId", value => value.FeaturedSong, song => song
                .Identity(value => value!.Id)
                .Collection<Artist>("ArtistId", value => value!.Artists, artist => artist
                    .Identity(value => value.Id)))
            .Collection<Song>("SongId", value => value.Songs, song => song
                .Collection<Artist>("ArtistId", value => value.Artists)));
        var mapper = map.ToCorrelatedSequentialResultMap().CreateResultMapper<Album>();
        var artist = new Artist { Id = 10 };

        var (album, _) = mapper.ApplyResultRow(
            [new Album { Id = 1 }, new Song { Id = 100 }, artist, new Song { Id = 100 }, new Artist { Id = 10 }]);
        mapper.ApplyResultRow(
            [new Album { Id = 1 }, new Song { Id = 100 }, new Artist { Id = 10 }, new Song { Id = 100 }, new Artist { Id = 10 }]);

        var song = Assert.Single(album.Songs);
        Assert.Same(song, album.FeaturedSong);
        Assert.Same(artist, Assert.Single(song.Artists));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyResultRow_TracksMissingChildrenSeparatelyForEachCollection(bool addNullChildren)
    {
        var map = ResultMap<OptionalArtists>.Create(album => album
            .Identity(value => value.Id)
            .Collection<Artist?>("ArtistId", value => value.Artists, addNullChildren)
            .Collection<Artist?>("GuestArtistId", value => value.GuestArtists, addNullChildren));
        var mapper = map.ToCorrelatedSequentialResultMap().CreateResultMapper<OptionalArtists>();

        var (album, _) = mapper.ApplyResultRow([new OptionalArtists { Id = 1 }, null!, null!]);
        mapper.ApplyResultRow([new OptionalArtists { Id = 1 }, null!, null!]);

        if (addNullChildren)
        {
            Assert.NotNull(album.Artists);
            Assert.NotNull(album.GuestArtists);
            Assert.Null(Assert.Single(album.Artists));
            Assert.Null(Assert.Single(album.GuestArtists));
        }
        else
        {
            Assert.NotNull(album.Artists);
            Assert.NotNull(album.GuestArtists);
            Assert.Empty(album.Artists);
            Assert.Empty(album.GuestArtists);
        }
    }

    [Fact]
    public async Task RunAsync_RegistersTupleIdentityFromARazorTemplate()
    {
        var engine = new RazorEngine();
        var template = await engine.CompileAsync<RazQLModel<int, Artist>>("""
            @{
                ResultMap(artist => artist.Identity(value => (value.TenantId, value.Id)));
            }
            select 1
            """);
        var builder = new ResultMapBuilder<Artist>();

        var sql = await template.RunAsync(model =>
        {
            model.Model = Substitute.For<IDataBinder<int>>();
            model.ResultMapBuilder = builder;
        });
        var mapper = builder.Build().ToCorrelatedSequentialResultMap().CreateResultMapper<Artist>();
        var (first, _) = mapper.ApplyResultRow([new Artist { TenantId = 1, Id = 2 }]);
        var (second, isNew) = mapper.ApplyResultRow([new Artist { TenantId = 1, Id = 2 }]);

        Assert.Contains("select 1", sql);
        Assert.Same(first, second);
        Assert.False(isNew);
    }

    [Fact]
    public void ApplyResultRow_SharesCanonicalObjectsAcrossRelationships()
    {
        var map = ResultMap<Album>.Create(album => album
            .Identity(value => value.Id)
            .Association<Artist?>("PrimaryArtistId", value => value.PrimaryArtist)
            .Collection<Song>("SongId", value => value.Songs, song => song
                .Identity(value => value.Id)
                .Collection<Artist>("ArtistId", value => value.Artists, artist => artist
                    .Identity(value => value.Id))));
        var mapper = map.ToCorrelatedSequentialResultMap().CreateResultMapper<Album>();
        var artist = new Artist { Id = 10 };

        var (first, firstIsNew) = mapper.ApplyResultRow(
            [new Album { Id = 1 }, artist, new Song { Id = 100 }, new Artist { Id = 10 }]);
        var (second, secondIsNew) = mapper.ApplyResultRow(
            [new Album { Id = 1 }, new Artist { Id = 10 }, new Song { Id = 100 }, new Artist { Id = 20 }]);

        Assert.True(firstIsNew);
        Assert.False(secondIsNew);
        Assert.Same(first, second);
        Assert.Same(artist, first.PrimaryArtist);
        var songResult = Assert.Single(first.Songs);
        Assert.Equal(2, songResult.Artists.Count);
        Assert.Same(artist, songResult.Artists.First());
    }

    [Fact]
    public void ApplyResultRow_UsesCompositeIdentityForRootObjects()
    {
        var map = ResultMap<Artist>.Create(artist => artist.Identity(value => (value.TenantId, value.Id)));
        var mapper = map.ToCorrelatedSequentialResultMap().CreateResultMapper<Artist>();

        var (first, _) = mapper.ApplyResultRow([new Artist { TenantId = 1, Id = 10 }]);
        var (equal, equalIsNew) = mapper.ApplyResultRow([new Artist { TenantId = 1, Id = 10 }]);
        var (different, differentIsNew) = mapper.ApplyResultRow([new Artist { TenantId = 2, Id = 10 }]);

        Assert.Same(first, equal);
        Assert.False(equalIsNew);
        Assert.NotSame(first, different);
        Assert.True(differentIsNew);
    }

    [Fact]
    public void ApplyResultRow_EmitsRootPreviouslySeenAsAChild()
    {
        var map = ResultMap<Node>.Create(node => node
            .Identity(value => value.Id)
            .Association<Node?>("NextId", value => value.Next));
        var mapper = map.ToCorrelatedSequentialResultMap().CreateResultMapper<Node>();
        var child = new Node { Id = 2 };

        var (firstRoot, _) = mapper.ApplyResultRow([new Node { Id = 1 }, child]);
        var (root, isNewRoot) = mapper.ApplyResultRow([new Node { Id = 2 }, null!]);

        Assert.True(isNewRoot);
        Assert.Same(child, root);
        Assert.Same(root, firstRoot.Next);
        var (_, repeatedIsNew) = mapper.ApplyResultRow([new Node { Id = 2 }, null!]);
        Assert.False(repeatedIsNew);
    }

    [Fact]
    public void MultiMapFunction_DistinguishesNullResultsFromSkippedRows()
    {
        var mapper = ResultMap<Artist?>.Create(_ => { })
            .ToCorrelatedSequentialResultMap().CreateResultMapper<Artist?>();
        var mapRow = mapper.MultiMapFunction;

        var presentNull = mapRow([null!]);
        var skipped = mapRow([null!]);

        Assert.True(presentNull.HasResult);
        Assert.Null(presentNull.Value);
        Assert.False(skipped.HasResult);
        Assert.Null(skipped.Value);
    }

    [Fact]
    public void MultiMapFunction_DistinguishesDefaultValueResultsFromSkippedRows()
    {
        var mapper = ResultMap<int>.Create(map => map.Identity(value => value))
            .ToCorrelatedSequentialResultMap().CreateResultMapper<int>();
        var mapRow = mapper.MultiMapFunction;

        var zero = mapRow([0]);
        var skipped = mapRow([0]);
        var one = mapRow([1]);

        Assert.True(zero.HasResult);
        Assert.Equal(0, zero.Value);
        Assert.False(skipped.HasResult);
        Assert.True(one.HasResult);
        Assert.Equal(1, one.Value);
    }

    [Fact]
    public void ApplyResultRow_ReusesRootWhenRelationshipsChangeDtoEquality()
    {
        var map = ResultMap<AlbumRecord>.Create(album => album
            .Identity(value => value.Id)
            .Association<Artist?>("ArtistId", value => value.PrimaryArtist, artist => artist
                .Identity(value => value?.Id)));
        var mapper = map.ToCorrelatedSequentialResultMap().CreateResultMapper<AlbumRecord>();
        var original = new AlbumRecord(1);
        var artist = new Artist { Id = 10 };

        var (first, firstIsNew) = mapper.ApplyResultRow([original, artist]);
        var (second, secondIsNew) = mapper.ApplyResultRow([new AlbumRecord(1), new Artist { Id = 10 }]);

        Assert.True(firstIsNew);
        Assert.False(secondIsNew);
        Assert.Same(original, first);
        Assert.Same(first, second);
        Assert.Same(artist, second.PrimaryArtist);
    }

    [Fact]
    public void CreateResultMapper_KeepsObjectCachesIndependentAcrossExecutions()
    {
        var map = ResultMap<Artist>.Create(artist => artist.Identity(value => value.Id))
            .ToCorrelatedSequentialResultMap();

        var (first, _) = map.CreateResultMapper<Artist>().ApplyResultRow([new Artist { Id = 1 }]);
        var (second, _) = map.CreateResultMapper<Artist>().ApplyResultRow([new Artist { Id = 1 }]);

        Assert.NotSame(first, second);
    }

    [Fact]
    public void Identity_RejectsDuplicateRegistrationsAcrossMappingPaths()
    {
        var error = Assert.Throws<InvalidOperationException>(() => ResultMap<Album>.Create(album => album
            .Association<Artist?>("ArtistId", value => value.PrimaryArtist, artist => artist
                .Identity(value => value?.Id))
            .Collection<Song>("SongId", value => value.Songs, song => song
                .Collection<Artist>("ArtistId", value => value.Artists, artist => artist
                    .Identity(value => value.Id)))));

        Assert.Contains(typeof(Artist).ToString(), error.Message);
    }

    [Fact]
    public void Identity_RejectsNullSelector()
    {
        var builder = new ResultMapBuilder<Artist>();

        Assert.Throws<ArgumentNullException>(() => builder.Identity<int>(null!));
    }

    [Fact]
    public void Build_SnapshotsIdentityRegistrations()
    {
        var builder = new ResultMapBuilder<Album>();
        var before = builder.Build();
        builder.Identity(value => value.Id);
        var after = builder.Build();

        Assert.Empty(before.IdentitySelectors);
        Assert.Single(after.IdentitySelectors);
        var selectors = Assert.IsAssignableFrom<IDictionary<Type, Func<object, object?>>>(after.IdentitySelectors);
        Assert.Throws<NotSupportedException>(() => selectors.Clear());
    }

    public sealed class Album
    {
        public int Id { get; init; }
        public Artist? PrimaryArtist { get; set; }
        public Artist? SecondaryArtist { get; set; }
        public Song? FeaturedSong { get; set; }
        public ICollection<Artist> Artists { get; } = new List<Artist>();
        public ICollection<Artist> GuestArtists { get; } = new List<Artist>();
        public ICollection<Song> Songs { get; } = new List<Song>();
    }

    public sealed class OptionalArtists
    {
        public int Id { get; init; }
        public ICollection<Artist?>? Artists { get; set; } = new List<Artist?>();
        public ICollection<Artist?>? GuestArtists { get; set; } = new List<Artist?>();
    }

    public sealed record AlbumRecord(int Id)
    {
        public Artist? PrimaryArtist { get; set; }
    }

    public sealed class Song
    {
        public int Id { get; init; }
        public ICollection<Artist> Artists { get; } = new List<Artist>();
    }

    public sealed class Artist
    {
        public int TenantId { get; init; }
        public int Id { get; init; }
    }

    public sealed class Node
    {
        public int Id { get; init; }
        public Node? Next { get; set; }
    }
}
