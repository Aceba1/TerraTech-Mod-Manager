using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;

namespace TerraTechModManager
{
    public partial class Start : Form
    {
#warning Change Version_Number with every update!
        public const string Version_Number = "1.4";

        bool finished = false;
        /// <summary>
        /// Key: skipstart
        /// </summary>
        public static bool SkipAtStart = false;
        /// <summary>
        /// Key: ttroot
        /// </summary>
        string RootPath = @"C:\Program Files (x86)\Steam\steamapps\common\TerraTech Beta";

        public Start()
        {
            ConfigHandler.LoadConfig();
            ConfigHandler.TryGetValue(ref SkipAtStart, "skipstart");
            ConfigHandler.TryGetValue(ref RootPath, "ttroot");
            string txt = "Location of TerraTech";
            if (SkipAtStart)
            txt = BootMain();
            InitializeComponent();
            V_Num.Text = Version_Number;
            label2.Text = txt;
            textBoxFolderDirectory.Text = RootPath;
        }

        private void OpenMain_Click(object sender, EventArgs e)
        {
            RootPath = textBoxFolderDirectory.Text;
            label2.Text = BootMain();
            if (finished)
                Close();
        }

        private void Start_Load(object sender, EventArgs e)
        {
            if (finished)
                Close();
        }

        private string BootMain()
        {
            string rootpath = RootPath;
            if (Directory.Exists(rootpath))
            {
                int file = System.IO.Directory.GetFiles(rootpath, "TerraTech*.exe", System.IO.SearchOption.TopDirectoryOnly).Length;
                string datafolder = "";
                if (file != 0)
                {
                    bool flag = false;
                    foreach (var folder in new System.IO.DirectoryInfo(rootpath).GetDirectories())
                        if (File.Exists(folder.FullName + @"\Managed\Assembly-CSharp.dll"))
                        {
                            datafolder = folder.FullName;
                            flag = true;
                            break;
                        }
                    if (flag)
                    {
                        if (CheckPiracy())
                        {
                            ConfigHandler.ResetConfig();
                            this.Close();
                            return "Cancelled Operation";
                        }
                        ConfigHandler.SetValue(rootpath, "ttroot");
                        NewMain Main = new NewMain()
                        {
                            RootFolder = rootpath,
                            DataFolder = datafolder,
                        };
                        ShowInTaskbar = false;
                        TopMost = false;
                        Enabled = false;
                        Visible = false;
                        //load settings from json
                        Main.ShowDialog();
                        finished = true;
                    }
                }
                if (!finished)
                {
                    return "Location of TerraTech (Invalid)";
                }
            }
            else
            {
                return "Location of TerraTech (Nonexistent)";
            }
            return "";
        }

        void FolderDirectory_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (System.IO.Directory.Exists(files[0]))
                    textBoxFolderDirectory.Text = files[0];
            }
        }

        void FolderDirectory_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void textBoxFolderDirectory_TextChanged(object sender, EventArgs e)
        {
            label2.Text = "Location of TerraTech";
        }

        private bool CheckPiracy()
        {
            bool Check = new DirectoryInfo(RootPath).GetFiles("*igg*").Length > 0;
            Check = Check || new DirectoryInfo(RootPath).GetFiles("*ogg*").Length > 0;
            if (!Check)
            {
                var f = new DirectoryInfo(RootPath).GetFiles("valve.ini");
                if (f.Length > 0)
                {
                    string check = File.ReadAllText(f[0].FullName).ToLower();
                    Check = check.Contains("igg");
                    Check = Check || check.Contains("ogg");
                }
            }
            if (Check)
            {
                Enabled = false;
                Warning warning = new Warning();
                warning.ShowDialog();
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public static class ConfigHandler
    {
        private static Dictionary<string, object> config = new Dictionary<string, object>();

        public static void ResetConfig()
        {
            string path = Environment.CurrentDirectory + @"\config.json";
            File.Delete(path);
        }

        public static void LoadConfig()
        {
            string path = Environment.CurrentDirectory + @"\config.json";
            if (File.Exists(path))
                try
                {
                    config = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(path), new JsonSerializerSettings() { MissingMemberHandling = MissingMemberHandling.Ignore });
                }
                catch
                {
                    NewMain.StartMessage += "There is a problem with the config.json file\n";
                }
        }
        public static void SaveConfig()
        {
            string path = Environment.CurrentDirectory + @"\config.json";
            File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
        }
        public static void TryGetValue<T>(ref T obj, string Key)
        {
            if (config.TryGetValue(Key, out object newval))
            {
                obj = (T)newval;
                return;
            }
            config[Key] = obj;
        }

        public static T TryGetValue<T>(string Key, T Default)
        {
            object cache = null;
            if (config.TryGetValue(Key, out cache))
            {

                try
                {
                    return (T)cache;
                }
                catch
                {
                    try
                    {
                        return (T)Convert.ChangeType(cache, typeof(T));
                    }
                    catch
                    {
                        return ((Newtonsoft.Json.Linq.JObject)cache).ToObject<T>();
                    }
                }

            }
            config[Key] = Default;
            return Default;
        }

        public static void SetValue(object obj, string Key)
        {
            config[Key] = obj;
        }
    }
}
