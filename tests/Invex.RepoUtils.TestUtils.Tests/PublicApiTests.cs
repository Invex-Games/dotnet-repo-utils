namespace Invex.RepoUtils.TestUtils.Tests;

[TestFixture]
public class PublicApiTests
{
    [Test]
    public async Task VerifyPublicApiSurface() =>
        await VerifyJson(PublicApiSurfaceTestUtil.GetPublicApiSurface(typeof(PublicApiSurfaceTestUtil).Assembly));
}
