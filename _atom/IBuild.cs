using Invex.RepoUtils.Atom.Module.Helpers;

namespace Atom;

[BuildDefinition]
[GenerateEntryPoint]
[GenerateSolutionModel]
internal interface IBuild : IWorkflowBuildDefinition,
    IGithubWorkflows,
    IGitVersion,
    IDotnetPackHelper,
    IDotnetTestHelper,
    INugetHelper,
    IGithubReleaseHelper,
    IApproveDependabotPr,
    ICheckPrForBreakingChanges,
    IDocFxHelper
{
    [ParamDefinition("test-framework", "Test framework to use for unit tests")]
    string TestFramework => GetParam(() => TestFramework, "net10.0");

    [ParamDefinition("nuget-push-feed", "The Nuget feed to push to.")]
    string NugetFeed => GetParam(() => NugetFeed, "https://api.nuget.org/v3/index.json");

    [SecretDefinition("nuget-push-api-key", "The API key to use to push to Nuget.")]
    string NugetApiKey => GetParam(() => NugetApiKey)!;

    static readonly string[] ProjectsToPack =
    [
        Projects.Invex_RepoUtils_Atom_Module.Name,
        Projects.Invex_RepoUtils_PublicApiAnalyzers.Name,
        Projects.Invex_RepoUtils_TestUtils.Name,
    ];

    static readonly string[] ProjectsToTest =
    [
        Projects.Invex_RepoUtils_Atom_Module_Tests.Name,
        Projects.Invex_RepoUtils_PublicApiAnalyzers_Tests.Name,
        Projects.Invex_RepoUtils_TestUtils_Tests.Name,
    ];

    static readonly string[] TestFrameworkNames =
    [
        WorkflowLabels.Dotnet.Framework.Net_8_0,
        WorkflowLabels.Dotnet.Framework.Net_9_0,
        WorkflowLabels.Dotnet.Framework.Net_10_0,
    ];

    IEnumerable<RootedPath> ICheckPrForBreakingChanges.BreakingChangeFilesToCheck =>
        RootedFileSystem
            .Directory
            .GetFiles(RootedFileSystem.AtomRootDirectory / "tests", "*.verified.txt", SearchOption.AllDirectories)
            .Select(RootedFileSystem.CreateRootedPath);

    IReadOnlyList<IBuildOption> IBuildDefinition.Options =>
    [
        BuildOptions.GitVersion.ProvideBuildId,
        BuildOptions.GitVersion.ProvideBuildVersion,
        BuildOptions.Steps.SetupDotnet.Dotnet100X(),
    ];

    Target PackProjects =>
        t => t
            .DescribedAs("Packs the projects")
            .ProducesArtifacts(ProjectsToPack)
            .Executes(async cancellationToken =>
            {
                foreach (var packageProject in ProjectsToPack)
                    await DotnetPackAndStage(packageProject, cancellationToken: cancellationToken);
            });

    Target TestProjects =>
        t => t
            .DescribedAs("Tests the projects")
            .RequiresParam(nameof(TestFramework))
            .ProducesArtifacts(ProjectsToTest)
            .Executes(async cancellationToken =>
            {
                var exitCode = 0;

                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var project in ProjectsToTest)
                    exitCode += await DotnetTestAndStage(project,
                        new()
                        {
                            IncludeCoverage = false,
                            TestOptions = new()
                            {
                                Framework = TestFramework,
                            },
                        },
                        cancellationToken);

                if (exitCode != 0)
                    throw new StepFailedException("One or more unit tests failed");
            });

    Target PushToNuget =>
        t => t
            .DescribedAs("Pushes the packages to Nuget")
            .RequiresParam(nameof(NugetFeed))
            .RequiresParam(nameof(NugetApiKey))
            .ConsumesVariable(nameof(SetupBuildInfo), nameof(BuildId))
            .ConsumesArtifacts(nameof(PackProjects), ProjectsToPack)
            .DependsOn(nameof(TestProjects))
            .Executes(async cancellationToken =>
            {
                foreach (var project in ProjectsToPack)
                    await PushProject(project, NugetFeed, NugetApiKey, cancellationToken: cancellationToken);
            });

    Target PushToRelease =>
        d => d
            .DescribedAs("Pushes the packages to the release feed.")
            .RequiresParam(nameof(GithubToken))
            .ConsumesVariable(nameof(SetupBuildInfo), nameof(BuildVersion))
            .ConsumesVariable(nameof(SetupBuildInfo), nameof(BuildId))
            .ConsumesArtifacts(nameof(PackProjects), ProjectsToPack)
            .DependsOn(nameof(TestProjects))
            .Executes(async () =>
            {
                foreach (var artifact in ProjectsToPack)
                    await UploadArtifactToRelease(artifact, $"v{BuildVersion}");
            });

    Target BuildDocs =>
        t => t.Executes(cancellationToken =>
            BuildDocFxDocs([Projects.Invex_RepoUtils_PublicApiAnalyzers.Path(RootedFileSystem)], cancellationToken));

    Target ServeDocs =>
        t => t
            .DependsOn(nameof(BuildDocs))
            .Executes(ServeDocFxDocs);

    Target PublishDocs =>
        t => t
            .RequiresParam(nameof(GithubToken))
            .DependsOn(nameof(BuildDocs))
            .Executes(cancellationToken =>
                PublishDocFxDocsToGithub(GithubToken, GeneratedDocsArtifactName, cancellationToken));

    IReadOnlyList<WorkflowDefinition> IWorkflowBuildDefinition.Workflows =>
    [
        new("Validate")
        {
            Triggers = [WorkflowTriggers.Manual, WorkflowTriggers.PullIntoMain],
            Targets =
            [
                new(nameof(SetupBuildInfo)),
                new(nameof(PackProjects))
                {
                    Options = [BuildOptions.Target.SuppressArtifactPublishing],
                },
                new(nameof(TestProjects))
                {
                    MatrixDimensions =
                    [
                        new(nameof(TestFramework))
                        {
                            Values = TestFrameworkNames,
                        },
                    ],
                    Options =
                    [
                        BuildOptions.Target.SuppressArtifactPublishing,
                        BuildOptions.Steps.SetupDotnet.Dotnet80X(),
                        BuildOptions.Steps.SetupDotnet.Dotnet90X(),
                    ],
                },
                new(nameof(CheckPrForBreakingChanges))
                {
                    Options =
                    [
                        BuildOptions.Target.SuppressArtifactPublishing,
                        BuildOptions.Inject.Secret(nameof(GithubToken)),
                        BuildOptions.Github.TokenPermissions.Set(new Permissions.Exact(new()
                        {
                            IdTokens = PermissionsLevel.Write,
                            Contents = PermissionsLevel.Write,
                            PullRequests = PermissionsLevel.Write,
                            Checks = PermissionsLevel.Write,
                        })),
                        BuildOptions.Inject.Github.PullRequestNumber,
                        BuildOptions.Target.RunIfWorkflowCondition(
                            TextExpressions.Github.GithubEventName.EqualToString("pull_request")),
                    ],
                },
            ],
            Types = [WorkflowTypes.Github.Action],
        },
        new("Build")
        {
            Triggers =
            [
                WorkflowTriggers.Manual,
                new GitPushTrigger
                {
                    IncludedBranches = ["main", "feature/**", "patch/**"],
                },
                new GithubTrigger(new On.Release([On.Release.ReleaseType.released])),
            ],
            Targets =
            [
                new(nameof(SetupBuildInfo)),
                new(nameof(PackProjects)),
                new(nameof(TestProjects))
                {
                    MatrixDimensions =
                    [
                        new(nameof(TestFramework))
                        {
                            Values = TestFrameworkNames,
                        },
                    ],
                    Options = [BuildOptions.Steps.SetupDotnet.Dotnet80X(), BuildOptions.Steps.SetupDotnet.Dotnet90X()],
                },
                new(nameof(PushToNuget))
                {
                    Options = [BuildOptions.Inject.Secret(nameof(NugetApiKey))],
                },
                new(nameof(PushToRelease))
                {
                    Options =
                    [
                        BuildOptions.Inject.Secret(nameof(GithubToken)),
                        new GithubTokenPermissionsOption(new Permissions.Exact(new()
                        {
                            Contents = PermissionsLevel.Write,
                        })),
                        BuildOptions.Target.RunIfWorkflowCondition(TextExpressions
                            .Target
                            .ParamOutput(this, nameof(SetupBuildInfo), nameof(BuildVersion))
                            .Contains("-")
                            .EqualTo(false)),
                    ],
                },
            ],
            Types = [WorkflowTypes.Github.Action],
        },
        new("Dependabot Enable auto-merge")
        {
            Triggers = [WorkflowTriggers.PullIntoMain],
            Targets =
            [
                new(nameof(ApproveDependabotPr))
                {
                    Options =
                    [
                        BuildOptions.Inject.Github.PullRequestNumber,
                        BuildOptions.Inject.Github.DependabotEnableAutoMergePat,
                        BuildOptions.Target.RunIfWorkflowCondition(
                            TextExpressions.Github.GithubActor.EqualToString("dependabot[bot]")),
                    ],
                },
            ],
            Types = [WorkflowTypes.Github.Action],
        },
        WorkflowPresets.Github.Dependabot(new()
        {
            Registries = new Dictionary<string, DependabotRegistry>
            {
                ["nuget"] = new()
                {
                    Type = RegistryType.NugetFeed,
                    Url = WorkflowLabels.Github.Dependabot.NugetUrl,
                },
            },
            Updates =
            [
                new()
                {
                    Directory = "/",
                    PackageEcosystem = WorkflowLabels.Github.Dependabot.NugetEcosystem,
                    Registries = new DependabotRegistries.Named("nuget"),
                    Groups = new Dictionary<string, DependabotGroup>
                    {
                        ["nuget-deps"] = new DependabotGroup.FromPatterns
                        {
                            Patterns = ["*"],
                        },
                    },
                    Schedule = new()
                    {
                        Interval = ScheduleInterval.Daily,
                    },
                    TargetBranch = "main",
                    OpenPullRequestsLimit = 10,
                },
            ],
        }),
    ];
}
