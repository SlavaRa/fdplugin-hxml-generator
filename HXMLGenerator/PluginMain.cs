using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PluginCore;
using PluginCore.Helpers;
using PluginCore.Localization;
using PluginCore.Managers;
using PluginCore.Utilities;
using ProjectManager;
using ProjectManager.Controls.TreeView;
using ProjectManager.Helpers;
using ProjectManager.Projects.Haxe;

namespace HXMLGenerator
{
    public class PluginMain : IPlugin
	{
        string settingFilename;
        TreeView projectTree;

        #region Required Properties

        /// <summary>
        /// Api level of the plugin
        /// </summary>
        public int Api => 1;

        /// <summary>
        /// Name of the plugin
        /// </summary> 
        public string Name => "HXMLGenerator";

        /// <summary>
        /// GUID of the plugin
        /// </summary>
        public string Guid => "59be851a-6030-4fd9-9422-50f2071c7446";

        /// <summary>
        /// Author of the plugin
        /// </summary> 
        public string Author => "SlavaRa";

        /// <summary>
        /// Description of the plugin
        /// </summary> 
        public string Description => "HXML Generator Plugin For FlashDevelop";

        /// <summary>
        /// Web address for help
        /// </summary> 
        public string Help => "https://github.com/SlavaRa/fdplugin-hxml-generator";

        /// <summary>
        /// Object that contains the settings
        /// </summary>
        [Browsable(false)]
        public object Settings { get; private set; }
		
		#endregion
		
		#region Required Methods
		
		/// <summary>
		/// Initializes the plugin
		/// </summary>
		public void Initialize()
		{
            InitBasics();
            LoadSettings();
            AddEventHandlers();
		}

	    /// <summary>
		/// Disposes the plugin
		/// </summary>
		public void Dispose() => SaveSettings();

        /// <summary>
        /// Adds the required event handlers
        /// </summary>
        public void AddEventHandlers() => EventManager.AddEventHandler(this, EventType.Command);

        /// <summary>
        /// Handles the incoming events
        /// </summary>
        public void HandleEvent(object sender, NotifyEvent e, HandlingPriority priority)
        {
            if (e.Type != EventType.Command) return;
            switch (((DataEvent) e).Action)
            {
                case ProjectManagerEvents.Project:
                    if (PluginBase.CurrentProject is HaxeProject)
                        DirectoryNode.OnDirectoryNodeRefresh += OnDirectoryNodeRefresh;
                    break;
                case ProjectManagerEvents.TreeSelectionChanged:
                    OnTreeSelectionChanged();
                    break;
            }
        }

        #endregion

        #region Custom Private Methods

        /// <summary>
        /// Initializes important variables
        /// </summary>
        void InitBasics()
        {
            var path = Path.Combine(PathHelper.DataDir, nameof(HXMLGenerator));
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            settingFilename = Path.Combine(path, "Settings.fdb");
        }

        /// <summary>
        /// Loads the plugin settings
        /// </summary>
        void LoadSettings()
        {
            Settings = new Settings();
            if (!File.Exists(settingFilename)) SaveSettings();
            else Settings = (Settings) ObjectSerializer.Deserialize(settingFilename, Settings);
        }

        /// <summary>
        /// Saves the plugin settings
        /// </summary>
        void SaveSettings() => ObjectSerializer.Serialize(settingFilename, Settings);

        void GenerateHXMLBuildFile(string output)
        {
            var project = (HaxeProject) PluginBase.CurrentProject;
            var paths = ProjectManager.PluginMain.Settings.GlobalClasspaths.ToArray();
            var args = project.BuildHXML(paths, output, true).ToList();
            args.RemoveAll(it =>
            {
                var line = it.Trim();
                return line.Length == 0 
                    || line == "-D no-compilation"
                    || line == "-D display";
            });
            FileHelper.WriteFile(output, "", Encoding.UTF8);
            using (var writer = new StreamWriter(output, false))
            {
                writer.WriteLine($"## {project.TargetBuild}");
                args.ForEach(writer.WriteLine);
                writer.Close();
            }
        }

        #endregion

        #region Event Handlers

        void OnDirectoryNodeRefresh(DirectoryNode node) => projectTree = node.TreeView;

        void OnTreeSelectionChanged()
        {
            if (!(PluginBase.CurrentProject is HaxeProject)) return;
            if (!(projectTree?.SelectedNode is ProjectNode)) return;
            var item = ((ProjectContextMenu) projectTree.ContextMenuStrip).CloseProject;
            var index = projectTree.ContextMenuStrip.Items.IndexOf(item) + 1;
            item = new ToolStripMenuItem("&Generate hxml build...");
            item.Click += OnGenerateHXMLClick;
            projectTree.ContextMenuStrip.Items.Insert(index, item);
        }

        void OnGenerateHXMLClick(object sender, EventArgs e)
        {
            const string defaultOutput = "build.hxml";
            var label = TextHelper.GetString("ProjectManager.Label.FileName");
            var dialog = new LineEntryDialog("Generate hxml build file...", label, defaultOutput);
            if (dialog.ShowDialog() != DialogResult.OK) return;
            var output = dialog.Line.Trim();
            if (output.Length == 0)
            {
                var project = (HaxeProject)PluginBase.CurrentProject;
                output = Path.Combine(project.Directory, defaultOutput);
            }
            GenerateHXMLBuildFile(output);
        }

        #endregion
    }
}