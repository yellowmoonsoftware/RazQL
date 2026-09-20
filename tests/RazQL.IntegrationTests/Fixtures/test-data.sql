set role gmdb_readwrite;
SET search_path TO gmdb;


-- Insert Artist
insert into artist(name, type)
select a->>'Name',(a->>'ArtistType')::artist_type
from jsonb_array_elements($json$[
  {
    "Name" : "AC/DC",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Aerosmith",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Audioslave",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Barbara Logan",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Black Sabbath",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Brad Wilk",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Chris Cornell",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Cinderella",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Dave Grohl",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "David A. Stewart",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Doyle Bramhall",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Duff McKagan",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Fleetwood Mac",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Foo Fighters",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Green Day",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Guns N' Roses",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Izzy Stradlin",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Jeff Lyne",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Jimi Hendrix",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "John Keene",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Led Zeppelin",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Metallica",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Mike Campbell",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Neil Young",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Ozzy Osbourne",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Pearl Jam",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Pete Townshend",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Pink Floyd",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Poison",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Queen",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Rage Against The Machine",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Ratt",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Rush",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Ry Cooder",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Skid Row",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Slash",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Soundgarden",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Steve Vai",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Steven Adler",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Stevie Ray Vaughan",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Stone Temple Pilots",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "The Rolling Stones",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "The Who",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Tim Commerford",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Tom Keifer",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Tom Morello",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Tom Petty",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Tom Petty & the Heartbreakers",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Van Halen",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "W. Axl Rose",
    "ArtistType" : "PERSON"
  },
  {
    "Name" : "Yes",
    "ArtistType" : "BAND"
  },
  {
    "Name" : "Zack de la Rocha",
    "ArtistType" : "PERSON"
  }
]$json$::jsonb) as a;


-- Insert Albums
insert into album(title, details, primary_artist_id)
select
    a->>'Album' title,
    jsonb_strip_nulls(
        jsonb_build_object(
                'releaseDate', a->>'ReleaseDate',
                'resources', a->'Resources',
                'resourceId', a->>'ResourceId'
        )) details,
    artist.id primary_artist_id
from jsonb_array_elements(
             $json$
             [
               {
                 "Album" : "Appetite For Destruction",
                 "PerformedBy" : "Guns N' Roses",
                 "ReleaseDate" : "1987-07-21",
                 "ResourceId": "4b8c2c6f-0a74-4d5a-a271-f4734b6ce8a2",
                 "Resources": {
                   "ALBUM_ART": {
                     "mediaType": "image/png",
                     "originalFilename": "gmdb-test-album-art.png"
                   }
                 }
               },
               {
                 "Album" : "Rage Against The Machine",
                 "PerformedBy" : "Rage Against The Machine",
                 "ReleaseDate" : "1992-11-03"
               },
               {
                 "Album" : "The Sky Is Crying",
                 "PerformedBy" : "Stevie Ray Vaughan",
                 "ReleaseDate" : "1991-11-05"
               },
               {
                 "Album" : "Long Cold Winter",
                 "PerformedBy" : "Cinderella",
                 "ReleaseDate" : "1988-05-21"
               },
               {
                 "Album" : "Greatest Hits",
                 "PerformedBy" : "Tom Petty & the Heartbreakers",
                 "ReleaseDate" : "1993-11-16"
               },
               {
                 "Album" : "Meaty Beaty Big and Bouncy",
                 "PerformedBy" : "The Who",
                 "ReleaseDate" : "1971-10-30"
               },
               {
                 "Album" : "The Colour and the Shape",
                 "PerformedBy" : "Foo Fighters",
                 "ReleaseDate" : "1997-05-20"
               },
               {
                 "Album" : "Lost Dogs",
                 "PerformedBy" : "Pearl Jam",
                 "ReleaseDate" : "2003-11-11"
               },
               {
                 "Album" : "Who's Next",
                 "PerformedBy" : "The Who"
               }
             ] $json$::jsonb) as a
         inner join artist on a->>'PerformedBy' = artist."name";

-- Insert Songs
with songs as (
    select x.* from jsonb_array_elements($json$[
      {
        "name" : "American Girl",
        "album" : "Greatest Hits",
        "track": 1
      },
      {
        "name" : "Bombtrack",
        "album" : "Rage Against The Machine",
        "track": 1
      },
      {
        "name" : "Breakdown",
        "album" : "Greatest Hits",
        "track": 2
      },
      {
        "name" : "Don't Come Around Here No More",
        "album" : "Greatest Hits",
        "track": 11
      },
      {
        "name" : "Don't Do Me Like That",
        "album" : "Greatest Hits",
        "track": 6
      },
      {
        "name" : "Even The Losers",
        "album" : "Greatest Hits",
        "track": 7
      },
      {
        "name" : "Everlong",
        "album" : "The Colour and the Shape",
        "track": 11
      },
      {
        "name" : "Free Fallin'",
        "album" : "Greatest Hits",
        "track": 14
      },
      {
        "name" : "Gypsy Road",
        "album" : "Long Cold Winter",
        "track": 2
      },
      {
        "name" : "Here Comes My Girl",
        "album" : "Greatest Hits",
        "track": 8
      },
      {
        "name" : "I Need To Know",
        "album" : "Greatest Hits",
        "track": 4
      },
      {
        "name" : "I Won't Back Down",
        "album" : "Greatest Hits",
        "track": 12
      },
      {
        "name" : "Into The Great Wide Open",
        "album" : "Greatest Hits",
        "track": 16
      },
      {
        "name" : "Learning to Fly",
        "album" : "Greatest Hits",
        "track": 15
      },
      {
        "name" : "Life By The Drop",
        "album" : "The Sky Is Crying",
        "track": 10
      },
      {
        "name" : "Listen To Her Heart",
        "album" : "Greatest Hits",
        "track": 3
      },
      {
        "name" : "Mary Jane's Last Dance",
        "album" : "Greatest Hits",
        "track": 17
      },
      {
        "name" : "Refugee",
        "album" : "Greatest Hits",
        "track": 5
      },
      {
        "name" : "Rocket Queen",
        "album" : "Appetite For Destruction",
        "track": 12
      },
      {
        "name" : "Runnin' Down a Dream",
        "album" : "Greatest Hits",
        "track": 13
      },
      {
        "name" : "Something In The Air",
        "album" : "Greatest Hits",
        "track": 18
      },
      {
        "name" : "Substitute",
        "album" : "Meaty Beaty Big and Bouncy",
        "track": 13
      },
      {
        "name" : "The Waiting",
        "album" : "Greatest Hits",
        "track": 9
      },
      {
        "name" : "You Got Lucky",
        "album" : "Greatest Hits",
        "track": 10
      }
    ]$json$::jsonb) as t(song)
                        join lateral (
        select t.song->>'name' song, t.song->>'album' album, (t->>'track')::int track
        ) as x on true
)
insert into song(title,details,album_id)
select s.song, jsonb_build_object('trackNumber', s.track) , a.id from songs s inner join album a on s.album = a.title;

insert into song(title, details, album_id)
values ('Standalone Song Without Album', '{}'::jsonb, null);


-- Insert pub/pub_idx
with pubs as (
    select t.* from jsonb_array_elements($json$
        [
          {
            "Name" : "Guitar World",
            "PubType" : "Mag",
            "PubDate" : 1543622400000,
            "Serial" : "10456295",
            "details" : {
              "volume": "39", "issue": "13", "issueName": "Holiday 2018"
            }
          },
          {
            "Name" : "Guitar World",
            "PubType" : "Mag",
            "PubDate" : 1541030400000,
            "Serial" : "10456295",
            "details" : {
              "volume": "39", "issue": "11", "issueName": "November 2018",
              "resources": {
                "COVER_IMAGE": {
                  "mediaType": "image/png",
                  "originalFilename": "gmdb-test-cover-image.png"
                }
              },
              "resourceId": "9d4c6f61-2c7d-49d1-9e36-0e99afef0cf7"
            }
          },
          {
            "Name" : "Guitar World",
            "PubType" : "Mag",
            "PubDate" : 1577750400000,
            "Serial" : "10456295",
            "details" : {
              "volume": "41", "issue": "1", "issueName": "January 2020"
            }
          },
          {
            "Name" : "Guitar World",
            "PubType" : "Mag",
            "PubDate" : 1582934400000,
            "Serial" : "10456295",
            "details" : {
              "volume": "41", "issue": "3", "issueName": "March 2020"
            }
          },
          {
            "Name" : "Guitar World",
            "PubType" : "Mag",
            "PubDate" : 1619827200000,
            "Serial" : "10456295",
            "details" : {
              "volume": "42", "issue": "5", "issueName": "May 2021"
            }
          },
          {
            "Name" : "Guitar For The Practicing Musician",
            "PubType" : "Mag",
            "PubDate" : 596937600000,
            "Serial" : "0738937X",
            "details" : {
              "volume": "6", "issue": "2", "issueName": "December 1988"
            }
          },
          {
            "Name" : "Tom Petty & the Heartbreakers: Greatest Hits",
            "PubType" : "Book",
            "PubDate" : 765158400000,
            "Serial" : "0898987660",
            "details" : { "edition": "First Printing" }
          }
        ]
    $json$) as p(pubs)
                        join lateral (
        select p.pubs->>'Name' name,
               upper(p.pubs->>'PubType')::pub_type pub_type,
               (to_timestamp((p.pubs->>'PubDate')::bigint/1000) at time zone 'UTC')::date pub_date,
               p.pubs->>'Serial' serial,
               p.pubs->'details' details
        ) as t on true
),
     i_pub_idx as (
         insert into pub_idx(name,type,serial_number)
             select distinct name, pub_type, serial from pubs
             returning *
     )
insert into pub(pub_date, pub_idx_id, details)
select p.pub_date, i.id, p.details from i_pub_idx i inner join pubs p on i.serial_number = p.serial;

-- Insert song_artist
with sa as (
    select x.* from jsonb_array_elements($json$[
      {
        "Title" : "American Girl",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "American Girl",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Bombtrack",
        "Album" : "Rage Against The Machine",
        "Name" : "Brad Wilk",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Bombtrack",
        "Album" : "Rage Against The Machine",
        "Name" : "Rage Against The Machine",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Bombtrack",
        "Album" : "Rage Against The Machine",
        "Name" : "Tim Commerford",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Bombtrack",
        "Album" : "Rage Against The Machine",
        "Name" : "Tom Morello",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Bombtrack",
        "Album" : "Rage Against The Machine",
        "Name" : "Zack de la Rocha",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Breakdown",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Breakdown",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Don't Come Around Here No More",
        "Album" : "Greatest Hits",
        "Name" : "David A. Stewart",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Don't Come Around Here No More",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Don't Come Around Here No More",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Don't Do Me Like That",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Don't Do Me Like That",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Even The Losers",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Even The Losers",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Everlong",
        "Album" : "The Colour and the Shape",
        "Name" : "Dave Grohl",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Everlong",
        "Album" : "The Colour and the Shape",
        "Name" : "Foo Fighters",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Free Fallin'",
        "Album" : "Greatest Hits",
        "Name" : "Jeff Lyne",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Free Fallin'",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Free Fallin'",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Gypsy Road",
        "Album" : "Long Cold Winter",
        "Name" : "Cinderella",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Gypsy Road",
        "Album" : "Long Cold Winter",
        "Name" : "Tom Keifer",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Here Comes My Girl",
        "Album" : "Greatest Hits",
        "Name" : "Mike Campbell",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Here Comes My Girl",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Here Comes My Girl",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "I Need To Know",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "I Need To Know",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "I Won't Back Down",
        "Album" : "Greatest Hits",
        "Name" : "Jeff Lyne",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "I Won't Back Down",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "I Won't Back Down",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Into The Great Wide Open",
        "Album" : "Greatest Hits",
        "Name" : "Jeff Lyne",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Into The Great Wide Open",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Into The Great Wide Open",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Learning to Fly",
        "Album" : "Greatest Hits",
        "Name" : "Jeff Lyne",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Learning to Fly",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Learning to Fly",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Life By The Drop",
        "Album" : "The Sky Is Crying",
        "Name" : "Barbara Logan",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Life By The Drop",
        "Album" : "The Sky Is Crying",
        "Name" : "Doyle Bramhall",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Life By The Drop",
        "Album" : "The Sky Is Crying",
        "Name" : "Stevie Ray Vaughan",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Listen To Her Heart",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Listen To Her Heart",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Mary Jane's Last Dance",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Mary Jane's Last Dance",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Refugee",
        "Album" : "Greatest Hits",
        "Name" : "Mike Campbell",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Refugee",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Refugee",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Rocket Queen",
        "Album" : "Appetite For Destruction",
        "Name" : "Duff McKagan",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Rocket Queen",
        "Album" : "Appetite For Destruction",
        "Name" : "Guns N' Roses",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Rocket Queen",
        "Album" : "Appetite For Destruction",
        "Name" : "Izzy Stradlin",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Rocket Queen",
        "Album" : "Appetite For Destruction",
        "Name" : "Slash",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Rocket Queen",
        "Album" : "Appetite For Destruction",
        "Name" : "Steven Adler",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Rocket Queen",
        "Album" : "Appetite For Destruction",
        "Name" : "W. Axl Rose",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Runnin' Down a Dream",
        "Album" : "Greatest Hits",
        "Name" : "Jeff Lyne",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Runnin' Down a Dream",
        "Album" : "Greatest Hits",
        "Name" : "Mike Campbell",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Runnin' Down a Dream",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Runnin' Down a Dream",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Something In The Air",
        "Album" : "Greatest Hits",
        "Name" : "John Keene",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Something In The Air",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "Substitute",
        "Album" : "Meaty Beaty Big and Bouncy",
        "Name" : "Pete Townshend",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "Substitute",
        "Album" : "Meaty Beaty Big and Bouncy",
        "Name" : "The Who",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "The Waiting",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "The Waiting",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      },
      {
        "Title" : "You Got Lucky",
        "Album" : "Greatest Hits",
        "Name" : "Mike Campbell",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "You Got Lucky",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty",
        "Roles" : ["MUSIC_BY","WORDS_BY"]
      },
      {
        "Title" : "You Got Lucky",
        "Album" : "Greatest Hits",
        "Name" : "Tom Petty & the Heartbreakers",
        "Roles" : ["PERFORMED_BY"]
      }
    ]$json$::jsonb) as t(sa)
                        join lateral (
        select
            t.sa->>'Name' artist,
            t.sa->>'Album' album,
            t.sa->>'Title' song,
            t.sa->'Roles' roles
        ) as x on true
)
insert into song_artist(song_id, artist_id, roles)
select s.id, a.id, sa.roles from sa
                                     inner join artist a on sa.artist = a.name
                                     inner join album al on sa.album = al.title
                                     inner join song s on sa.song = s.title and s.album_id = al.id;


-- Insert transcriptions
with d as (
    select x.* from jsonb_array_elements($json$[
      {
        "Name" : "Rocket Queen",
        "Album" : "Appetite For Destruction",
        "Serial" : "10456295",
        "PubDate" : 1541030400.0000,
        "PubDetails" : {
            "volume": "39", "issue": "11", "issueName": "November 2018",
            "resources": {
                "COVER_IMAGE": {
                    "mediaType": "image/png",
                    "originalFilename": "gmdb-test-cover-image.png"
                }
            },
            "resourceId": "9d4c6f61-2c7d-49d1-9e36-0e99afef0cf7"
        },
        "Details" : {
            "pageNumber": 98,
            "resources": {
                "TRANSCRIPTION": {
                    "mediaType": "application/pdf",
                    "originalFilename": "gmdb-test-transcription.pdf"
                }
            },
            "resourceId": "f8e2c95a-6f45-44cb-8a4f-2e7e33f3df70"
        },
        "Transcribers" : ["Andy Aledort"]
      },
      {
        "Name" : "Bombtrack",
        "Album" : "Rage Against The Machine",
        "Serial" : "10456295",
        "PubDate" : 1541030400.0000,
        "PubDetails" : {
            "volume": "39", "issue": "11", "issueName": "November 2018",
            "resources": {
                "COVER_IMAGE": {
                    "mediaType": "image/png",
                    "originalFilename": "gmdb-test-cover-image.png"
                }
            },
            "resourceId": "9d4c6f61-2c7d-49d1-9e36-0e99afef0cf7"
        },
        "Details" : {"pageNumber": 112},
        "Transcribers" : ["Danny Begelman"]
      },
      {
        "Name" : "Life By The Drop",
        "Album" : "The Sky Is Crying",
        "Serial" : "10456295",
        "PubDate" : 1543622400.0000,
        "PubDetails" : {"volume": "39", "issue": "13", "issueName": "Holiday 2018"},
        "Details" : {"pageNumber": 108},
        "Transcribers" : ["Dave Whitehill"]
      },
      {
        "Name" : "Gypsy Road",
        "Album" : "Long Cold Winter",
        "Serial" : "0738937X",
        "PubDate" : 596937600.0000,
        "PubDetails" : {"volume": "6", "issue": "2", "issueName": "December 1988"},
        "Details" : {"pageNumber": 87},
        "Transcribers" : ["Andy Aledort"]
      },
      {
        "Name" : "American Girl",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 12},
        "Transcribers" : []
      },
      {
        "Name" : "Breakdown",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 18},
        "Transcribers" : []
      },
      {
        "Name" : "Listen To Her Heart",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 22},
        "Transcribers" : []
      },
      {
        "Name" : "I Need To Know",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 26},
        "Transcribers" : []
      },
      {
        "Name" : "Refugee",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 30},
        "Transcribers" : []
      },
      {
        "Name" : "Don't Do Me Like That",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 35},
        "Transcribers" : []
      },
      {
        "Name" : "Even The Losers",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 38},
        "Transcribers" : []
      },
      {
        "Name" : "The Waiting",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 48},
        "Transcribers" : []
      },
      {
        "Name" : "You Got Lucky",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 54},
        "Transcribers" : []
      },
      {
        "Name" : "Don't Come Around Here No More",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 58},
        "Transcribers" : []
      },
      {
        "Name" : "I Won't Back Down",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 63},
        "Transcribers" : []
      },
      {
        "Name" : "Runnin' Down a Dream",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 66},
        "Transcribers" : []
      },
      {
        "Name" : "Free Fallin'",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 73},
        "Transcribers" : []
      },
      {
        "Name" : "Learning to Fly",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 76},
        "Transcribers" : []
      },
      {
        "Name" : "Into The Great Wide Open",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 82},
        "Transcribers" : []
      },
      {
        "Name" : "Mary Jane's Last Dance",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 87},
        "Transcribers" : []
      },
      {
        "Name" : "Something In The Air",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 93},
        "Transcribers" : []
      },
      {
        "Name" : "Here Comes My Girl",
        "Album" : "Greatest Hits",
        "Serial" : "0898987660",
        "PubDate" : 765158400.0000,
        "PubDetails" : {"edition": "First Printing"},
        "Details" : {"pageNumber": 44},
        "Transcribers" : []
      },
      {
        "Name" : "Substitute",
        "Album" : "Meaty Beaty Big and Bouncy",
        "Serial" : "10456295",
        "PubDate" : 1619827200.0000,
        "PubDetails" : {"volume": "42", "issue": "5", "issueName": "May 2021"},
        "Details" : {"pageNumber": 94},
        "Transcribers" : ["Jeff Perrin"]
      },
      {
        "Name" : "Everlong",
        "Album" : "The Colour and the Shape",
        "Serial" : "10456295",
        "PubDate" : 1619827200.0000,
        "PubDetails" : {"volume": "42", "issue": "5", "issueName": "May 2021"},
        "Details" : {"pageNumber": 100},
        "Transcribers" : ["Jeff Perrin"]
      }]$json$::jsonb) as t(i)
                        join lateral (
        select
            t.i->>'Name' song,
            t.i->>'Album' album,
            t.i->>'Serial' serial_number,
            (to_timestamp((t.i->>'PubDate')::decimal) at time zone 'UTC')::date pub_date,
            t.i->'PubDetails' pub_details,
            t.i->'Details' details,
            t.i->'Transcribers' transcribers
        ) as x on true
),
     src as (
         select
             s.id song_id,
             p.id pub_id,
             d.details,
             d.transcribers
         from d
                  inner join pub_idx pi on d.serial_number = pi.serial_number
                  inner join pub p on pi.id = p.pub_idx_id and d.pub_date = p.pub_date and d.pub_details <@ p.details
                  inner join album a on d.album = a.title
                  inner join song s on d.song = s.title and a.id = s.album_id
     ),
     i_scrb as (
         insert into transcriber(name)
             select distinct jsonb_array_elements_text(d.transcribers) name from d
             on conflict (name) do update set name = excluded.name
             returning *
     ),
     i_tran as (
         insert into transcription(song_id, pub_id, details)
             select
                 s.song_id,
                 s.pub_id,
                 s.details
             from src s
             on conflict (song_id, pub_id) do update set song_id = excluded.song_id, pub_id = excluded.pub_id
             returning *
     )
insert into transcription_transcriber(transcription_id, transcriber_id)
select t.id transcription_id, i_scrb.id transcriber_id
from src s
         inner join i_tran t on s.song_id = t.song_id and s.pub_id = t.pub_id
         join lateral (select jsonb_array_elements_text(s.transcribers) as scrb) on true
         inner join i_scrb on scrb = i_scrb.name
on conflict(transcription_id, transcriber_id) do nothing;
