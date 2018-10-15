﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TerraTechModManager
{
    public partial class NewMain : Form
    {
#warning Change version number with every update
        public const string Version_Number = "1.3";

        private string LastSearch = "";

        public static string StartMessage = "";

        private static List<Task> TaskQueue = new List<Task>();

        public static NewMain inst;

        public static string GithubToken
        {
            get
            {
                return inst.githubTokenToolStripMenuItem.Text;
            }
            set
            {
                inst.githubTokenToolStripMenuItem.Text = "Github Token";
            }
        }

        List<ModInfo> Downloads = new List<ModInfo>();
        Task CurrentDownload;
        public static bool KillDownload = false;
        public static string KillDownloadPath = "";

        bool DownloadsPending
        {
            get => Downloads.Count != 0;
        }
        
        private Dictionary<string, ModInfo> LocalMods = new Dictionary<string, ModInfo>();
        private Dictionary<string, ModInfo> GithubMods = new Dictionary<string, ModInfo>();
        private Dictionary<string, ModInfo> FoundServerMods = new Dictionary<string, ModInfo>();
        /// <summary>
        /// Key: ttroot
        /// </summary>
        public string RootFolder;
        public string DataFolder;
        public NewMain()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            listViewCompactMods.FullRowSelect = true;
        }

        private static void CycleQueue(Task task)
        {
            TaskQueue.RemoveAt(0);
            int count = TaskQueue.Count;
            if (count != 0)
            {
                TaskQueue[0].Start();
            }
        }

        public static void AddToTaskQueue(Task task)
        {
            task.ContinueWith(CycleQueue);
            TaskQueue.Add(task);
            if (TaskQueue.Count == 1)
            {
                task.Start();
            }
        }

        private void LoadConfig()
        {
            bool ShowProgramUpdatePrompt = true;
            ConfigHandler.TryGetValue(ref ShowProgramUpdatePrompt, "getprogramupdates");
            showProgramUpdatesToolStripMenuItem.Checked = ShowProgramUpdatePrompt;

            bool LookForModUpdates = true;
            ConfigHandler.TryGetValue(ref LookForModUpdates, "getmodupdates");
            lookForModUpdatesToolStripMenuItem.Checked = LookForModUpdates;

            tabControl1.SelectedIndex = 1;
            ChangeVisibilityOfTabBar(!ConfigHandler.TryGetValue("hidetabs", true));
            bool hide = ConfigHandler.TryGetValue("hidelog", false);
            splitContainer1.Panel2Collapsed = hide;
            hideLogToolStripMenuItem.Checked = hide;
            skipStartToolStripMenuItem.Checked = Start.SkipAtStart;

            githubTokenToolStripMenuItem.Text = ConfigHandler.TryGetValue("githubtoken", "Github Token");

            if (showProgramUpdatesToolStripMenuItem.Checked)
            {
                AddToTaskQueue(new Task(action: LookForProgramUpdate_v));
            }

            ReloadLocalMods(true); // False for synchronous local/server mod loading, True for stability
        }
        public void SaveConfig()
        {
            ConfigHandler.SetValue(showProgramUpdatesToolStripMenuItem.Checked, "getprogramupdates");
            ConfigHandler.SetValue(lookForModUpdatesToolStripMenuItem.Checked, "getmodupdates");
            ConfigHandler.SetValue(panelHideTabs.Visible, "hidetabs");
            ConfigHandler.SetValue(splitContainer1.Panel2Collapsed, "hidelog");
            ConfigHandler.SetValue(Start.SkipAtStart, "skipstart");
            ConfigHandler.SetValue(githubTokenToolStripMenuItem.Text, "githubtoken");
            ConfigHandler.SaveConfig();
        }

        private void NewMain_Load(object sender, EventArgs e)
        {
            inst = this;
            Log(StartMessage, Color.Red, false);

            RunPatcher.MMWindow = this;
            RunPatcher.PathToExe = DataFolder + @"\Managed\QModManager.exe";
            if (!File.Exists(RunPatcher.PathToExe))
            {
                RunPatcher.UpdatePatcher(DataFolder + @"\Managed");
            }

            RunPatcher.RunExe("-i");
            ChangeVisibilityOfCompactModInfo(false);

            LoadConfig();

            NewMain.AddToTaskQueue(new Task(GetGitMods));
        }

        private void ClearGitMods()
        {
            for (int i = listViewCompactMods.Groups[1].Items.Count - 1; i >= 0; i--)
            {
                var item = listViewCompactMods.Groups[1].Items[i];
                listViewCompactMods.Items.Remove(item);
            }
            GithubMods.Clear();
        }

        private void LookForProgramUpdate_v()
        {
            LookForProgramUpdate();
        }

        private bool LookForProgramUpdate()
        {
            bool result = false;
            try
            {
                var latest = Downloader.GetUpdateInfo.GetReleases("Aceba1/TerraTech-Mod-Manager");
                if (latest.tag_name != Version_Number)
                {
                    result = true;
                    using (var updateScreen = new Update(latest))
                    {
                        updateScreen.ShowDialog();
                        if (updateScreen.Return == 1)
                        {
                            string targetPath = Environment.CurrentDirectory;
                            string downloadPath = Path.Combine(targetPath, "Update");
                            var info = Directory.CreateDirectory(downloadPath);
                            Downloader.DownloadFolder.Download("https://github.com/Aceba1/TerraTech-Mod-Manager/tree/master/Executable", "Aceba1/TerraTech-Mod-Manager", downloadPath);

                            Process process = new Process();
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.FileName = "cmd.exe";
                            startInfo.Arguments = "/C" +
                                "color f3" + "&" +
                                "echo Updating TTMM 2..." + "&" +
                                "timeout 8" + "&" +
                                "move /y \"" + downloadPath + "\\*\" \"" + targetPath + "\"" + "&" +
                                "del /q " + downloadPath + "&" +
                                "echo Program finished updating" + "&" +
                                "timeout 5" + "&" +
                                "start \"ttmm2\" \"" + targetPath + "\\" + info.GetFiles("*.exe")[0].Name + "\"";
                            process.StartInfo = startInfo;
                            Downloads.Clear();
                            process.Start();
                            Close();
                        }
                        else if (updateScreen.Return == -1)
                        {
                            showProgramUpdatesToolStripMenuItem.Checked = false;
                        }
                    }
                }
            }
            catch(Exception E)
            {
                Log("Update check failed: " + E.Message, Color.Red);
            }
            return result;
        }

        private void ChangeVisibilityOfCompactModInfo(bool Show)
        {
            int show = Show ? 1 : 0;
            if (tableLayoutPanel1.RowStyles[1].Height + show != 51)
            {
                tableLayoutPanel1.RowStyles[1].Height = 50 * show;
                tableLayoutPanel1.RowStyles[2].Height = 25 * show;
            }
        }

        private void ChangeVisibilityOfTabBar(bool Show)
        {
            if (Show)
            {
                tabControl1.ItemSize = new Size(45, 18);
                tabControl1.SizeMode = TabSizeMode.Normal;
                panelHideTabs.Visible = false;
            }
            else
            {
                tabControl1.ItemSize = new Size(1, 1);
                tabControl1.SizeMode = TabSizeMode.Fixed;
                panelHideTabs.Visible = true;
            }
            hideTabsToolStripMenuItem.Checked = !Show;
        }

        public void Log(string value, Color color, bool newLine = true)
        {
            richTextBoxLog.DeselectAll();
            richTextBoxLog.SelectionColor = color;
            richTextBoxLog.AppendText(value + (newLine ? "\r" : ""));
            richTextBoxLog.ScrollToCaret();
        }


        private void InstallPatch(object sender, EventArgs e)
        {
            RunPatcher.RunExe("-i");
        }

        private void UninstallPatch(object sender, EventArgs e)
        {
            RunPatcher.RunExe("-u");
        }

        #region Local Mod Handling

        private void ClearLocalMods()
        {
            for (int i = listViewCompactMods.Groups[0].Items.Count - 1; i >= 0; i--)
            {
                var item = listViewCompactMods.Groups[0].Items[i];
                listViewCompactMods.Items.Remove(item);
            }
        }

        private void ReloadLocalMods(bool Queue = true)
        {
            var task = new Task(GetLocalMods);
            if (Queue)
                AddToTaskQueue(task);
            else
                task.Start();
        }
        
        internal void SetLocalModState(string path, ModInfo.ModState State)
        {
            string modjson = path + @"\mod.json";

            var json = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(modjson));
            json["Enable"] = State == ModInfo.ModState.Enabled;
            File.WriteAllText(modjson, JsonConvert.SerializeObject(json, Formatting.Indented));
        }

        internal void SetLocalModDisabled(ref string path, bool Disable)
        {
            string newPath = RootFolder + @"\QMods" + (Disable ? @"-Disabled\" : @"\") + new DirectoryInfo(path).Name;
            Directory.Move(path, newPath);
            path = newPath;
        }


        #region Get Local Mods

        private void GetLocalMods()
        {
            if (!Directory.Exists(RootFolder + @"\QMods"))
            {
                Directory.CreateDirectory(RootFolder + @"\QMods");
            }
            else foreach (string folder in Directory.GetDirectories(RootFolder + @"\QMods"))
            {
                try
                {
                    GetLocalMod_Internal(folder, false);
                }
                catch (Exception E)
                {
                    Log("There was a problem handling a local mod: \n" + E.Message + "\nAt " + folder, Color.Red);
                }
            }
            if (!Directory.Exists(RootFolder + @"\QMods-Disabled"))
            {
                Directory.CreateDirectory(RootFolder + @"\QMods-Disabled");
            }
            else foreach (string folder in Directory.GetDirectories(RootFolder + @"\QMods-Disabled"))
            {
                try
                {
                    GetLocalMod_Internal(folder, true);
                }
                catch (Exception E)
                {
                    Log("There was a problem handling a local mod: \n" + E.Message + "\nAt " + folder, Color.Red);
                }
            }
        }

        internal void GetLocalMod_Internal(string path, bool IsDisabled = false)
        {
            string modjson = path + @"\mod.json";
            string ttmmjson = path + @"\ttmm.json";
            if (File.Exists(modjson))
            {
                var json = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(modjson));
                ModInfo modInfo = null;
                if (File.Exists(ttmmjson))
                {
                    try
                    {
                        modInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<ModInfo>(File.ReadAllText(ttmmjson), new JsonSerializerSettings() { MissingMemberHandling = MissingMemberHandling.Ignore });
                    }
                    catch { }
                }
                if (modInfo != null)
                {
                    modInfo.State = IsDisabled ? ModInfo.ModState.Disabled : (Convert.ToBoolean(json["Enable"]) ? ModInfo.ModState.Enabled : ModInfo.ModState.Inactive);
                    modInfo.FilePath = path;
                    modInfo.CloudName = modInfo.CloudName[0] == '/' ? modInfo.CloudName.Substring(1) : modInfo.CloudName;
                }
                else
                {
                    modInfo = new ModInfo()
                    {
                        Name = json["DisplayName"] as string,
                        Author = json["Author"] as string,
                        State = IsDisabled ? ModInfo.ModState.Disabled : (Convert.ToBoolean(json["Enable"]) ? ModInfo.ModState.Enabled : ModInfo.ModState.Inactive),
                        FilePath = path,
                        InlineDescription = json["Id"] as string
                    };
                }

                bool flag = true;
                if (LocalMods.TryGetValue(modInfo.Name, out ModInfo oldModInfo))
                {
                    oldModInfo.Visible.Remove();
                    flag = false;
                }

                var item = modInfo.GetListViewItem(listViewCompactMods.Groups[0], true);

                LocalMods[modInfo.Name] = modInfo;
                modInfo.Visible = listViewCompactMods.Items.Add(item);
                if (modInfo.CloudName != null && modInfo.CloudName != "" && flag)
                {
                    string version = FindServerMod(modInfo.CloudName);
                    if (version != "" && version != modInfo.CurrentVersion)
                    {
                        modInfo.Visible.SubItems[2].Text = "[Update Available] " + modInfo.Visible.SubItems[2].Text;
                    }
                }
            }
        }

        string FindServerMod(string CloudName)
        {
            string result = "";
            try
            {
                ModInfo serverMod;
                if (GithubMods.TryGetValue(CloudName, out serverMod))
                {
                    GithubMods.Remove(CloudName);
                    serverMod.FoundLocal = true;
                    serverMod.TrySetChecked(true);
                    if (lookForModUpdatesToolStripMenuItem.Checked)
                    result = serverMod.GetVersionTagFromCloud();
                }
                else
                {
                    serverMod = new ModInfo(Downloader.GetRepos.GetOneRepo(CloudName));
                    serverMod.FoundLocal = true;
                    if (lookForModUpdatesToolStripMenuItem.Checked)
                    result = serverMod.GetVersionTagFromCloud();
                }
                FoundServerMods[CloudName] = serverMod;
                if (GithubMods.TryGetValue(CloudName, out ModInfo serverMod2)) // Temporary duplication fix for synchronous local/server mod loading
                {
                    serverMod.Visible = serverMod2.Visible;
                    GithubMods.Remove(CloudName);
                    serverMod.TrySetChecked(true);
                }
            }
            catch (Exception E)
            {
                Log(E.Message, Color.Red);
            }
            return result;
        }

        #endregion

        #endregion

        #region Github Mod Search

        private void GetGitMods()
        {
            int count = 0;
            try
            {
                LastSearch = textBoxModSearch.Text;
                var repos = Downloader.GetRepos.GetFirstPage(LastSearch);
                count = repos.Length;
                foreach (var repo in repos)
                {
                    GithubModFromRepo(repo);
                }
                ShowLoadMoreModsButton(Downloader.GetRepos.MorePagesAvailable, "Load more");
            }
            catch (Exception E)
            {
                Log(E.Message, Color.Red);
            }
            Log("Found " + count.ToString() + " Github mods" + (LastSearch == "" ? "" : " with search '" + LastSearch + "'"), Color.LightBlue);
        }

        private void GetMoreGitMods()
        {
            int count = 0;
            try
            {
                LastSearch = textBoxModSearch.Text;
                var repos = Downloader.GetRepos.GetNextPage();
                count = repos.Length;
                foreach (var repo in repos)
                {
                    GithubModFromRepo(repo);
                }
                ShowLoadMoreModsButton(Downloader.GetRepos.MorePagesAvailable, "Load more");
            }
            catch (Exception E)
            {
                Log(E.Message, Color.Red);
            }
            Log("Found " + count.ToString() + " Github mods" + (LastSearch == "" ? "" : " with search '" + LastSearch + "'"), Color.LightBlue);
        }

        void GithubModFromRepo(Downloader.GetRepos.GithubRepoItem repo)
        {
            ModInfo mod;
            if (FoundServerMods.TryGetValue(repo.full_name, out mod))
            {
                mod.Visible = listViewCompactMods.Items.Add(mod.GetListViewItem(listViewCompactMods.Groups[1], false));
                mod.FoundLocal = true;
                mod.TrySetChecked(true);
            }
            else
            {
                mod = new ModInfo(repo);
                GithubMods.Add(mod.CloudName, mod);
                mod.Visible = listViewCompactMods.Items.Add(mod.GetListViewItem(listViewCompactMods.Groups[1], false));
                if (FoundServerMods.TryGetValue(repo.full_name, out ModInfo mod2)) // Temporary duplication fix for synchronous local/server mod loading
                {
                    GithubMods.Remove(mod2.CloudName);
                    mod2.FoundLocal = true;
                    mod2.Visible = mod.Visible;
                    mod2.TrySetChecked(true);
                    if (lookForModUpdatesToolStripMenuItem.Checked)
                        mod2.GetVersionTagFromCloud();
                }
            }
        }

        void ShowLoadMoreModsButton(bool Show, string Message)
        {
            buttonLoadMoreMods.Text = Message;
            buttonLoadMoreMods.Enabled = Show;
            buttonLoadMoreMods.Visible = Show;
        }

        public static string DigLink(ref string Source, int SearchPosition)
        {
            int linkend1 = Source.IndexOf(')', SearchPosition);
            int linkend2 = Source.IndexOf(' ', SearchPosition);
            int linkend3 = Source.IndexOf('\n', SearchPosition);
            int linkend4 = Source.IndexOf('\r', SearchPosition);
            int linkend = int.MaxValue;
            if (linkend1 != -1)
            {
                linkend = linkend1;
            }
            if (linkend2 != -1 && linkend2 < linkend)
            {
                linkend = linkend2;
            }
            if (linkend3 != -1 && linkend3 < linkend)
            {
                linkend = linkend3;
            }
            if (linkend4 != -1 && linkend4 < linkend)
            {
                linkend = linkend4;
            }
            if (linkend == int.MaxValue)
                return Source.Substring(SearchPosition);
            else
                return Source.Substring(SearchPosition, linkend - SearchPosition);
        }

        private bool GetDescription(ref ModInfo modInfo)
        {
            var client = new WebClient();
            string descpath = "https://raw.githubusercontent.com/" + modInfo.CloudName + "/master/DESC.";
            bool flag2 = false;
            string desc = "";
            try
            {
                desc = client.DownloadString(descpath + "md");
            }
            catch
            {
                try
                {
                    desc = client.DownloadString(descpath + "txt");
                }
                catch
                {
                    flag2 = true; // File doesn't exist
                }
            }
            if (!flag2)
            {
                modInfo.Description = desc; // DESCRIPTION
                return true;
            }
            return false;
        }

        #endregion

        private void reloadLocalModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadLocalMods();
        }

        private void hideTabsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeVisibilityOfTabBar(panelHideTabs.Visible);
        }

        private void hideLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool hide = !splitContainer1.Panel2Collapsed;
            splitContainer1.Panel2Collapsed = hide;
            hideLogToolStripMenuItem.Checked = hide;
        }

        private void saveConfigFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveConfig();
        }

        private void skipStartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool skip = !TerraTechModManager.Start.SkipAtStart;
            TerraTechModManager.Start.SkipAtStart = skip;
            skipStartToolStripMenuItem.Checked = skip;
        }

        private void SetModActive(object sender, ItemCheckEventArgs e)
        {
            try
            {
                var modInfo = GetModInfoFromIndex(e.Index);
                if (modInfo.State == ModInfo.ModState.Server)
                {
                    e.NewValue = modInfo.FoundLocal ? CheckState.Checked : CheckState.Unchecked;
                    return;
                }
                if (modInfo.State != ModInfo.ModState.Disabled)
                {
                    if (listViewCompactMods.SelectedItems.Count != 0 && e.Index == listViewCompactMods.SelectedItems[0].Index)
                        comboBoxModState.SelectedIndex = e.NewValue == CheckState.Checked ? 0 : 1;
                    else
                        ChangeLocalModState(modInfo, e.NewValue == CheckState.Checked ? ModInfo.ModState.Enabled : ModInfo.ModState.Inactive);
                }
                else if (modInfo.State == ModInfo.ModState.Disabled && e.NewValue == CheckState.Checked)
                {
                    ChangeLocalModState(modInfo, e.NewValue == CheckState.Checked ? ModInfo.ModState.Enabled : ModInfo.ModState.Inactive);
                }
            }
            catch
            {

            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (listViewCompactMods.SelectedItems.Count != 0)
                {
                    var modInfo = GetModInfoFromIndex(listViewCompactMods.SelectedItems[0].Index);
                    labelModName.Text = modInfo.Name;
                    labelModIDesc.Text = modInfo.InlineDescription;

                    buttonDownloadMod.Visible = modInfo.Site != null && modInfo.Site.Length > 1;

                    if (modInfo.Site == "")
                    {
                        labelModLink.Text = modInfo.FilePath;
                    }
                    else
                    {
                        labelModLink.Text = modInfo.Site;
                    }

                    if (modInfo.State == ModInfo.ModState.Server)
                    {
                        comboBoxModState.Visible = false;
                        buttonDownloadMod.Text = "Download";
                        buttonLocalModDelete.Visible = false;
                    }
                    else
                    {
                        if (comboBoxModState.Visible == false)
                        {
                            comboBoxModState.Visible = true;
                            buttonLocalModDelete.Visible = true;
                        }
                        comboBoxModState.SelectedIndex = (int)modInfo.State;
                        buttonDownloadMod.Text = "Update";
                    }
                    ChangeVisibilityOfCompactModInfo(true);
                }
                else
                {
                    ChangeVisibilityOfCompactModInfo(false);
                }
            }
            catch(Exception E)
            {
                Log(E.Message, Color.Red);
                ChangeVisibilityOfCompactModInfo(false);
            }
        }

        private int GetCurrentIndex()
        {
            return listViewCompactMods.SelectedItems[0].Index;
        }

        private ModInfo GetModInfoFromIndex(int Index = -1)
        {
            ModInfo result;
            if (Index == -1)
            {
                string key = listViewCompactMods.SelectedItems[0].SubItems[4].Text;
                if (LocalMods.TryGetValue(key, out result))
                    return result;
                if (FoundServerMods.TryGetValue(key, out result))
                    return result;
                return GithubMods[key];
            }
            string key1 = listViewCompactMods.Items[Index].SubItems[4].Text;
            if (LocalMods.TryGetValue(key1, out result))
                return result;
            if (FoundServerMods.TryGetValue(key1, out result))
                return result;
            return GithubMods[key1];
        }

        private void OpenModLink(object sender, EventArgs e)
        {
            Process.Start(labelModLink.Text);
        }

        private void ChangeLocalModState(int Index, ModInfo.ModState newState)
        {
            try
            {
                ModInfo modInfo = GetModInfoFromIndex(Index);
                ChangeLocalModState(modInfo, newState);
            }
            catch(Exception E)
            {
                Log(E.Message, Color.Red);
            }
        }

        private void ChangeLocalModState(ModInfo modInfo, ModInfo.ModState newState)
        {
            if (newState == modInfo.State)
                return;
            if (newState == ModInfo.ModState.Disabled)
            {
                SetLocalModDisabled(ref modInfo.FilePath, true);
                Log("Relocated " + modInfo.Name, Color.DarkOliveGreen);
            }
            else
            {
                if (modInfo.State == ModInfo.ModState.Disabled)
                {
                    SetLocalModDisabled(ref modInfo.FilePath, false);
                    Log("Returned " + modInfo.Name, Color.DarkOliveGreen);
                }
                SetLocalModState(modInfo.FilePath, newState);
                Log("Set " + modInfo.Name + " to " + newState.ToString(), Color.DarkSeaGreen);
                if (newState == ModInfo.ModState.Enabled && modInfo.RequiredModNames != null)
                    foreach (var modrequired in modInfo.RequiredModNames)
                    {
                        if (LocalMods.TryGetValue(modrequired.Substring(modrequired.LastIndexOf('/') + 1), out ModInfo value))
                        {
                            foreach (ListViewItem item in listViewCompactMods.Items)
                            {
                                if (item.SubItems[4].Text == value.Name)
                                {
                                    item.Checked = true;
                                    break;
                                }
                            }
                        }
                    }
            }
            modInfo.State = newState;
        }

        private void LocalModStateChanged(object sender, EventArgs e)
        {
            try
            {
                ChangeLocalModState(-1, (ModInfo.ModState)comboBoxModState.SelectedIndex);
                listViewCompactMods.SelectedItems[0].Checked = comboBoxModState.SelectedIndex == 0;
            }
            catch { }
        }

        private void IsClosing(object sender, FormClosingEventArgs e)
        {
            if (DownloadsPending)
            {
                var result = MessageBox.Show("There are still downloads processing, are you sure you want to close?\n\nTo avoid possible problems, try to download later", "Close TTMM", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            if (TaskQueue.Count != 0)
            {
                TaskQueue.Clear();
            }
            KillDownload = true;
            Downloads.Clear();
            SaveConfig();
            KillPatcherEXE();
        }

        private void PromptDeleteMod(object sender, EventArgs e)
        {
            var response = MessageBox.Show("Are you sure you want to delete this mod?\nYou can set it as disabled instead to use later and keep completely out of the game\nDeleting may remove any special data it has created in it's folder!", "Delete Mod", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (response == DialogResult.OK)
            {
                var modInfo = GetModInfoFromIndex();
                if (listViewCompactMods.SelectedItems[0].SubItems[2].Text == "Updating...")
                {
                    if (Downloads[0].Name == modInfo.Name)
                    {
                        Log("Killing update process for " + modInfo.Name + "...", Color.DarkRed);
                        KillDownload = true;
                        KillDownloadPath = modInfo.FilePath;
                    }
                    else
                    {
                        Downloads.Remove(FoundServerMods[modInfo.CloudName]);
                        Directory.Delete(modInfo.FilePath, true);
                        Log("Cancelled update; Deleted " + modInfo.Name + "...", Color.DarkRed);
                    }
                }
                else
                {
                    Directory.Delete(modInfo.FilePath, true);
                    Log("Deleted " + modInfo.Name + "...", Color.DarkRed);
                }
                LocalMods.Remove(modInfo.Name);
                if (modInfo.CloudName != null && FoundServerMods.TryGetValue(modInfo.CloudName, out ModInfo cloudMod))
                {
                    cloudMod.FoundLocal = false;
                    FoundServerMods.Remove(modInfo.CloudName);
                    GithubMods.Add(modInfo.CloudName, cloudMod);
                    cloudMod.TrySetChecked(false);
                }
                listViewCompactMods.Items.RemoveAt(GetCurrentIndex());
            }
        }

        #region Download Mods
        
        private void GetModFromCloud(object sender, EventArgs e)
        {
            var modInfo = GetModInfoFromIndex();
            if (modInfo.State == ModInfo.ModState.Server && !modInfo.FoundLocal)
            {
                AddModDownload(modInfo);
                return;
            }
            if (modInfo.FoundLocal)
            {
                var localMod = LocalMods[modInfo.Name];
                if (localMod.State == ModInfo.ModState.Disabled)
                {
                    MessageBox.Show("Can't update a disabled mod!", "Download Mod", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (AddModDownload(modInfo))
                {
                    localMod.Visible.SubItems[2].Text = "Updating...";
                }
                return;
            }
            if (modInfo.CloudName != null && modInfo.CloudName.Length > 0 && FoundServerMods.TryGetValue(modInfo.CloudName, out ModInfo cloudModInfo))
            {
                if (modInfo.State == ModInfo.ModState.Disabled)
                {
                    MessageBox.Show("Can't update a disabled mod!", "Download Mod", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (AddModDownload(cloudModInfo))
                {
                    modInfo.Visible.SubItems[2].Text = "Updating...";
                }
            }
        }



        private bool AddModDownload(ModInfo ModBeingDownloaded, ListViewItem itemToRemove = null)
        {
            List<ModInfo> neededmods = new List<ModInfo>();
            foreach (var modname in ModBeingDownloaded.RequiredModNames)
            {
                if (GithubMods.TryGetValue(modname, out ModInfo modInfo))
                {
                    var response = MessageBox.Show(modInfo.Name + " is needed for this mod to work.\nDownload as well?", "Download Mod", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                    if (response == DialogResult.Yes)
                    {
                        neededmods.Add(modInfo);
                    }
                    else if (response == DialogResult.Cancel)
                    {
                        return false;
                    }
                }
            }
            Downloads.Add(ModBeingDownloaded);
            foreach (ModInfo mod in neededmods)
            {
                Downloads.Add(mod);
            }
            CycleDownloads();
            return true;
        }

        private void CycleDownloads()
        {
            if (DownloadsPending && CurrentDownload == null)
            {
                var downloadinfo = Downloads[0];
                SetCurrentDownload(downloadinfo.FilePath, downloadinfo.CloudName);
            }
        }

        private void SetCurrentDownload(params string[] Parameters)
        {
            CurrentDownload = new Task(Download_Internal, Parameters);
            CurrentDownload.Start();
            return;
        }

        private void Download_Internal(object Params)
        {
            var param = Params as string[];
            Log("Downloading " + param[1], Color.LawnGreen);
            try
            {
                string newFolder = TerraTechModManager.Downloader.DownloadFolder.Download(param[0], param[1], RootFolder + @"\QMods")[0];
                if (KillDownload)
                {
                    KillDownload = false;

                    throw new Exception("Download was killed at end of download!!!");
                }
                ModInfo serverMod;
                if (GithubMods.TryGetValue(param[1], out serverMod))
                {
                    FoundServerMods.Add(serverMod.CloudName, serverMod);
                    serverMod.FoundLocal = true;
                    GithubMods.Remove(param[1]);
                    serverMod.TrySetChecked(true);
                }
                else
                {
                    serverMod = FoundServerMods[param[1]];
                }
                serverMod.GetVersionTagFromCloud();
                File.WriteAllText(RootFolder + @"\QMods\" + newFolder + @"\ttmm.json", JsonConvert.SerializeObject(serverMod));
                GetLocalMod_Internal(RootFolder + @"\QMods\" + newFolder);
            }
            catch (Exception e)
            {
                Log(e.Message, Color.OrangeRed);
                if (KillDownloadPath != "")
                {
                    Directory.Delete(KillDownloadPath, true);
                    KillDownloadPath = "";
                    Log("Deleted " + param[1] + "...", Color.DarkRed);
                }
            }
            Download_Internal_2();
        }

        void Download_Internal_2()
        {
            CurrentDownload = null;
            Downloads.RemoveAt(0);
            CycleDownloads();
        }

        #endregion

        private void KillPatcherEXE()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("QModManager"))
                {
                    process.Kill();
                }
            }
            catch { }
        }

        private void DownloadPatcher(object sender, EventArgs e)
        {
            KillPatcherEXE();
            RunPatcher.UpdatePatcher(DataFolder + @"\Managed");
            RunPatcher.RunExe("-u");
            RunPatcher.IsReinstalling = true;
        }

        private void githubPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/Aceba1/TerraTech-Mod-Manager");
        }

        private void terraTechForumPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://forum.terratechgame.com/index.php?threads/terratech-mod-manager.17208/");
        }

        private void terraTechWikiPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://terratech.gamepedia.com/TerraTech_Mod_Manager");
        }

        private void tTMMDownloadPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/Aceba1/TerraTech-Mod-Manager/releases");
        }

        private void buttonLoadMoreMods_Click(object sender, EventArgs e)
        {
            bool ContinueSearch = LastSearch == textBoxModSearch.Text;
            if (ContinueSearch)
            {
                AddToTaskQueue(new Task(GetMoreGitMods));
            }
            else
            {
                ClearGitMods();
                    AddToTaskQueue(new Task(GetGitMods));
            }
        }

        private void showProgramUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showProgramUpdatesToolStripMenuItem.Checked = !showProgramUpdatesToolStripMenuItem.Checked;
        }

        private void lookForModUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lookForModUpdatesToolStripMenuItem.Checked = !lookForModUpdatesToolStripMenuItem.Checked;
        }

        private void lookForProgramUpdateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!LookForProgramUpdate())
                MessageBox.Show("No updates found", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void textBoxModSearch_TextChanged(object sender, EventArgs e)
        {
            if (textBoxModSearch.Text == LastSearch)
            {
                ShowLoadMoreModsButton(Downloader.GetRepos.MorePagesAvailable, "Load more");
            }
            else
            {
                ShowLoadMoreModsButton(true, "Search");
            }
        }

        private void buttonModShowDesc_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPageClassic;
        }

        private void buttonModHideDesc_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPageCompact;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }

    public class ModInfo
    {
        public string Name;
        public string Author;
        public string CloudName;
        public string InlineDescription;
        public string Description;
        public string Site;
        public string[] RequiredModNames;
        public string CurrentVersion;

        [JsonIgnore]
        public bool FoundLocal;

        [JsonIgnore]
        public ModState State;

        [JsonIgnore]
        public string FilePath;

        [JsonIgnore]
        public ListViewItem Visible;

        public ModInfo()
        {

        }

        public ModInfo(Downloader.GetRepos.GithubRepoItem repo)
        {
            State = ModState.Server;
            Name = repo.name;
            CloudName = repo.full_name;
            Author = repo.full_name.Substring(0, repo.full_name.IndexOf(repo.name) - 1);
            InlineDescription = repo.description;
            Site = "https://github.com/" + CloudName;

            var client = new WebClient();
            string linkspath = "https://raw.githubusercontent.com/" + CloudName + "/master/LINKS.";

            bool flag = false;
            string links = "";
            try
            {
                links = client.DownloadString(linkspath + "md");
            }
            catch
            {
                try
                {
                    links = client.DownloadString(linkspath + "txt");
                }
                catch
                {
                    flag = true; // File doesn't exist
                }
            }
            FilePath = "";
            if (!flag) // Get links from LINKS.md or LINKS.txt
            {
                var linkarray = links.Split('\n', '\r');
                FilePath = linkarray[0];
                RequiredModNames = new string[linkarray.Length - 1];
                int EmptySpace = -1;
                for (int i = 1; i < linkarray.Length; i++)
                {
                    string item = linkarray[i];
                    if (item == "")
                    {
                        EmptySpace--;
                        continue;
                    }
                    RequiredModNames[i + EmptySpace] = item[0] == '/' ? item.Substring(1) : item;
                }
                Array.Resize(ref RequiredModNames, linkarray.Length + EmptySpace);
            }
            else // Get link from README.md
            {
                string readmepath = "https://raw.githubusercontent.com/" + CloudName + "/master/README.md";
                string readme = client.DownloadString(readmepath); // Get README.md
                int linkget = readme.IndexOf("https://github.com/" + CloudName + "/tree/");
                if (linkget == -1)
                {
                    //The repo does not have a link to the latest build folder; Skipping
                    throw new Exception("Mod is not set up properly: No path to download can be found");
                }
                FilePath = NewMain.DigLink(ref readme, linkget);
                RequiredModNames = new string[0];
            }
        }

        public string GetVersionTagFromCloud()
        {
            try
            {
                CurrentVersion = Downloader.GetUpdateInfo.GetReleases(CloudName).tag_name;
                return CurrentVersion;
            }
            catch
            {
                return "";
            }
        }

        public ListViewItem GetListViewItem(ListViewGroup Group, bool IsLocal) => new ListViewItem(new string[] {
            "",
            Name,
            InlineDescription,
            Author,
            IsLocal ? Name : CloudName
        })
        {
            Group = Group,
            Checked = State == ModState.Enabled
        };

        public void TrySetChecked(bool Checked)
        {
            try
            {
                Visible.Checked = Checked;
            }
            catch { }
        }

        public enum ModState : byte
        {
            Enabled,
            Inactive,
            Disabled,
            Server
        }
    }

    public static class RunPatcher
    {
        public static string PathToExe;
        public static Process EXE;
        public static NewMain MMWindow;
        public static bool IsReinstalling = false;
        public static void UpdatePatcher(string ManagedPath)
        {
            MMWindow.Log("Downloading Patcher executable...", Color.GreenYellow);
            TerraTechModManager.Downloader.DownloadFolder.Download("https://github.com/QModManager/TerraTech/tree/master/Installer", "QModManager/TerraTech", ManagedPath);
            MMWindow.Log("Finished downloading!", Color.GreenYellow);
        }

        public static Process RunExe(string args = "")
        {
            try
            {
                if (EXE != null && !EXE.HasExited)
                {
                    MMWindow.Log("The patcher is already running!", Color.LightPink);
                    return EXE;
                }
            }
            catch { }
            Process patcher = new Process();
            patcher.StartInfo.WorkingDirectory = Path.Combine(PathToExe, @"../");
            patcher.StartInfo.FileName = PathToExe;
            patcher.StartInfo.Arguments = args;
            patcher.StartInfo.UseShellExecute = false;
            patcher.StartInfo.RedirectStandardOutput = true;
            patcher.StartInfo.CreateNoWindow = true;
            patcher.OutputDataReceived += HandlePatcher;
            patcher.Start();
            patcher.BeginOutputReadLine();

            EXE = patcher;

            MMWindow.Log("Booted patcher:", Color.FromArgb(255, 210, 52));

            return patcher;
        }

        private static void HandlePatcher(object sender, DataReceivedEventArgs e)
        {
            if (e.Data.EndsWith("exit..."))
            {
                EXE.CancelOutputRead();
                EXE.OutputDataReceived -= HandlePatcher;
                try
                {
                    EXE.CloseMainWindow();
                }
                catch { }
                try
                {
                    EXE.Close();
                }
                catch { }
                if (IsReinstalling)
                {
                    IsReinstalling = false;
                    try
                    {
                        RunPatcher.EXE.WaitForExit(5000);
                            }
                    catch { }
                    RunExe("-i");
                }
            }
            else if (e.Data.StartsWith("Tried to force"))
            {
                if (e.Data[15] == 'i')
                {
                    MMWindow.Log("The patch is already installed", Color.DarkGoldenrod);
                }
                else
                {
                    MMWindow.Log("The patch is already not installed", Color.DarkGoldenrod);
                }
            }
            else if (e.Data.EndsWith("successfully"))
            {
                MMWindow.Log(e.Data, Color.GreenYellow);
            }
            else
            {
                MMWindow.Log(e.Data, Color.Goldenrod);
            }
        }
    }
}
