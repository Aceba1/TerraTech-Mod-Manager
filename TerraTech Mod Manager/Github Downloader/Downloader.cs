using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TerraTechModManager.Downloader
{
    public static class GetUpdateInfo
    {
        public static GithubReleaseItem GetReleases(string CloudName)
        {
            return WebClientHandler.DeserializeApiCall<GithubReleaseItem>("https://api.github.com/repos/" + CloudName + "/releases/latest");
        }

        public class GithubReleaseItem
        {
            public string tag_name;
            public string name;
            public string body;
            public string html_url;
        }
    }

    public static class GetRepos
    {
        public static double Page { get; internal set; }
        public static double TotalCount { get; internal set; }
        public static double Counted { get; internal set; }
        public static string Search { get; internal set; }
        public static bool MorePagesAvailable
        {
            get => Counted < TotalCount;
        }

        public static string SearchReposURL(string search, double page = 0) => "https://api.github.com/search/repositories" +
                $"?per_page=100&page:{page}&q=topic:ttqmm{(string.IsNullOrEmpty(search) ? "" : "+" + search)}";

        public static GithubRepoItem GetOneRepo(string CloudName)
        {
            return WebClientHandler.DeserializeApiCall<GithubRepoItem>("https://api.github.com/repos/" + CloudName);
        }

        public static GithubRepoItem[] GetFirstPage(string search = "")
        {
            Search = Uri.EscapeUriString(search);
            var repos = WebClientHandler.DeserializeApiCall<GithubRepos>(SearchReposURL(Search));
            Counted = repos.items.Length;
            Page = 0;
            TotalCount = repos.total_count;
            return repos.items;
        }

        public static GithubRepoItem[] GetNextPage()
        {
            Page++;
            var repos = WebClientHandler.DeserializeApiCall<GithubRepos>(SearchReposURL(Search, Page));
            Counted += repos.items.Length;
            if (TotalCount != repos.total_count)
            {
                NewMain.inst.Log("Something has changed while this session was opened!", Color.LightBlue);
                TotalCount = repos.total_count;
            }
            return repos.items;
        }

        public class GithubRepos
        {
            public double total_count;
            public GithubRepoItem[] items;
        }

        public class GithubRepoItem
        {
            public string full_name;
            public string name;
            public string description;
            public string html_url;
            public string pushed_at;
        }
    }

    //https://api.github.com/search/repositories?q=topic:ttqmm
    //https://api.github.com/search/repositories?q=topic:ttqmm&page:0



    public static class DownloadFolder
    {
        public static IEnumerable<string> Download(string RepositoryPath, string CloudName, string DownloadPath)
        {
            string StartBranch = RepositoryPath.Substring(RepositoryPath.IndexOf("tree/master/") + 11);
            NewMain.inst.Log(GetApiUrl(CloudName, StartBranch), Color.Red);
            var Entries = WebClientHandler.DeserializeApiCall<GithubItem[]>(GetApiUrl(CloudName, StartBranch));
            foreach (var entry in Entries)
            {
                if (entry.type == "file" && entry.name == "mod.json")
                {
                    DownloadPath = System.IO.Path.Combine(DownloadPath, CloudName.Substring(CloudName.LastIndexOf('/') + 1));
                    break;
                }
            }
            return RecursiveDownload(Entries, DownloadPath);
        }

        public static string GetApiUrl(string CloudName, string BranchingPath) => "https://api.github.com/repos/" + CloudName + "/contents" + BranchingPath;

        private static List<string> RecursiveDownload(IEnumerable<GithubItem> entries, string DownloadFolder, string CurrentPath = "")
        {
            List<string> result = new List<string>();
            try
            {
                foreach (var item in entries)
                {
                    if (NewMain.KillDownload)
                    {
                        NewMain.KillDownload = false;
                        throw new Exception("The download was killed, but it did not finish!");
                    }
                    var localItem = item;

                    if (localItem.type == "dir")
                    {
                        if (!System.IO.Directory.Exists(DownloadFolder + CurrentPath + @"/" + localItem.name))
                        {
                            System.IO.Directory.CreateDirectory(DownloadFolder + CurrentPath + @"/" + localItem.name);
                        }

                        var subEntries = WebClientHandler.DeserializeApiCall<GithubItem[]>(localItem.url);
                        if (!subEntries.Any())
                        {
                            continue;
                        }

                        result.AddRange(RecursiveDownload(subEntries, DownloadFolder, CurrentPath + "/" + localItem.name));
                    }
                    else if (localItem.type == "file")
                    {
                        if (localItem.name == "mod.json")
                        {
                            result.Add(DownloadFolder + CurrentPath);
                        }
                        using (var wc = new WebClient())
                        {
                            NewMain.inst.Log("Downloading " + CurrentPath + "/" + localItem.name, Color.Green);
                            string filepath = DownloadFolder + CurrentPath + "/" + localItem.name;
                            if (System.IO.File.Exists(filepath))
                            {
                                while (true)
                                {
                                    try
                                    {
                                        System.IO.File.Delete(filepath);
                                        break;
                                    }
                                    catch (Exception E)
                                    {
                                        NewMain.inst.Log("File is occupied!\n" + E.Message, Color.Red);
                                        System.Threading.Thread.Sleep(10000);
                                    }
                                }
                            }
                            while (true)
                            {
                                try
                                {
                                    wc.DownloadFile(localItem.download_url, filepath);
                                    break;
                                }
                                catch (Exception E)
                                {
                                    NewMain.inst.Log("Could not download " + localItem.name + "!\n" + E.Message, Color.Red);

                                    System.Threading.Thread.Sleep(30000);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NewMain.inst.Log(ex.Message, Color.Red);
            }
            return result;
        }

        private class GithubItem
        {
            public string name = null;
            public string url = null;
            public string download_url = null;
            public string path = null;
            public long size = 0;
            public string type = null;
        }
    }

    internal static class WebClientHandler
    {
        public static T DeserializeApiCall<T>(string ApiUrl)
        {
            string Token = NewMain.GithubToken;
            WebClient webClient = new WebClient();
            Retry:
            webClient.Headers.Add("user-agent", "ttmm-downloader-client");
            bool flag = false;
            if (Token != null && Token != "" && Token != "Github Token")
            {
                webClient.Headers.Add("Authorization", "Token " + Token);
                flag = true;
            }
            try
            {
                string jsonData = webClient.DownloadString(ApiUrl);
                return JsonConvert.DeserializeObject<T>(jsonData);
            }
            catch (Exception E)
            {
                if (flag)
                {
                    Token = "";
                    NewMain.inst.Log("Skipping Github Token", Color.Orange);
                    goto Retry;
                }
                throw new Exception($"Could not access API!\n{(E.Message.Contains("Forbidden") ? "(Github Rate Limiting: Try setting a Github user token in the Config)\n" : "")}{E.Message}\n{ApiUrl}");
            }
        }
    }
}
