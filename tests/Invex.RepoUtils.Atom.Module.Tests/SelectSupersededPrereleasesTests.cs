using Invex.RepoUtils.Atom.Module.Helpers;
using Shouldly;

namespace Invex.RepoUtils.Atom.Module.Tests;

[TestFixture]
public class SelectSupersededPrereleasesTests
{
    private static IReadOnlyList<SemVer> Select(string current, params string[] published) =>
        INugetPackageUnlistHelper.SelectSupersededPrereleases(SemVer.Parse(current), published.Select(SemVer.Parse));

    [Test]
    public void StableVersion_SelectsAllPrereleasesOfSameCore()
    {
        var result = Select("1.1.0", "1.1.0-beta.1", "1.1.0-rc.1", "1.1.0");

        result
            .Select(version => version.ToString())
            .ShouldBe(["1.1.0-beta.1", "1.1.0-rc.1"]);
    }

    [Test]
    public void StableVersion_DoesNotSelectPrereleasesOfADifferentCore()
    {
        var result = Select("1.1.0", "1.0.1-rc.1", "1.2.0-rc.1");

        result.ShouldBeEmpty();
    }

    [Test]
    public void PrereleaseVersion_SelectsOnlyEarlierPrereleasesOfSameCore()
    {
        var result = Select("1.1.0-rc.2", "1.1.0-beta.1", "1.1.0-rc.1", "1.1.0-rc.2", "1.1.0-rc.3", "1.1.0");

        result
            .Select(version => version.ToString())
            .ShouldBe(["1.1.0-beta.1", "1.1.0-rc.1"]);
    }

    [Test]
    public void DoesNotSelectTheCurrentVersionItself()
    {
        var result = Select("1.1.0-rc.1", "1.1.0-rc.1");

        result.ShouldBeEmpty();
    }

    [Test]
    public void DoesNotSelectHigherVersionsEvenWhenPrerelease()
    {
        var result = Select("1.1.0", "1.1.1-rc.1", "2.0.0-rc.1");

        result.ShouldBeEmpty();
    }

    [Test]
    public void IgnoresStableVersionsOfTheSameCore()
    {
        var result = Select("1.1.0", "1.1.0-rc.1", "1.0.0", "1.1.0");

        result
            .Select(version => version.ToString())
            .ShouldBe(["1.1.0-rc.1"]);
    }

    [Test]
    public void ReturnsResultsInAscendingPrecedenceOrder()
    {
        var result = Select("1.1.0", "1.1.0-rc.1", "1.1.0-beta.2", "1.1.0-alpha.1");

        result
            .Select(version => version.ToString())
            .ShouldBe(["1.1.0-alpha.1", "1.1.0-beta.2", "1.1.0-rc.1"]);
    }
}
