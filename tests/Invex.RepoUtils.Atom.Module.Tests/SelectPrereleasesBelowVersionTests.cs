using Invex.RepoUtils.Atom.Module.Helpers;
using Shouldly;

namespace Invex.RepoUtils.Atom.Module.Tests;

[TestFixture]
public class SelectPrereleasesBelowVersionTests
{
    private static IReadOnlyList<SemVer> Select(string version, params string[] published) =>
        INugetPackageUnlistHelper.SelectPrereleasesBelowVersion(SemVer.Parse(version), published.Select(SemVer.Parse));

    [Test]
    public void SelectsPrereleasesBelowTheGivenVersion()
    {
        var result = Select("2.0.0", "0.1.0-alpha.1", "1.5.0-rc.1", "2.0.0-rc.1");

        result
            .Select(version => version.ToString())
            .ShouldBe(["0.1.0-alpha.1", "1.5.0-rc.1", "2.0.0-rc.1"]);
    }

    [Test]
    public void SelectsAcrossPatchAndMinorBoundaries()
    {
        var result = Select("1.2.0", "1.1.0-rc.1", "1.2.0-beta.1", "1.2.0-rc.2");

        result
            .Select(version => version.ToString())
            .ShouldBe(["1.1.0-rc.1", "1.2.0-beta.1", "1.2.0-rc.2"]);
    }

    [Test]
    public void DoesNotSelectStableVersions()
    {
        var result = Select("2.0.0", "1.0.0", "1.5.0", "0.9.0");

        result.ShouldBeEmpty();
    }

    [Test]
    public void DoesNotSelectPrereleasesAtOrAboveTheGivenVersion()
    {
        var result = Select("1.2.0", "1.2.0", "1.2.1-rc.1", "2.0.0-rc.1");

        result.ShouldBeEmpty();
    }

    [Test]
    public void ReturnsResultsInAscendingPrecedenceOrder()
    {
        var result = Select("2.0.0", "1.2.0-rc.1", "0.1.0-alpha.1", "1.1.0-beta.2");

        result
            .Select(version => version.ToString())
            .ShouldBe(["0.1.0-alpha.1", "1.1.0-beta.2", "1.2.0-rc.1"]);
    }

    [Test]
    public void ReturnsEmptyWhenNoVersionsQualify()
    {
        var result = Select("1.0.0", "2.0.0-rc.1", "3.0.0-beta.1");

        result.ShouldBeEmpty();
    }
}


