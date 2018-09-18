using System;
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
        public static string StartMessage = "";

        

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
        List<ListViewItem> UpdatingItems = new List<ListViewItem>();
        Task CurrentDownload;
        public static bool KillDownload = false;
        public static string KillDownloadPath = "";

        bool DownloadsPending
        {
            get => Downloads.Count != 0;
        }

        private Dictionary<string, ModInfo> AllMods = new Dictionary<string, ModInfo>();
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

        private void LoadConfig()
        {
            bool ShowProgramUpdatePrompt = true;
            ConfigHandler.TryGetValue(ref ShowProgramUpdatePrompt, "getprogramupdates");
            hideProgramUpdatesToolStripMenuItem.Checked = !ShowProgramUpdatePrompt;

            tabControl1.SelectedIndex = ConfigHandler.TryGetValue("mmstyle", 1);
            ChangeVisibilityOfTabBar(!ConfigHandler.TryGetValue("hidetabs", false));
            bool hide = ConfigHandler.TryGetValue("hidelog", false);
            splitContainer1.Panel2Collapsed = hide;
            hideLogToolStripMenuItem.Checked = hide;
            skipStartToolStripMenuItem.Checked = Start.SkipAtStart;
            for (int i = 0; i < checkedListBoxCompactModSources.Items.Count; i++)
                checkedListBoxCompactModSources.SetItemChecked(i, ConfigHandler.TryGetValue("source" + i.ToString(), true));
            githubTokenToolStripMenuItem.Text = ConfigHandler.TryGetValue("githubtoken", "Github Token");
        }
        public void SaveConfig()
        {
            ConfigHandler.SetValue(!hideProgramUpdatesToolStripMenuItem.Checked, "getprogramupdates");
            ConfigHandler.SetValue(tabControl1.SelectedIndex, "mmstyle");
            ConfigHandler.SetValue(panelHideTabs.Visible, "hidetabs");
            ConfigHandler.SetValue(splitContainer1.Panel2Collapsed, "hidelog");
            for (int i = 0; i < checkedListBoxCompactModSources.Items.Count; i++)
                ConfigHandler.SetValue(checkedListBoxCompactModSources.GetItemChecked(i), "source" + i.ToString());
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
            ReloadLocalMods();

            LoadConfig();

            if (!hideProgramUpdatesToolStripMenuItem.Checked)
            {
                Task.Run(action:LookForProgramUpdate);
            }
        }

        private void LookForProgramUpdate()
        {
            var latest = Downloader.GetUpdateInfo.GetReleases("Aceba1/TerraTech-Mod-Manager")[0];
            if (latest.tag_name != Start.Version_Number)
            {
                var updateScreen = new Update(latest);
                updateScreen.Show();
                if (updateScreen.Return == 1)
                {
                    string downloadPath = Path.Combine(Environment.CurrentDirectory, "Update");
                    Downloader.DownloadFolder.Download("https://github.com/Aceba1/TerraTech-Mod-Manager/tree/master/Executable", "Aceba1/TerraTech-Mod-Manager", downloadPath);

                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = "/C move /y \""+downloadPath+"\" \""+ Environment.CurrentDirectory+"\"";
                    process.StartInfo = startInfo;
                    process.Start();
                    this.Close();
                }
                else if (updateScreen.Return == -1)
                {
                    hideProgramUpdatesToolStripMenuItem.Checked = true;
                }
            }
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
            string _value = value;
            richTextBoxLog.DeselectAll();
            richTextBoxLog.SelectionColor = color;
            richTextBoxLog.AppendText(_value + (newLine ? "\r" : ""));
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
        private void ReloadLocalMods()
        {
            new Task(GetLocalMods).Start();
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
                    modInfo.State = IsDisabled ? ModInfo.ModState.Disabled : (Convert.ToBoolean(json["Enable"]) ? ModInfo.ModState.Enabled : ModInfo.ModState.Load);
                    modInfo.FilePath = path;
                }
                else
                {
                    modInfo = new ModInfo()
                    {
                        Name = json["DisplayName"] as string,
                        Author = json["Author"] as string,
                        State = IsDisabled ? ModInfo.ModState.Disabled : (Convert.ToBoolean(json["Enable"]) ? ModInfo.ModState.Enabled : ModInfo.ModState.Load),
                        FilePath = path,
                        InlineDescription = json["Id"] as string
                    };
                }


                var item = modInfo.GetListViewItem(listViewCompactMods.Groups[0], true);
                if (UpdatingItems.Count > 0)
                {
                    if (GetModInfoFromIndex(UpdatingItems[0].Index).Site == modInfo.Site)
                    {
                        UpdatingItems[0].Remove();
                        UpdatingItems.RemoveAt(0);
                    }
                }
                AllMods[modInfo.Name] = modInfo;
                listViewCompactMods.Items.Add(item);
            }
        }

        #endregion

        #endregion

        #region Github Mod Search

        private void GetGitMods()
        {
            Downloader.GetRepos.GetFirstPage();
            //var client = new WebClient();
            //try
            //{                
            //    const string ModLink = @"<a href="; // End of search
            //    const string ModPoint = @"<article"; // Mod Link

            //    string site = client.DownloadString(new Uri("https://github.com/topics/ttqmm")); // Search
            //    int newmodpos = site.IndexOf(ModPoint); // Get first mod position
            //    int modpos = 0; // Current mod position
            //    while (newmodpos != -1)
            //    {
            //        modpos = newmodpos;
            //        int str = site.IndexOf(ModLink, modpos) + 9;
            //        newmodpos = site.IndexOf(ModPoint, str); // Next mod position

            //        string mod = site.Substring(str, site.IndexOf('"', str) - str);
            //        int modnamestart = mod.LastIndexOf('/') + 1;
            //        string modname = mod.Substring(modnamestart); // Get the name of the repo

            //        GetGitMod_Internal(client, mod, modname, ref site, str, true);
            //    }

            //}
            //catch (Exception E)
            //{
            //    Log(E.Message, Color.Red);
            //}
            //if (false)
            //try
            //{
            //    string StartingPoint = @"<ul class=""repo-list"">"; // Start of search
            //    string EndingPoint = @"<a href="; // End of search
            //    string ModPoint = @"href="""; // Mod Link
            //    string site = client.DownloadString(new Uri("https://github.com/search?utf8=%E2%9C%93&q=TTQMM&ref=simplesearch")); // Search
            //    int startget = site.IndexOf(StartingPoint); // Get start of search
            //    int endget = site.IndexOf(EndingPoint, startget); // Get end of search
            //    int newmodpos = site.IndexOf(ModPoint, startget); // Get first mod position
            //    int modpos = 0; // Current mod position
            //    while (newmodpos < endget)
            //    {
            //        modpos = newmodpos;
            //        int str = modpos + 6;
            //        newmodpos = site.IndexOf(ModPoint, str); // Next mod position

            //        string mod = site.Substring(str, site.IndexOf('"', str) - str);
            //        int modnamestart = mod.LastIndexOf('/') + 1;
            //        string modname = mod.Substring(modnamestart); // Get the name of the repo
            //        if (modname.StartsWith("TTQMM-")) // If the repo's name starts with "TTQMM-"
            //        {
            //            GetGitMod_Internal(client, mod, modname, ref site, modpos, false);
            //        }
            //    }
            //}
            //catch (Exception E)
            //{
            //    Log(E.Message, Color.Red);
            //}
        }
        
        private void GetGitMod_Internal(WebClient client, string mod, string modname, ref string site, int modpos, bool IDescTypeDIV)
        {
            //string linkspath = "https://raw.githubusercontent.com" + mod + "/master/LINKS.";
            //string searchfor = "https://github.com" + mod + "/tree/";

            //bool flag = false;
            //string links = "";
            //try
            //{
            //    links = client.DownloadString(linkspath + "md");
            //}
            //catch
            //{
            //    try
            //    {
            //        links = client.DownloadString(linkspath + "txt");
            //    }
            //    catch
            //    {
            //        flag = true; // File doesn't exist
            //    }
            //}
            //string downloadpath = "";
            //string[] requiredmods;
            //if (!flag) // Get links from LINKS.md or LINKS.txt
            //{
            //    var linkarray = links.Split('\n', '\r');
            //    downloadpath = linkarray[0];
            //    requiredmods = new string[linkarray.Length - 1];
            //    int EmptySpace = -1;
            //    for (int i = 1; i < linkarray.Length; i++)
            //    {
            //        if (linkarray[i] == "")
            //        {
            //            EmptySpace--;
            //            continue;
            //        }
            //        requiredmods[i + EmptySpace] = linkarray[i];
            //    }
            //    Array.Resize(ref requiredmods, linkarray.Length + EmptySpace);
            //}
            //else // Get link from README.md
            //{
            //    string readmepath = "https://raw.githubusercontent.com" + mod + "/master/README.md";
            //    string readme = client.DownloadString(readmepath); // Get README.md
            //    int linkget = readme.IndexOf(searchfor);
            //    if (linkget == -1)
            //    {
            //         The repo does not have a link to the latest build folder; Skipping
            //        return;
            //    }
            //    downloadpath = DigLink(ref readme, linkget);
            //    requiredmods = new string[0];

            //}
            //ModInfo modInfo;
            //if (AllMods.ContainsKey(mod))
            //{
            //    return;
            //}
            //else
            //{
            //    modInfo = new ModInfo()
            //    {
            //        Name = modname,
            //        CloudName = mod,
            //        Author = mod.Substring(1, mod.Length - modname.Length - 2),
            //        FilePath = downloadpath,
            //        Site = "https://github.com" + mod,
            //        RequiredModNames = requiredmods,
            //        State = ModInfo.ModState.Server,
            //    };
            //    AllMods.Add(mod, modInfo);
            //}

            // Start getting inline description
            //string ty = IDescTypeDIV ? "div" : "p";
            //int idescpos = site.IndexOf("<"+ty+" class=", modpos);
            //idescpos = site.IndexOf(">", idescpos) + 2;
            //int idescend = site.IndexOf("</"+ty+">", idescpos);
            //string inlinedesc = site.Substring(idescpos, idescend - idescpos).Trim(); // Get inline description
            //modInfo.InlineDescription = inlinedesc; // INLINE DESCRIPTION


            //Log("Found mod in Github: " + mod, Color.LightBlue);
            //listViewCompactMods.Items.Add(modInfo.GetListViewItem(listViewCompactMods.Groups[1], false));

        }

        private string DigLink(ref string Source, int SearchPosition)
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

        private void ChangeModSources(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                switch (e.Index)
                {
                    case 0:
                        new Task(GetGitMods).Start();
                        break;
                    case 1:
                        break;
                }
            }
            else
            {
                for (int i = listViewCompactMods.Groups[e.Index + 1].Items.Count - 1; i >= 0; i--)
                {
                    var item = listViewCompactMods.Groups[e.Index + 1].Items[i];
                    listViewCompactMods.Items.Remove(item);
                    AllMods.Remove(item.SubItems[4].Text);
                }
            }
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
            var modInfo = GetModInfoFromIndex(e.Index);
            if (modInfo.State == ModInfo.ModState.Server)
            {
                e.NewValue = CheckState.Unchecked;
                return;
            }
            if (modInfo.State != ModInfo.ModState.Disabled)
            {
                if (listViewCompactMods.SelectedItems.Count != 0 && e.Index == listViewCompactMods.SelectedItems[0].Index)
                    comboBoxModState.SelectedIndex = e.NewValue == CheckState.Checked ? 0 : 1;
                else
                    ChangeLocalModState(modInfo, e.NewValue == CheckState.Checked ? ModInfo.ModState.Enabled : ModInfo.ModState.Load);
            }
            else if (modInfo.State == ModInfo.ModState.Disabled && e.NewValue == CheckState.Checked)
            {
                ChangeLocalModState(modInfo, e.NewValue == CheckState.Checked ? ModInfo.ModState.Enabled : ModInfo.ModState.Load);
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
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

        private int GetCurrentIndex()
        {
            return listViewCompactMods.SelectedItems[0].Index;
        }

        private ModInfo GetModInfoFromIndex(int Index = -1)
        {
            if (Index == -1)
            {
                return AllMods[listViewCompactMods.SelectedItems[0].SubItems[4].Text];
            }
            return AllMods[listViewCompactMods.Items[Index].SubItems[4].Text];
        }

        private void OpenModLink(object sender, EventArgs e)
        {
            Process.Start(labelModLink.Text);
        }


        void Foo()
        {

        }


        private void ChangeLocalModState(int Index, ModInfo.ModState newState)
        {
            ModInfo modInfo = GetModInfoFromIndex(Index);
            ChangeLocalModState(modInfo, newState);
        }

        private void ChangeLocalModState(ModInfo modInfo, ModInfo.ModState newState)
        {
            if (newState == modInfo.State)
                return;
            if (newState == ModInfo.ModState.Disabled)
            {
                SetLocalModDisabled(ref modInfo.FilePath, true);
                Log("Disabled " + modInfo.Name, Color.DarkGreen);
            }
            else
            {
                if (modInfo.State == ModInfo.ModState.Disabled)
                {
                    SetLocalModDisabled(ref modInfo.FilePath, false);
                    Log("Enabled " + modInfo.Name, Color.DarkGreen);
                }
                SetLocalModState(modInfo.FilePath, newState);
                Log("Set " + modInfo.Name + " to " + newState.ToString(), Color.DarkSeaGreen);
                if (newState == ModInfo.ModState.Enabled && modInfo.RequiredModNames != null)
                    foreach (var modrequired in modInfo.RequiredModNames)
                    {
                        if (AllMods.TryGetValue(modrequired.Substring(modrequired.LastIndexOf('/') + 1), out ModInfo value))
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
                        Downloads.Remove(AllMods[modInfo.CloudName]);
                        Directory.Delete(modInfo.FilePath, true);
                        Log("Cancelled update; Deleted " + modInfo.Name + "...", Color.DarkRed);
                    }
                }
                else
                {
                    Directory.Delete(modInfo.FilePath, true);
                    Log("Deleted " + modInfo.Name + "...", Color.DarkRed);
                }
                AllMods.Remove(modInfo.Name);
                listViewCompactMods.Items.RemoveAt(GetCurrentIndex());
            }
        }

        #region Download Mods

#warning Fix downloading existent local mod

        private void GetModFromCloud(object sender, EventArgs e)
        {
            var modInfo = GetModInfoFromIndex();
            if (modInfo.State == ModInfo.ModState.Server)
            {
                AddModDownload(modInfo);
                return;
            }
            if (modInfo.CloudName != null && modInfo.CloudName.Length > 0 && AllMods.TryGetValue(modInfo.CloudName, out ModInfo cloudModInfo))
            {
                if (modInfo.State == ModInfo.ModState.Disabled)
                {
                    MessageBox.Show("Can't update a disabled mod!", "Download Mod", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (AddModDownload(cloudModInfo))
                {
                    var item = listViewCompactMods.SelectedItems[0];
                    item.SubItems[2].Text = "Updating...";
                    UpdatingItems.Add(item);
                }
            }
        }

        private bool AddModDownload(ModInfo ModBeingDownloaded, ListViewItem itemToRemove = null)
        {
            List<ModInfo> neededmods = new List<ModInfo>();
            foreach (var modname in ModBeingDownloaded.RequiredModNames)
            {
                if (AllMods.TryGetValue(modname, out ModInfo modInfo))
                {
                    if (!AllMods.ContainsKey(modInfo.Name))
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

                    throw new Exception("Download was killed at end of download!");
                }
                File.WriteAllText(RootFolder + @"\QMods\" + newFolder + @"\ttmm.json", JsonConvert.SerializeObject(AllMods[param[1]]));
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
            RunPatcher.RunExe("-i");
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

        }

        private void hideProgramUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            hideProgramUpdatesToolStripMenuItem.Checked = !hideProgramUpdatesToolStripMenuItem.Checked;
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
        public ModState State;

        [JsonIgnore]
        public string FilePath;

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

        public enum ModState : byte
        {
            Enabled,
            Load,
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
            TerraTechModManager.Downloader.DownloadFolder.Download("https://github.com/QModManager/TerraTech/tree/master/Installer", "/QModManager/TerraTech", ManagedPath);
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
                    RunPatcher.EXE.WaitForExit(5000);
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
