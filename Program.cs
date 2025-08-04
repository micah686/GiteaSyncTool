using System.Text;
using Newtonsoft.Json;
using Octokit;
using RestSharp;
using Spectre.Console;


namespace GiteaSyncTool
{
    internal class Program
    {
        private const string SETTINGS_FILE = "settings.json";
        static async Task Main(string[] args)
        {
            System.Console.OutputEncoding = Encoding.UTF8;
            System.Console.InputEncoding = Encoding.UTF8;

            SettingsFile.GenerateSettings();
            var settings = SettingsFile.LoadSettings();
            if (settings == null)
            {
                AnsiConsole.MarkupLine($"[red]Failed to load settings. Exiting...[/]");
                return;
            }

            string starsFile = "stars.json";
            string reposFile = "repos.json";
            if (!File.Exists(starsFile) && !File.Exists(reposFile))
            {
                await GetGithubReposAndStars(settings);
            }

            var reposJson = File.ReadAllText(reposFile);
            var starsJson = File.ReadAllText(starsFile);


            var githubRepos = JsonConvert.DeserializeObject<IReadOnlyList<GithubRepoInfo>>(reposJson);
            var githubStars = JsonConvert.DeserializeObject<IReadOnlyList<GithubRepoInfo>>(starsJson);


            await SyncToGitea(settings, githubRepos, githubStars);

        }

        static async Task GetGithubReposAndStars(BaseSettings settings)
        {
            var githubClient = new GitHubClient(new ProductHeaderValue("GiteaSyncApp"))
            {
                Credentials = new Credentials(settings.GithubExportSettings.GithubToken)
            };

            List<Octokit.Repository> repositoryFullList = new();
            List<Octokit.Repository> starsFullList = new();

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Fetching {settings.GithubExportSettings.GithubUsername}'s repositories and stars metadata from GitHub. Please wait", async ctx =>
                {
                    var repos = await githubClient.Repository.GetAllForUser(settings.GithubExportSettings.GithubUsername);
                    repositoryFullList = repos.ToList();
                    var stars = await githubClient.Activity.Starring.GetAllForUser(settings.GithubExportSettings.GithubUsername);
                    starsFullList = stars.ToList();
                });


            List<GithubRepoInfo> repoList = new List<GithubRepoInfo>();
            List<GithubRepoInfo> starList = new List<GithubRepoInfo>();
            foreach (var repo in repositoryFullList)
            {
                repoList.Add(new GithubRepoInfo
                {
                    GithubFullUrl = repo.HtmlUrl,
                    RepoName = repo.Name,
                    Description = repo.Description,
                    Homepage = repo.Homepage,
                    Language = repo.Language,
                    HasWiki = repo.HasWiki,
                    HasIssues = repo.HasIssues,
                    HasDownloads = repo.HasDownloads,
                    RepositorySize = repo.Size,
                    IsArchived = repo.Archived,
                    Topics = repo.Topics.ToArray(),
                    IsVisible = !repo.Private
                });
            }
            foreach (var star in starsFullList)
            {
                starList.Add(new GithubRepoInfo
                {
                    GithubFullUrl = star.HtmlUrl,
                    RepoName = star.Name,
                    Description = star.Description,
                    Homepage = star.Homepage,
                    Language = star.Language,
                    HasWiki = star.HasWiki,
                    HasIssues = star.HasIssues,
                    HasDownloads = star.HasDownloads,
                    RepositorySize = star.Size,
                    IsArchived = star.Archived,
                    Topics = star.Topics.ToArray(),
                    IsVisible = !star.Private
                });
            }

            var reposJson = JsonConvert.SerializeObject(repoList, Formatting.Indented);
            File.WriteAllText("repos.json", reposJson);

            var starsJson = JsonConvert.SerializeObject(starList, Formatting.Indented);
            File.WriteAllText("stars.json", starsJson);
        }

        static async Task SyncToGitea(BaseSettings settings, IReadOnlyList<GithubRepoInfo> repos, IReadOnlyList<GithubRepoInfo> stars)
        {

            var baseUrl = $"{settings.GiteaSyncSettings.GiteaUrl}/api/v1";
            var httpClient = new HttpClient();
            var rco = new RestClientOptions(baseUrl);
            var restClient = new RestClient(rco, useClientFactory: true);
            restClient.AddDefaultHeader("Authorization", $"token {settings.GiteaSyncSettings.GiteaToken}");
            // do an initial request to check if the token is valid
            var authRequest = new RestRequest($"/user/emails", Method.Get);
            var authResponse = await restClient.ExecuteAsync(authRequest);

            List<string> existingRepoNames = new List<string>();
            if (!settings.GiteaSyncSettings.OwnerIsOrg)
            {
                var repoCheckRequest = new RestRequest($"/users/{settings.GiteaSyncSettings.Owner}/repos", Method.Get);
                var repoCheckResponse = await restClient.ExecuteAsync(repoCheckRequest);

                var array = JsonConvert.DeserializeObject<List<dynamic>>(repoCheckResponse?.Content);
                existingRepoNames = array
                    .Where(item => item.name != null)
                    .Select(item => (string)item.name)
                    .ToList();
            }
            else
            {
                var repoCheckRequest = new RestRequest($"/orgs/{settings.GiteaSyncSettings.Owner}/repos", Method.Get);
                var repoCheckResponse = await restClient.ExecuteAsync(repoCheckRequest);

                var array = JsonConvert.DeserializeObject<List<dynamic>>(repoCheckResponse?.Content);
                existingRepoNames = array
                    .Where(item => item.name != null)
                    .Select(item => (string)item.name)
                    .ToList();
            }


            var allRepositories = new List<GithubRepoInfo>();
            allRepositories.AddRange(repos);
            allRepositories.AddRange(stars);

            int counter = 0;
            foreach (var repo in allRepositories)
            {
                try
                {
                    if (counter != 0 && counter % 100 == 0)
                    {
                        AnsiConsole.MarkupLine($"[blue]Processed {counter} repositories. Delaying for a bit[/]");
                        await Task.Delay(TimeSpan.FromSeconds(15));
                    }

                    if (existingRepoNames.Contains(repo.RepoName))
                    {
                        AnsiConsole.MarkupLine($"[yellow]Repository {repo.RepoName} already exists. Skipping...[/]");
                        continue;
                    }
                    AnsiConsole.MarkupLine($"[yellow]Processing repository: {repo.RepoName}[/]");


                    var migration = new MigrateRepoOptions()
                    {
                        CloneAddr = repo.GithubFullUrl,
                        RepoName = repo.RepoName,
                        Service = MigrateRepoOptions.ServiceEnum.Github,

                        AuthToken = settings.GithubExportSettings.GithubToken,
                        Description = repo.Description ?? "",
                        Issues = settings.GithubExportSettings.Issues,
                        Labels = settings.GithubExportSettings.Labels,
                        Lfs = settings.GithubExportSettings.MigrateLFS,
                        //LFSEndpoint = settings.GithubExportSettings.LFSEndpoint,
                        Milestones = settings.GithubExportSettings.Milestones,
                        Mirror = settings.GithubExportSettings.IsMirror,
                        //MirrorInterval = 
                        Private = settings.GiteaSyncSettings.IsPrivate,
                        PullRequests = settings.GithubExportSettings.PullRequests,
                        Releases = settings.GithubExportSettings.Releases,
                        RepoOwner = settings.GiteaSyncSettings.Owner,
                        Wiki = settings.GithubExportSettings.Wiki,
                    }.ToJson();

                    AnsiConsole.MarkupLine($"Migrating repository: {repo.RepoName}.");
                    var request = new RestRequest("/repos/migrate", Method.Post);
                    request.AddJsonBody(migration);
                    var response = await restClient.ExecuteAsync(request);
                    if (response.IsSuccessful)
                    {
                        AnsiConsole.MarkupLine($"[green]Successfully migrated repository: {repo.RepoName}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]Failed to migrate repository: {repo.RepoName}. Status: {response.StatusCode}[/]");
                    }
                    // Delay to avoid hitting rate limits
                    await Task.Delay(TimeSpan.FromSeconds(settings.ImportDelay + 1));
                    AnsiConsole.MarkupLine($"[blue]Waiting {settings.ImportDelay} seconds before next migration...[/]");
                    counter++;
                }
                catch (Exception)
                {

                    continue;
                }
                
            }
        }

        
    }
}
