﻿using System;
using System.IO;
using System.Linq;
using Squirrel;
using Squirrel.Tests.TestHelpers;
using Xunit;

namespace Squirrel.Tests.Core
{
    public class ReleaseEntryTests
    {
        [Theory]
        [InlineData(@"94689fede03fed7ab59c24337673a27837f0c3ec MyCoolApp-1.0.nupkg 1004502", "MyCoolApp-1.0.nupkg", 1004502, null)]
        [InlineData(@"3a2eadd15dd984e4559f2b4d790ec8badaeb6a39   MyCoolApp-1.1.nupkg   1040561", "MyCoolApp-1.1.nupkg", 1040561, null)]
        [InlineData(@"14db31d2647c6d2284882a2e101924a9c409ee67  MyCoolApp-1.1.nupkg.delta  80396", "MyCoolApp-1.1.nupkg.delta", 80396, null)]
        [InlineData(@"0000000000000000000000000000000000000000  http://test.org/Folder/MyCoolApp-1.2.nupkg  2569", "MyCoolApp-1.2.nupkg", 2569, "http://test.org/Folder/")]
        [InlineData(@"0000000000000000000000000000000000000000  https://www.test.org/Folder/MyCoolApp-1.2-delta.nupkg  1231953", "MyCoolApp-1.2-delta.nupkg", 1231953, "https://www.test.org/Folder/")]
        [InlineData(@"94689fede03fed7ab59c24337673a27837f0c3ec MyCoolApp-1.0.nupkg 1004502 20141112131415", "MyCoolApp-1.0.nupkg", 1004502, null)]
        [InlineData(@"3a2eadd15dd984e4559f2b4d790ec8badaeb6a39   MyCoolApp-1.1.nupkg   1040561 20141112131415", "MyCoolApp-1.1.nupkg", 1040561, null)]
        [InlineData(@"14db31d2647c6d2284882a2e101924a9c409ee67  MyCoolApp-1.1.nupkg.delta  80396 20141112131415", "MyCoolApp-1.1.nupkg.delta", 80396, null)]
        [InlineData(@"0000000000000000000000000000000000000000  http://test.org/Folder/MyCoolApp-1.2.nupkg  2569 20141112131415", "MyCoolApp-1.2.nupkg", 2569, "http://test.org/Folder/")]
        [InlineData(@"0000000000000000000000000000000000000000  https://www.test.org/Folder/MyCoolApp-1.2-delta.nupkg  1231953 20141112131415", "MyCoolApp-1.2-delta.nupkg", 1231953, "https://www.test.org/Folder/")]
        [InlineData(@"F3AE4F750A440C1767AE58FA3F5FBD70282B876E MyProduct-2.2.0-delta.nupkg 26705 20141112131415", "MyProduct-2.2.0-delta.nupkg", 26705, null)]
        public void ParseValidReleaseEntryLines(string releaseEntry, string fileName, long fileSize, string baseUrl)
        {
            var fixture = ReleaseEntry.ParseReleaseEntry(releaseEntry);
            Assert.Equal(fileName, fixture.Filename);
            Assert.Equal(fileSize, fixture.Filesize);
            Assert.Equal(baseUrl, fixture.BaseUrl);

            // Workaround since we cannot pass date/time via attributes
            if (fixture.ReleaseDate != DateTime.MinValue)
            {
                Assert.Equal(new DateTime(2014, 11, 12, 13, 14, 15), fixture.ReleaseDate);    
            }
        }

        [Theory]
        [InlineData(@"0000000000000000000000000000000000000000  file:/C/Folder/MyCoolApp-0.0.nupkg  0")]
        [InlineData(@"0000000000000000000000000000000000000000  C:\Folder\MyCoolApp-0.0.nupkg  0")]
        [InlineData(@"0000000000000000000000000000000000000000  ..\OtherFolder\MyCoolApp-0.0.nupkg  0")]
        [InlineData(@"0000000000000000000000000000000000000000  ../OtherFolder/MyCoolApp-0.0.nupkg  0")]
        [InlineData(@"0000000000000000000000000000000000000000  \\Somewhere\NetworkShare\MyCoolApp-0.0.nupkg.delta  0")]
        public void ParseThrowsWhenInvalidReleaseEntryLines(string releaseEntry)
        {
            Assert.Throws<Exception>(() => ReleaseEntry.ParseReleaseEntry(releaseEntry));
        }

        [Theory]
        [InlineData(@"0000000000000000000000000000000000000000 file.nupkg 0")]
        [InlineData(@"0000000000000000000000000000000000000000 http://path/file.nupkg 0")]
        public void EntryAsStringMatchesParsedInput(string releaseEntry)
        {
            var fixture = ReleaseEntry.ParseReleaseEntry(releaseEntry);
            Assert.Equal(releaseEntry, fixture.EntryAsString);
        }

        [Theory]
        [InlineData("Squirrel.Core.1.0.0.0.nupkg", 4457, "75255cfd229a1ed1447abe1104f5635e69975d30")]
        [InlineData("Squirrel.Core.1.1.0.0.nupkg", 15830, "9baf1dbacb09940086c8c62d9a9dbe69fe1f7593")]
        public void GenerateFromFileTest(string name, long size, string sha1)
        {
            var path = IntegrationTestHelper.GetPath("fixtures", name);

            using (var f = File.OpenRead(path))
            {
                var fixture = ReleaseEntry.GenerateFromFile(f, "dontcare", "dontcare");
                Assert.Equal(size, fixture.Filesize);
                Assert.Equal(sha1, fixture.SHA1.ToLowerInvariant());
            }
        }

        [Theory]
        [InlineData("94689fede03fed7ab59c24337673a27837f0c3ec  MyCoolApp-1.0.nupkg  1004502", 1, 0)]
        [InlineData("3a2eadd15dd984e4559f2b4d790ec8badaeb6a39  MyCoolApp-1.1.nupkg  1040561", 1, 1)]
        [InlineData("14db31d2647c6d2284882a2e101924a9c409ee67  MyCoolApp-1.1-delta.nupkg  80396", 1, 1)]
        public void ParseVersionTest(string releaseEntry, int expectedMajor, int expectedMinor)
        {
            var fixture = ReleaseEntry.ParseReleaseEntry(releaseEntry);

            var version = fixture.Version.ClassicVersion;

            Assert.Equal(expectedMajor, version.Major);
            Assert.Equal(expectedMinor, version.Minor);
        }

        [Fact]
        public void CanParseGeneratedReleaseEntryAsString()
        {
            var path = IntegrationTestHelper.GetPath("fixtures", "Squirrel.Core.1.1.0.0.nupkg");
            var entryAsString = ReleaseEntry.GenerateFromFile(path).EntryAsString;
            ReleaseEntry.ParseReleaseEntry(entryAsString);
        }

        [Fact]
        public void InvalidReleaseNotesThrowsException()
        {
            var path = IntegrationTestHelper.GetPath("fixtures", "Squirrel.Core.1.0.0.0.nupkg");
            var fixture = ReleaseEntry.GenerateFromFile(path);
            Assert.Throws<Exception>(() => fixture.GetReleaseNotes(IntegrationTestHelper.GetPath("fixtures")));
        }

        [Fact]
        public void GetLatestReleaseWithNullCollectionReturnsNull()
        {
            Assert.Null(ReleaseEntry.GetPreviousRelease(
                null, null, null));
        }

        [Fact]
        public void GetLatestReleaseWithEmptyCollectionReturnsNull()
        {
            Assert.Null(ReleaseEntry.GetPreviousRelease(
                Enumerable.Empty<ReleaseEntry>(), null, null));
        }

        [Fact]
        public void WhenCurrentReleaseMatchesLastReleaseReturnNull()
        {
            var package = new ReleasePackage("Espera-1.7.6-beta.nupkg");

            var releaseEntries = new[] {
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.7.6-beta.nupkg"))
            };
            Assert.Null(ReleaseEntry.GetPreviousRelease(
                releaseEntries, package, @"C:\temp\somefolder"));
        }

        [Fact]
        public void WhenMultipleReleaseMatchesReturnEarlierResult()
        {
            var expected = "1.7.5-beta";
            var package = new ReleasePackage("Espera-1.7.6-beta.nupkg");

            var releaseEntries = new[] {
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.7.6-beta.nupkg")),
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.7.5-beta.nupkg"))
            };

            var actual = ReleaseEntry.GetPreviousRelease(
                releaseEntries,
                package,
                @"C:\temp\");

            Assert.Equal(expected, actual.Version.Version);
        }

        [Fact]
        public void WhenMultipleReleasesFoundReturnPreviousVersion()
        {
            var expected = "1.7.6-beta";
            var input = new ReleasePackage("Espera-1.7.7-beta.nupkg");

            var releaseEntries = new[] {
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.7.6-beta.nupkg")),
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.7.5-beta.nupkg"))
            };

            var actual = ReleaseEntry.GetPreviousRelease(
                releaseEntries,
                input,
                @"C:\temp\");

            Assert.Equal(expected, actual.Version.Version);
        }

        [Fact]
        public void WhenMultipleReleasesFoundInOtherOrderReturnPreviousVersion()
        {
            var expected = "1.7.6-beta";
            var input = new ReleasePackage("Espera-1.7.7-beta.nupkg");

            var releaseEntries = new[] {
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.7.5-beta.nupkg")),
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.7.6-beta.nupkg"))
            };

            var actual = ReleaseEntry.GetPreviousRelease(
                releaseEntries,
                input,
                @"C:\temp\");

            Assert.Equal(expected, actual.Version.Version);
        }

        [Fact]
        public void WhenReleasesAreOutOfOrderSortByVersion()
        {
            var path = Path.GetTempFileName();
            var firstVersion = new SemVersion("1.0.0");
            var secondVersion = new SemVersion("1.1.0");
            var thirdVersion = new SemVersion("1.2.0");

            var releaseEntries = new[] {
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.2.0-delta.nupkg")),
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.1.0-delta.nupkg")),
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.1.0-full.nupkg")),
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.2.0-full.nupkg")),
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.0.0-full.nupkg"))
            };

            ReleaseEntry.WriteReleaseFile(releaseEntries, path);

            var releases = ReleaseEntry.ParseReleaseFile(File.ReadAllText(path)).ToArray();

            Assert.Equal(firstVersion, new SemVersion(releases[0].Version.Version));
            Assert.Equal(secondVersion, new SemVersion(releases[1].Version.Version));
            Assert.Equal(true, releases[1].IsDelta);
            Assert.Equal(secondVersion, new SemVersion(releases[2].Version.Version));
            Assert.Equal(false, releases[2].IsDelta);
            Assert.Equal(thirdVersion, new SemVersion(releases[3].Version.Version));
            Assert.Equal(true, releases[3].IsDelta);
            Assert.Equal(thirdVersion, new SemVersion(releases[4].Version.Version));
            Assert.Equal(false, releases[4].IsDelta);
        }

        [Fact]
        public void WhenReleasesAreOutOfOrderSortByVersionSemVer()
        {
            var path = Path.GetTempFileName();
            var expectedVersions = new[]
            {
                new SemVersion("1.0.0"),
                new SemVersion("1.1.0"),
                new SemVersion("1.2.0-unstable002"),
                new SemVersion("1.2.0")
            };

            var releaseEntries = new[] {
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.2.0-delta.nupkg")),
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.2.0-unstable002-delta.nupkg")),
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.1.0-full.nupkg")),
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.1.0-delta.nupkg")),
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.2.0-full.nupkg")),
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.0.0-full.nupkg")),
                ReleaseEntry.ParseReleaseEntry(MockReleaseEntry("Espera-1.2.0-unstable002-full.nupkg"))
            };

            ReleaseEntry.WriteReleaseFile(releaseEntries, path);

            var releases = ReleaseEntry.ParseReleaseFile(File.ReadAllText(path)).ToArray();

            Assert.Equal(expectedVersions[0], new SemVersion(releases[0].Version.Version));
            for (int i = 1; i < releaseEntries.Length; i = i + 2)
            {
                var expectedReleaseIndex = i / 2 + 1;

                Assert.Equal(true, releases[i].IsDelta);
                Assert.Equal(expectedVersions[expectedReleaseIndex], new SemVersion(releases[i + 1].Version.Version));
            }
        }

        [Fact]
        public void ParseReleaseFileShouldReturnNothingForBlankFiles()
        {
            Assert.True(ReleaseEntry.ParseReleaseFile(string.Empty).Count() == 0);
            Assert.True(ReleaseEntry.ParseReleaseFile(null).Count() == 0);
        }

        static string MockReleaseEntry(string name)
        {
            return string.Format("94689fede03fed7ab59c24337673a27837f0c3ec  {0}  1004502", name);
        }
    }
}
