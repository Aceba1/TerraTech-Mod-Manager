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
            if (SkipAtStart)
            BootMain();
            InitializeComponent();
            textBoxFolderDirectory.Text = RootPath;
        }

        private void OpenMain_Click(object sender, EventArgs e)
        {
            RootPath = textBoxFolderDirectory.Text;
            BootMain();
            if (finished)
                Close();
        }

        private void Start_Load(object sender, EventArgs e)
        {
            if (finished)
                Close();
        }

        private void BootMain()
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
                    label2.Text = "Location of TerraTech (Invalid)";
                }
            }
            else
            {
                label2.Text = "Location of TerraTech (Nonexistent)";
            }
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
    }

    public static class ConfigHandler
    {
        private static Dictionary<string, object> config = new Dictionary<string, object>();

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
