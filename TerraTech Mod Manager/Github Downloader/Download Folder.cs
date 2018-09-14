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
    public static class GetUpdate
    {
        private static bool isUpdateAvailable = false;
        //public static bool ModUpdateAvailable(string CloudName)
        //{

        //}
        public static bool ProgramUpdateAvailable()
        {
            if (isUpdateAvailable)
            {
                return true;
            }
            var releases = WebClientHandler.DeserializeApiCall<GithubItem[]>("https://api.github.com/repos/Aceba1/TerraTech-Mod-Manager/releases");
            return releases[0].tag_name == Start.Version_Number;
            
        }

        private class GithubItem
        {
            public string tag_name;
            public string name;
            public string body;
            public string html_url;
        }
    }

    public static class GetUpdateInfo
    {
        //https://api.github.com/repos/_owner_/_reponame_/releases [0]
    }

    public static class GetRepos
    {
        public static int Page { get; internal set; }

        public static string eelse;


        public class GithubRepo
        {

        }
    }

    //https://api.github.com/search/repositories?q=topic:ttqmm
    //https://api.github.com/search/repositories?q=topic:ttqmm&page:0



    public static class DownloadFolder
    {
        public static string[] Download(string RepositoryPath, string CloudName, string DownloadPath)
        {
            string StartBranch = RepositoryPath.Substring(RepositoryPath.IndexOf("tree/master/") + 11);
            NewMain.inst.Log(GetApiUrl(CloudName, StartBranch), Color.Red);
            return ProcessListOfEntries(WebClientHandler.DeserializeApiCall<GithubItem[]>(GetApiUrl(CloudName, StartBranch)), DownloadPath, RepositoryPath, CloudName, StartBranch);
        }

        public static string GetApiUrl(string CloudName, string BranchingPath)
        {
            /* <quote>
            This API has an upper limit of 1,000 files for a directory.
            If you need to retrieve more files, use the Git Trees API.
            This API supports files up to 1 megabyte in size.
               </quote> */
            string subpath = "";
            if (BranchingPath.Length != 0)
            {
                subpath = BranchingPath;
                subpath += @"?ref=master";
            }
            return "https://api.github.com/repos" + CloudName + "/contents" + BranchingPath;
        }

        private static string[] ProcessListOfEntries(IEnumerable<GithubItem> entries, string DownloadFolder, string repostiorySubDir, string CloudName, string StartBranch, string CurrentPath = "")
        {
            List<string> outFolders = new List<string>();
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
                        if (!System.IO.Directory.Exists(DownloadFolder + CurrentPath + @"\" + localItem.name))
                        {
                            System.IO.Directory.CreateDirectory(DownloadFolder + CurrentPath + @"\" + localItem.name);
                        }
                        outFolders.Add(localItem.name);
                        var subEntries = WebClientHandler.DeserializeApiCall<GithubItem[]>(localItem.url);
                        if (!subEntries.Any())
                        {
                            continue;
                        }

                        ProcessListOfEntries(subEntries, DownloadFolder, repostiorySubDir, CloudName, StartBranch, CurrentPath + @"\" + localItem.name);
                    }
                    else if (localItem.type == "file")
                    {
                        using (var wc = new WebClient())
                        {
                            NewMain.inst.Log("Downloading " + CurrentPath + @"\" + localItem.name + " from ", Color.Green);
                            string filepath = DownloadFolder + CurrentPath + @"\" + localItem.name;
                            if (System.IO.File.Exists(filepath))
                            {
                                try
                                {
                                    System.IO.File.Delete(filepath);
                                }
                                catch
                                {
                                    NewMain.inst.Log("Could not remove existing file!", Color.Red);
                                }
                            }
                            try
                            {
                                wc.DownloadFile(localItem.download_url, filepath);
                            }
                            catch
                            {
                                NewMain.inst.Log("Could not download " + localItem.name + "!", Color.Red);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NewMain.inst.Log(ex.Message, Color.Red);
            }
            return outFolders.ToArray();
        }

        private class GithubItem
        {
            public string name;
            public string url;
            public string download_url;
            public string path;
            public long size;
            public string type;
        }
    }

    internal static class WebClientHandler
    {
        public static T DeserializeApiCall<T>(string ApiUrl)
        {
            WebClient webClient = new WebClient();
        Retry:
            webClient.Headers.Add("user-agent", "ttmm-downloader-client");
            bool flag = false;
            if (NewMain.GithubToken != null && NewMain.GithubToken != "" && NewMain.GithubToken != "Github Token")
            {
                webClient.Headers.Add("Authorization", "Token " + NewMain.GithubToken);
                flag = true;
            }
            try
            {
                string jsonData = webClient.DownloadString(ApiUrl);
                return JsonConvert.DeserializeObject<T>(jsonData);
            }
            catch
            {
                if (flag)
                {
                    NewMain.GithubToken = "";
                    NewMain.inst.Log("Github Token does not appear valid; Resetting...", Color.Orange);
                    goto Retry;
                }
                throw new Exception("Could not access API! Try again later?");
            }
        }
    }
}
