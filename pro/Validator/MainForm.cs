﻿using System;
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

namespace PeachValidator
{
	public partial class MainForm : Form
	{
		public Dictionary<string, string> DefinedValues = new Dictionary<string, string>();
		string windowTitle = "Peach Validator v" + Assembly.GetAssembly(typeof(Peach.Core.Engine)).GetName().Version;
		string windowTitlePit = "Peach Validator v" + Assembly.GetAssembly(typeof(Peach.Core.Engine)).GetName().Version + " - {0}";
		string windowTitlePitSample = "Peach Validator v" + Assembly.GetAssembly(typeof(Peach.Core.Engine)).GetName().Version + " - {0} - {1}";
		string windowTitleSample = "Peach Validator v" + Assembly.GetAssembly(typeof(Peach.Core.Engine)).GetName().Version + " - None - {0}";
		public string sampleFileName = null;
		public string pitFileName = null;
		Dictionary<string, object> parserArgs = new Dictionary<string, object>();
		CrackModel crackModel = new CrackModel();
		Dictionary<DataElement, CrackNode> crackMap = new Dictionary<DataElement, CrackNode>();
	    private MemoryTarget logTarget = null;

		public MainForm()
		{
			InitializeComponent();

			setTitle();
			AddNewDefine("Peach.Pwd=" + Utilities.ExecutionDirectory);
           var nconfig = new LoggingConfiguration();
            logTarget = new MemoryTarget();
            nconfig.AddTarget("console", logTarget);
            logTarget.Layout = "${logger} ${message}";

		    var rule = new LoggingRule("*", LogLevel.Debug, logTarget);
            nconfig.LoggingRules.Add(rule);

            LogManager.Configuration = nconfig;
		}

		public void AddNewDefine(string value)
		{
			if (value.IndexOf("=") < 0)
				throw new PeachException("Error, defined values supplied via -D/--define must have an equals sign providing a key-pair set.");

			var kv = value.Split('=');
			DefinedValues[kv[0]] = kv[1];
		}

		protected void setTitle()
		{
			if (!string.IsNullOrEmpty(sampleFileName) && !string.IsNullOrEmpty(pitFileName))
				Text = string.Format(windowTitlePitSample, Path.GetFileName(pitFileName), Path.GetFileName(sampleFileName));
			else if (string.IsNullOrEmpty(sampleFileName) && !string.IsNullOrEmpty(pitFileName))
				Text = string.Format(windowTitlePit, Path.GetFileName(pitFileName));
			else if (!string.IsNullOrEmpty(sampleFileName) && string.IsNullOrEmpty(pitFileName))
				Text = string.Format(windowTitleSample, Path.GetFileName(sampleFileName));
			else
				Text = windowTitle;
		}

		private void toolStripButtonOpenSample_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;

			sampleFileName = ofd.FileName;
			setTitle();

			DynamicFileByteProvider dynamicFileByteProvider;
			dynamicFileByteProvider = new DynamicFileByteProvider(new FileStream(sampleFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
			hexBox1.ByteProvider = dynamicFileByteProvider;

			toolStripButtonRefreshSample_Click(null, null);
		}

		private void toolStripButtonRefreshSample_Click(object sender, EventArgs e)
		{
			var cursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
            var holder = (DataModelHolder)toolStripComboBoxDataModel.SelectedItem;
			try
			{
                textBoxLogs.Clear();

				if (holder == null || string.IsNullOrEmpty(sampleFileName) || string.IsNullOrEmpty(pitFileName))
					return;

				byte[] buff;
				using (Stream sin = new FileStream(sampleFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					buff = new byte[sin.Length];
					sin.Read(buff, 0, buff.Length);
				}
				
				treeViewAdv1.BeginUpdate();
				treeViewAdv1.Model = null;

                try
				{
					BitStream data = new BitStream(buff);
					DataCracker cracker = new DataCracker();
					cracker.EnterHandleNodeEvent += new EnterHandleNodeEventHandler(cracker_EnterHandleNodeEvent);
					cracker.ExitHandleNodeEvent += new ExitHandleNodeEventHandler(cracker_ExitHandleNodeEvent);
					cracker.AnalyzerEvent += new AnalyzerEventHandler(cracker_AnalyzerEvent);
					cracker.ExceptionHandleNodeEvent += new ExceptionHandleNodeEventHandler(cracker_ExceptionHandleNodeEvent);
					//cracker.CrackData(dom.dataModels[dataModel], data);
					cracker.CrackData(holder.MakeCrackModel(), data);
				}
				catch (CrackingFailure ex)
				{
					MessageBox.Show("Error cracking \"" + ex.element.fullName + "\".\n" + ex.Message, "Error Cracking");

					long endPos = -1;
					foreach (var element in exceptions)
					{
						CrackNode currentModel;
						if (crackMap.TryGetValue(element, out currentModel))
						{
							currentModel.Error = true;

							if (endPos == -1)
								endPos = currentModel.StartBits;

							currentModel.StopBits = endPos;

							if (element.parent != null && crackMap.ContainsKey(element.parent))
								crackMap[element.parent].Children.Add(currentModel);
						}
					}
				}

				foreach (var node in crackMap.Values)
				{
					if (node.DataElement.parent != null)
						node.Parent = crackMap[node.DataElement.parent];
				}

				crackModel.Root = crackMap.Values.First().Root;
				treeViewAdv1.Model = crackModel;
				treeViewAdv1.EndUpdate();
				treeViewAdv1.Root.Children[0].Expand();

				// No longer needed
				crackMap.Clear();

                // Display debug logs
                textBoxLogs.AppendText(string.Join("\r\n", logTarget.Logs)); 
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
			if (!crackMap.TryGetValue(element, out currentModel))
				return;

			if (element.parent != null && crackMap.ContainsKey(element.parent))
				crackMap[element.parent].Children.Remove(currentModel);
			crackMap.Remove(element);

			// Remove any elements that have 'element' as a parent
			var res = crackMap.Select(kv => kv.Key).Where(k => k.parent == element).ToList();
			foreach (var elem in res)
			{
				RemoveElement(elem);
			}
		}

		List<DataElement> exceptions = new List<DataElement>();

		void cracker_ExceptionHandleNodeEvent(DataElement element, long position, BitStream data, Exception e)
		{
			if (!crackMap.ContainsKey(element))
			{
				// If offsets can't be figured out - we will get a crack exception
				// before getting a begin element.
				crackMap.Add(element, new CrackNode(crackModel, element, position, 0));
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

			var currentModel = crackMap[element];
			currentModel.StopBits = position;

			if (element.parent != null && crackMap.ContainsKey(element.parent))
				crackMap[element.parent].Children.Add(currentModel);
			else
			{
				// TODO -- Need to handle this case!
			}
		}

		void cracker_EnterHandleNodeEvent(DataElement element, long position, BitStream data)
		{
			crackMap[element] = new CrackNode(crackModel, element, position, 0);
		}

		private void toolStripButtonOpenPit_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Title = "Select PIT file";

			if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;

			pitFileName = ofd.FileName;
			setTitle();

			Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(pitFileName));
			AddNewDefine("Peach.Cwd=" + Environment.CurrentDirectory);


			Regex re = new Regex("##\\w+##");
			if (File.Exists(pitFileName) && re.IsMatch(File.ReadAllText(pitFileName)))
			{
                List<KeyValuePair<string, string>> defs;
                if (File.Exists(pitFileName + ".config"))
                {
                    defs = PitParser.parseDefines(pitFileName + ".config");
                }
                else
                {
                    ofd.Title = "Select PIT defines file";

                    if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                        return;

                    defs = PitParser.parseDefines(ofd.FileName);
                }

				foreach (var kv in defs)
				{
					// Allow command line to override values in XML file.
					if (!DefinedValues.ContainsKey(kv.Key))
						DefinedValues.Add(kv.Key, kv.Value);
				}
			}

			parserArgs[PitParser.DEFINED_VALUES] = DefinedValues;
			toolStripButtonRefreshPit_Click(null, null);
		}


        private void addDataModels(Dom dom, string ns)
        {
            if (!string.IsNullOrEmpty(ns))
                ns += ":";

            foreach (var otherNs in dom.ns)
                addDataModels(otherNs, ns + dom.name);

			var name = dom.name;

			if (!string.IsNullOrEmpty(name))
				name += ":";

            foreach (var dm in dom.dataModels)
                toolStripComboBoxDataModel.Items.Add(new DataModelHolder(dm, ns + name +dm.name));
        }

		private void toolStripButtonRefreshPit_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(pitFileName))
				return;

			try
			{
				PitParser parser = new PitParser();
				Dom dom;

				if (!string.IsNullOrWhiteSpace(Path.GetDirectoryName(pitFileName)))
					Directory.SetCurrentDirectory((DefinedValues["Peach.Cwd"]));

				dom = parser.asParser(parserArgs, pitFileName);

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
			if (pitFileName != null)
				toolStripButtonRefreshPit_Click(null, null);

			if (sampleFileName != null)
			{
				DynamicFileByteProvider dynamicFileByteProvider;
				dynamicFileByteProvider = new DynamicFileByteProvider(new FileStream(sampleFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
				hexBox1.ByteProvider = dynamicFileByteProvider;

				toolStripButtonRefreshSample_Click(null, null);
			}
		}
	}
}
