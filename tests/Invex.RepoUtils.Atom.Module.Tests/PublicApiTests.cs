using Invex.RepoUtils.Atom.Module.Targets;
using Invex.RepoUtils.TestUtils;

namespace Invex.RepoUtils.Atom.Module.Tests;

[TestFixture]
public class PublicApiTests
{
    [Test]
    public async Task VerifyPublicApiSurface() =>
        await VerifyJson(PublicApiSurfaceTestUtil.GetPublicApiSurface(typeof(IApproveDependabotPr).Assembly));
}
