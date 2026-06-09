namespace Invex.RepoUtils.Atom.Module.Helpers;

/// <summary>
/// Provides functionality for discovering and unlisting prerelease packages that have been
/// superseded by a newly published version. Versions are discovered through the NuGet flat-container
/// API and unlisted via the feed's HTTP <c>DELETE</c> endpoint, with a summary written to the Atom
/// build report.
/// </summary>
[PublicAPI]
public interface INugetPackageUnlistHelper : IReportsHelper
{
    /// <summary>
    /// The NuGet flat-container resource type advertised in a feed's service index.
    /// </summary>
    const string FlatContainerResourceType = "PackageBaseAddress/3.0.0";

    /// <summary>
    /// The NuGet package publish resource type advertised in a feed's service index. The same base
    /// address is used to unlist packages via an HTTP <c>DELETE</c>.
    /// </summary>
    const string PublishResourceType = "PackagePublish/2.0.0";

    /// <summary>
    /// The maximum number of attempts (including the initial attempt) made for each HTTP request
    /// before giving up.
    /// </summary>
    const int MaxHttpAttempts = 3;

    /// <summary>
    /// Selects the published versions that are superseded by <paramref name="currentVersion"/>: every
    /// version that shares the same <see cref="SemVer.Prefix"/> (core <c>MAJOR.MINOR.PATCH</c>), is a
    /// prerelease, and has lower SemVer precedence than the current version.
    /// </summary>
    /// <param name="currentVersion">The version that was just published.</param>
    /// <param name="publishedVersions">All versions currently published for the package.</param>
    /// <returns>
    /// The superseded prerelease versions in ascending precedence order. A stable
    /// <paramref name="currentVersion"/> matches all prereleases of the same core version, while a
    /// versions of a different
    /// core, equal/higher versions, and the current version itself are never selected.
    /// </returns>
    static IReadOnlyList<SemVer> SelectSupersededPrereleases(SemVer currentVersion, IEnumerable<SemVer> publishedVersions) =>
        publishedVersions
            .Where(version => version.IsPreRelease && version.Prefix == currentVersion.Prefix && version < currentVersion)
            .OrderBy(version => version)
            .ToList();

    /// <summary>
    /// Unlists every prerelease package version that is superseded by <paramref name="currentVersion"/>
    /// for the supplied package ids, and writes a summary of the work performed to the Atom build report.
    /// </summary>
    /// <param name="feedUrl">The service index URL of the NuGet feed (for example, <c>https://api.nuget.org/v3/index.json</c>).</param>
    /// <param name="apiKey">The API key used to authenticate the unlist (<c>DELETE</c>) requests.</param>
    /// <param name="packageIds">The ids of the packages whose superseded prereleases should be unlisted.</param>
    /// <param name="currentVersion">The version that was just published, used as the supersedence baseline.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The per-version results of the unlist attempts.</returns>
    /// <exception cref="StepFailedException">Thrown when one or more versions could not be unlisted after retries.</exception>
    async Task<IReadOnlyList<UnlistResult>> UnlistSupersededPrereleasesForPackages(
        string feedUrl,
        string apiKey,
        IEnumerable<string> packageIds,
        SemVer currentVersion,
        CancellationToken cancellationToken)
    {
        var results = new List<UnlistResult>();

        using var client = new HttpClient();

        // Discover the resources we depend on up front so we can fail fast / skip gracefully when the
        // feed does not support listing versions or unlisting packages.
        var flatContainerBase = await GetServiceResourceAsync(client, feedUrl, FlatContainerResourceType, cancellationToken);
        var publishBase = await GetServiceResourceAsync(client, feedUrl, PublishResourceType, cancellationToken);

        if (flatContainerBase is null || publishBase is null)
        {
            Logger.LogWarning(
                "Feed {Feed} does not expose the required NuGet resources (flat container: {HasFlatContainer}, publish: {HasPublish}); skipping prerelease unlisting.",
                feedUrl,
                flatContainerBase is not null,
                publishBase is not null);

            AddUnlistReport(currentVersion, results);

            return results;
        }

        foreach (var packageId in packageIds)
        {
            var publishedVersions = await GetPublishedVersionsAsync(client, flatContainerBase, packageId, cancellationToken);

            var superseded = SelectSupersededPrereleases(currentVersion, publishedVersions);

            Logger.LogInformation(
                "Found {Count} superseded prerelease version(s) of {PackageId} for published version {Version}.",
                superseded.Count,
                packageId,
                currentVersion);

            foreach (var version in superseded)
                results.Add(await UnlistPackageAsync(client, publishBase, apiKey, packageId, version, cancellationToken));
        }

        AddUnlistReport(currentVersion, results);

        var failures = results.Count(result => result.Outcome is UnlistOutcome.Failed);

        if (failures > 0)
            throw new StepFailedException($"Failed to unlist {failures} superseded prerelease package version(s).");

        return results;
    }

    /// <summary>
    /// Resolves the base address of a resource advertised in a feed's service index.
    /// </summary>
    /// <param name="client">The HTTP client used to query the feed.</param>
    /// <param name="feedUrl">The service index URL of the NuGet feed.</param>
    /// <param name="resourceType">The resource <c>@type</c> to look up.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The resource base address without a trailing slash, or <see langword="null"/> when the resource is not present.</returns>
    private async Task<string?> GetServiceResourceAsync(
        HttpClient client,
        string feedUrl,
        string resourceType,
        CancellationToken cancellationToken)
    {
        using var response = await SendWithRetryAsync(() => client.GetAsync(feedUrl, cancellationToken), cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("Could not read service index from {Feed}: HTTP {StatusCode}.", feedUrl, (int)response.StatusCode);

            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("resources", out var resources))
            return null;

        foreach (var resource in resources.EnumerateArray())
            if (resource.TryGetProperty("@type", out var typeProperty) &&
                typeProperty.GetString() is { } type &&
                type.StartsWith(resourceType, StringComparison.OrdinalIgnoreCase) &&
                resource.TryGetProperty("@id", out var idProperty) &&
                idProperty.GetString() is { } id)
                return id.TrimEnd('/');

        return null;
    }

    /// <summary>
    /// Reads all published versions of a package from the NuGet flat-container API.
    /// </summary>
    /// <param name="client">The HTTP client used to query the feed.</param>
    /// <param name="flatContainerBase">The flat-container base address resolved from the service index.</param>
    /// <param name="packageId">The id of the package to query.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The parsed published versions; an empty list when the package has no published versions.</returns>
    private async Task<IReadOnlyList<SemVer>> GetPublishedVersionsAsync(
        HttpClient client,
        string flatContainerBase,
        string packageId,
        CancellationToken cancellationToken)
    {
        // The flat-container API requires the lower-cased package id in the request path.
        var url = $"{flatContainerBase}/{packageId.ToLowerInvariant()}/index.json";

        using var response = await SendWithRetryAsync(() => client.GetAsync(url, cancellationToken), cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            Logger.LogInformation("No published versions found for {PackageId}.", packageId);

            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            Logger.LogWarning("Could not read versions for {PackageId}: HTTP {StatusCode}.", packageId, (int)response.StatusCode);

            return [];
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var versions = new List<SemVer>();

        if (document.RootElement.TryGetProperty("versions", out var versionsElement))
            foreach (var item in versionsElement.EnumerateArray())
                if (item.GetString() is { } raw && SemVer.TryParse(raw, out var version))
                    versions.Add(version);

        return versions;
    }

    /// <summary>
    /// Unlists a single package version from the feed via an HTTP <c>DELETE</c>.
    /// </summary>
    /// <param name="client">The HTTP client used to issue the request.</param>
    /// <param name="publishBase">The publish base address resolved from the service index.</param>
    /// <param name="apiKey">The API key used to authenticate the request.</param>
    /// <param name="packageId">The id of the package to unlist.</param>
    /// <param name="version">The version to unlist.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The result of the unlist attempt.</returns>
    private async Task<UnlistResult> UnlistPackageAsync(
        HttpClient client,
        string publishBase,
        string apiKey,
        string packageId,
        SemVer version,
        CancellationToken cancellationToken)
    {
        var url = $"{publishBase}/{packageId}/{version}";

        try
        {
            using var response = await SendWithRetryAsync(
                () =>
                {
                    // A fresh request is created on every attempt because an HttpRequestMessage cannot be resent.
                    var request = new HttpRequestMessage(HttpMethod.Delete, url);
                    request.Headers.Add("X-NuGet-ApiKey", apiKey);

                    return client.SendAsync(request, cancellationToken);
                },
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.NotFound)
            {
                Logger.LogInformation(
                    "Package {PackageId} {Version} was not found (already unlisted or removed); skipping.",
                    packageId,
                    version);

                return new(packageId, version, UnlistOutcome.Skipped, "Not found (already unlisted).");
            }

            if (!response.IsSuccessStatusCode)
            {
                var reason = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}".Trim();

                Logger.LogError("Failed to unlist {PackageId} {Version}: {Reason}.", packageId, version, reason);

                return new(packageId, version, UnlistOutcome.Failed, reason);
            }

            Logger.LogInformation("Unlisted {PackageId} {Version}.", packageId, version);

            return new(packageId, version, UnlistOutcome.Unlisted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.LogError(exception, "Error unlisting {PackageId} {Version}.", packageId, version);

            return new(packageId, version, UnlistOutcome.Failed, exception.Message);
        }
    }

    /// <summary>
    /// Sends an HTTP request, retrying transient failures with exponential backoff up to
    /// <see cref="MaxHttpAttempts"/> attempts.
    /// </summary>
    /// <param name="send">A factory that produces the request task; it is invoked once per attempt.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The final HTTP response.</returns>
    private async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1;; attempt++)
        {
            try
            {
                var response = await send();

                if (attempt < MaxHttpAttempts && IsTransient(response.StatusCode))
                {
                    Logger.LogWarning(
                        "Transient HTTP {StatusCode} on attempt {Attempt}/{MaxAttempts}; retrying.",
                        (int)response.StatusCode,
                        attempt,
                        MaxHttpAttempts);

                    response.Dispose();

                    await DelayForAttemptAsync(attempt, cancellationToken);

                    continue;
                }

                return response;
            }
            catch (HttpRequestException exception) when (attempt < MaxHttpAttempts)
            {
                Logger.LogWarning(
                    exception,
                    "HTTP request failed on attempt {Attempt}/{MaxAttempts}; retrying.",
                    attempt,
                    MaxHttpAttempts);

                await DelayForAttemptAsync(attempt, cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < MaxHttpAttempts)
            {
                Logger.LogWarning(
                    "HTTP request timed out on attempt {Attempt}/{MaxAttempts}; retrying.",
                    attempt,
                    MaxHttpAttempts);

                await DelayForAttemptAsync(attempt, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Determines whether an HTTP status code represents a transient failure worth retrying.
    /// </summary>
    /// <param name="statusCode">The status code to classify.</param>
    /// <returns><see langword="true"/> when the status code is transient; otherwise <see langword="false"/>.</returns>
    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;

    /// <summary>
    /// Delays for an exponentially increasing duration based on the attempt number.
    /// </summary>
    /// <param name="attempt">The one-based attempt number that just failed.</param>
    /// <param name="cancellationToken">A token used to cancel the delay.</param>
    private static Task DelayForAttemptAsync(int attempt, CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)), cancellationToken);

    /// <summary>
    /// Adds a summary of the unlisting work to the Atom build report.
    /// </summary>
    /// <param name="currentVersion">The version that was published.</param>
    /// <param name="results">The per-version unlist results to report.</param>
    private void AddUnlistReport(SemVer currentVersion, IReadOnlyList<UnlistResult> results)
    {
        if (results.Count is 0)
        {
            AddReportData(new TextReportData(
                $"No superseded prerelease packages were found to unlist for published version {currentVersion}.")
            {
                Title = "Superseded Prerelease Unlisting",
            });

            return;
        }

        var rows = new List<IReadOnlyList<string>>();

        foreach (var result in results)
            rows.Add([result.PackageId, result.Version.ToString(), result.Outcome.ToString(), result.Message ?? string.Empty]);

        AddReportData(new TableReportData(rows)
        {
            Title = $"Superseded Prerelease Unlisting (published {currentVersion})",
            Header = ["Package", "Version", "Outcome", "Details"],
            ColumnAlignments = [ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left, ColumnAlignment.Left],
        });
    }
}

