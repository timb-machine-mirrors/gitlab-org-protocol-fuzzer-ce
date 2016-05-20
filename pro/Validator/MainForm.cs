using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Be.Windows.Forms;
using NLog.Config;
using NLog.Targets;
using Peach.Core.Dom;
using Peach.Core;
using Peach.Core.Cracker;
using Peach.Core.IO;
using Peach.Core.Analyzers;
using System.Reflection;
using System.Text.RegularExpressions;
using NLog;
using Peach.Pro.Core;

namespace PeachValidator
{
	public partial class MainForm : Form
	{
	    readonly string _windowTitle = "Peach Validator v" + Assembly.GetAssembly(typeof(Peach.Core.Engine)).GetName().Version;
	    readonly string _windowTitlePit = "Peach Validator v" + Assembly.GetAssembly(typeof(Peach.Core.Engine)).GetName().Version + " - {0}";
	    readonly string _windowTitlePitSample = "Peach Validator v" + Assembly.GetAssembly(typeof(Peach.Core.Engine)).GetName().Version + " - {0} - {1}";
	    readonly string _windowTitleSample = "Peach Validator v" + Assembly.GetAssembly(typeof(Peach.Core.Engine)).GetName().Version + " - None - {0}";
		public string SampleFileName = null;
		public string PitFileName = null;
		public string SaveFileName = null;
	    readonly Dictionary<string, object> _parserArgs = new Dictionary<string, object>();
		CrackModel crackModel = new CrackModel();
	    readonly Dictionary<DataElement, CrackNode> _crackMap = new Dictionary<DataElement, CrackNode>();
	    private readonly MemoryTarget logTarget = null;

		public MainForm()
		{
			InitializeComponent();

			setTitle();

           var nconfig = new LoggingConfiguration();
            logTarget = new MemoryTarget();
            nconfig.AddTarget("console", logTarget);
			logTarget.Layout = "${logger} ${message} ${exception:format=tostring}";

		    var rule = new LoggingRule("*", LogLevel.Debug, logTarget);
            nconfig.LoggingRules.Add(rule);

            LogManager.Configuration = nconfig;
		}

		protected void setTitle()
		{
			if (!string.IsNullOrEmpty(SampleFileName) && !string.IsNullOrEmpty(PitFileName))
				Text = string.Format(_windowTitlePitSample, Path.GetFileName(PitFileName), Path.GetFileName(SampleFileName));
			else if (string.IsNullOrEmpty(SampleFileName) && !string.IsNullOrEmpty(PitFileName))
				Text = string.Format(_windowTitlePit, Path.GetFileName(PitFileName));
			else if (!string.IsNullOrEmpty(SampleFileName) && string.IsNullOrEmpty(PitFileName))
				Text = string.Format(_windowTitleSample, Path.GetFileName(SampleFileName));
			else
				Text = _windowTitle;
		}

		private void toolStripButtonOpenSample_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;

			SampleFileName = ofd.FileName;
			setTitle();

			toolStripButtonRefreshSample_Click(null, null);
		}

		private void toolStripButtonRefreshSample_Click(object sender, EventArgs e)
		{
			var cursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
            var holder = (DataModelHolder)toolStripComboBoxDataModel.SelectedItem;
			try
			{
				// Clear the cracking debug logs
				textBoxLogs.Text = "";
				logTarget.Logs.Clear();

				if (holder == null || string.IsNullOrEmpty(SampleFileName) || string.IsNullOrEmpty(PitFileName))
					return;

				// Refresh the hex display in case the file has changed.
				DynamicFileByteProvider dynamicFileByteProvider;
				dynamicFileByteProvider = new DynamicFileByteProvider(new FileStream(SampleFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
				hexBox1.ByteProvider = dynamicFileByteProvider;

				using (Stream sin = new FileStream(SampleFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					treeViewAdv1.BeginUpdate();
					treeViewAdv1.Model = null;

					try
					{
						BitStream data = new BitStream(sin);
						DataCracker cracker = new DataCracker();
						cracker.EnterHandleNodeEvent += new EnterHandleNodeEventHandler(cracker_EnterHandleNodeEvent);
						cracker.ExitHandleNodeEvent += new ExitHandleNodeEventHandler(cracker_ExitHandleNodeEvent);
						cracker.AnalyzerEvent += new AnalyzerEventHandler(cracker_AnalyzerEvent);
						cracker.ExceptionHandleNodeEvent += new ExceptionHandleNodeEventHandler(cracker_ExceptionHandleNodeEvent);
						//cracker.CrackData(dom.dataModels[dataModel], data);

						try
						{
							var dm = holder.MakeCrackModel();
							cracker.CrackData(dm, data);

							if (!string.IsNullOrEmpty(SaveFileName))
							{
								using (var f = File.Open(SaveFileName, FileMode.Create))
								{
									var val = dm.Value;
									val.CopyTo(f);
								}
							}
						}
						catch (CrackingFailure ex)
						{
							throw new PeachException("Error cracking \"" + ex.element.fullName + "\".\n" + ex.Message);
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error Cracking");

						long endPos = -1;
						foreach (var element in exceptions)
						{
							CrackNode currentModel;
							if (_crackMap.TryGetValue(element, out currentModel))
							{
								currentModel.Error = true;

								if (endPos == -1)
									endPos = currentModel.StartBits;

								currentModel.StopBits = endPos;

								if (element.parent != null && _crackMap.ContainsKey(element.parent))
									_crackMap[element.parent].Children.Add(currentModel);
							}
						}
					}

					foreach (var node in _crackMap.Values)
					{
						if (node.DataElement.parent != null && _crackMap.ContainsKey(node.DataElement.parent))
							node.Parent = _crackMap[node.DataElement.parent];
					}

					crackModel.Root = _crackMap.Values.First().Root;
					treeViewAdv1.Model = crackModel;
					treeViewAdv1.EndUpdate();
					treeViewAdv1.Root.Children[0].Expand();

					// No longer needed
					_crackMap.Clear();

					// Display debug logs
					textBoxLogs.Text = string.Join("\r\n", logTarget.Logs);
					logTarget.Logs.Clear();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error cracking file: " + ex.ToString());
			}
			finally
			{
				Cursor.Current = cursor;
			}
		}

		void RemoveElement(DataElement element)
		{
			CrackNode currentModel;
			if (!_crackMap.TryGetValue(element, out currentModel))
				return;

			if (element.parent != null && _crackMap.ContainsKey(element.parent))
				_crackMap[element.parent].Children.Remove(currentModel);
			_crackMap.Remove(element);

			// Remove any elements that have 'element' as a parent
			var res = _crackMap.Select(kv => kv.Key).Where(k => k.parent == element).ToList();
		    foreach (var elem in res)
		        RemoveElement(elem);
		}

		List<DataElement> exceptions = new List<DataElement>();

		void cracker_ExceptionHandleNodeEvent(DataElement element, long position, BitStream data, Exception e)
		{
			if (!_crackMap.ContainsKey(element))
			{
				// If offsets can't be figured out - we will get a crack exception
				// before getting a begin element.
				_crackMap.Add(element, new CrackNode(crackModel, element, position, 0));
			}

			exceptions.Add(element);
		}

		void cracker_AnalyzerEvent(DataElement element, BitStream data)
		{
			RemoveElement(element);
		}

		void cracker_ExitHandleNodeEvent(DataElement element, long position, BitStream data)
		{
			foreach (var item in exceptions)
				RemoveElement(item);
			exceptions.Clear();

		    if (!_crackMap.ContainsKey(element))
		        return;

			var currentModel = _crackMap[element];
			currentModel.StopBits = position;

			if (element.parent != null && _crackMap.ContainsKey(element.parent))
				_crackMap[element.parent].Children.Add(currentModel);
			else
			{
				// TODO -- Need to handle this case!
			}
		}

		void cracker_EnterHandleNodeEvent(DataElement element, long position, BitStream data)
		{
			_crackMap[element] = new CrackNode(crackModel, element, position, 0);
		}

		private void toolStripButtonOpenPit_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Title = "Select PIT file";

			if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;

			SelectPit(ofd.FileName, true);
		}

		private void SelectPit(string fileName, bool getDefs)
		{
			PitFileName = fileName;
			setTitle();

			// Switch out working folder to pit location
			// this should allow us to resolve local data files
			var pitPath = Path.GetDirectoryName(fileName);
			if(Directory.Exists(pitPath))
				Directory.SetCurrentDirectory(pitPath);

			Regex re = new Regex("##\\w+##");
            List<KeyValuePair<string, string>> defs;
            if (File.Exists(PitFileName + ".config"))
            {
                defs = PitDefines.ParseFile(PitFileName + ".config").Evaluate();
            }
			else if (getDefs && File.Exists(PitFileName) && re.IsMatch(File.ReadAllText(PitFileName)))
            {
	            var ofd = new OpenFileDialog();
	            ofd.Title = "Select PIT defines file";

	            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
		            return;

				defs = PitDefines.ParseFile(ofd.FileName).Evaluate();
            }
            else
            {
                defs = new List<KeyValuePair<string, string>>();
            }

			_parserArgs[PitParser.DEFINED_VALUES] = defs;
			toolStripButtonRefreshPit_Click(null, null);
		}


        private void addDataModels(Dom dom, string ns)
        {
            if (!string.IsNullOrEmpty(ns))
                ns += ":";

            foreach (var otherNs in dom.ns)
				addDataModels(otherNs, ns + dom.Name);

			var name = dom.Name;

			if (!string.IsNullOrEmpty(name))
				name += ":";

            foreach (var dm in dom.dataModels)
				toolStripComboBoxDataModel.Items.Add(new DataModelHolder(dm, ns + name + dm.Name));
        }

		private void toolStripButtonRefreshPit_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(PitFileName))
				return;

			try
			{
				var parser = new PitParser();

				var dom = parser.asParser(_parserArgs, PitFileName);

				var previouslySelectedModelName = toolStripComboBoxDataModel.SelectedItem;

				toolStripComboBoxDataModel.Items.Clear();

                addDataModels(dom, "");

                var newModelIndex = -1;

                if (previouslySelectedModelName != null)
                    newModelIndex = toolStripComboBoxDataModel.Items.IndexOf(previouslySelectedModelName);

				if (newModelIndex < 0)
                    newModelIndex = toolStripComboBoxDataModel.Items.Count - 1;

				if (toolStripComboBoxDataModel.Items.Count > 0)
					toolStripComboBoxDataModel.SelectedIndex = newModelIndex;

				treeViewAdv1.BeginUpdate();
                var model = (DataModelHolder)toolStripComboBoxDataModel.Items[newModelIndex];
                treeViewAdv1.Model = CrackModel.CreateModelFromPit(model.DataModel);
				treeViewAdv1.EndUpdate();
				treeViewAdv1.Root.Children[0].Expand();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error loading file: " + ex.ToString());
			}
		}

        class DataModelHolder
        {
            public DataModel DataModel { get; set; }
            public string FullName { get; set; }

            public DataModelHolder(DataModel DataModel, string FullName)
            {
                this.DataModel = DataModel;
                this.FullName = FullName;
            }

			public DataModel MakeCrackModel()
			{
				var ret = (DataModel)DataModel.Clone();

				// Need to set the dom so scripting environments will work.
				ret.dom = DataModel.dom;

				return ret;
			}

            public override string ToString()
            {
                return FullName;
            }

            public override bool Equals(object obj)
            {
                var other = obj as DataModelHolder;

                if (other == null)
                    return false;

                return other.FullName == this.FullName;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }


		private void toolStripComboBoxDataModel_SelectedIndexChanged(object sender, EventArgs e)
		{
			try
			{
				var dataModel = (DataModelHolder)toolStripComboBoxDataModel.SelectedItem;

				treeViewAdv1.BeginUpdate();

                crackModel = CrackModel.CreateModelFromPit(dataModel.DataModel);
                treeViewAdv1.Model = crackModel;
				treeViewAdv1.EndUpdate();
				treeViewAdv1.Root.Children[0].Expand();
			}
			catch
			{
			}
		}

		private void treeViewAdv1_SelectionChanged(object sender, EventArgs e)
		{
			if (treeViewAdv1.SelectedNode == null)
				return;

			var node = (CrackNode)treeViewAdv1.SelectedNode.Tag;
			hexBox1.Select(node.StartBits / 8, (node.StopBits - node.StartBits + 7) / 8);
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			if (PitFileName != null)
			{
				SelectPit(PitFileName, false);
			}

			if (SampleFileName != null)
			{
				DynamicFileByteProvider dynamicFileByteProvider;
				dynamicFileByteProvider = new DynamicFileByteProvider(new FileStream(SampleFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
				hexBox1.ByteProvider = dynamicFileByteProvider;

				toolStripButtonRefreshSample_Click(null, null);
			}
		}
	}
}
