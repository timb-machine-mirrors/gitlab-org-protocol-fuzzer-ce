using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Reflection;

using Peach.Core;
using Peach.Core.IO;
using Peach.Core.Dom;
using Peach.Core.Cracker;
using Peach.Core.Analyzers;

#if MONO
using Mono.Data.Sqlite;

using SQLiteCommand = Mono.Data.Sqlite.SqliteCommand;
using SQLiteConnection = Mono.Data.Sqlite.SqliteConnection;
using SQLiteParameter = Mono.Data.Sqlite.SqliteParameter;
#else
using System.Data.SQLite;
#endif

namespace PeachSampleNinja
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine();
			Console.WriteLine(">> Peach Pro Sample Ninja -- Sample Scanner");
			Console.WriteLine(">> Copyright (c) Deja vu Security\n");

			if (args.Length < 2)
			{
				Console.WriteLine(@"
This program will build the sample ninja database from a set sample files.  This
database is used to recombine sample files into new files during a Peach fuzzing
run.
");

				Console.WriteLine("Syntax: PeachSampleNinja <pitfile> <datamodel> <samplefolder>");
				return;
			}

			new Program(args[0], args[1], args[2]);
		}

		public DataModel FindDataModel(string name, Dom dom)
		{
			if (!name.Contains(':'))
			{
				if (!dom.dataModels.ContainsKey(name))
				{
					Console.WriteLine("\nError: Unable to find data model \"" + name + "\"!");
					Console.WriteLine("\nThe following data models were found:");

					throw new ApplicationException();
				}

				return dom.dataModels[name];
			}

			string [] parts = name.Split(':');

			for (int cnt = 0; cnt < parts.Length; cnt++)
			{
				if (dom.ns.ContainsKey(parts[cnt]))
				{
					dom = dom.ns[parts[cnt]];
					continue;
				}
				else if ((cnt + 1) != parts.Length)
				{
					Console.WriteLine("\nError: Unable to find namespace \"" + parts[0] + "\"!");

					throw new ApplicationException();
				}

				return FindDataModel(parts[cnt], dom);
			}

			return null;
		}

		public void PrintAllDataModels(Dom dom, string prefix = "")
		{
			foreach (string model in dom.dataModels.Keys)
				Console.WriteLine("\t" + prefix + model);

			foreach (string key in dom.ns.Keys)
				PrintAllDataModels(dom.ns[key], prefix + key + ":");
		}

		public Program(string pitfile, string datamodel, string samplefolder)
		{
			if (!File.Exists(pitfile))
			{
				Console.WriteLine("Error: Unable to find pit file.");
				return;
			}

			var config = Path.Combine(
				Path.GetDirectoryName(pitfile),
				Path.GetFileName(pitfile) + ".config");
			var DefinedValues = new Dictionary<string,string>();

			if (File.Exists(config))
			{
				Console.WriteLine("Using configuration file \"" + config + "\".");
				var defs = PitParser.parseDefines(config);

				foreach (var kv in defs)
				{
					// Allow command line to override values in XML file.
					if (!DefinedValues.ContainsKey(kv.Key))
						DefinedValues.Add(kv.Key, kv.Value);
				}
			}

			var parser = new PitParser();
			var parserArgs = new Dictionary<string, object>();
			parserArgs[PitParser.DEFINED_VALUES] = DefinedValues;

			var dom = parser.asParser(parserArgs, pitfile);

			try
			{
				Model = FindDataModel(datamodel, dom);
			}
			catch (Exception)
			{
				PrintAllDataModels(dom);
				return;
			}

			var database = Path.Combine(
				Path.GetDirectoryName(pitfile),
				Path.GetFileName(pitfile) + ".ninja");

			if (!File.Exists(database))
			{
				Console.WriteLine("Creating new sample database.");

				// Create database
				var assembly = Assembly.GetExecutingAssembly();
				using (var dbStream = assembly.GetManifestResourceStream("PeachSampleNinja.SampleNinja.db"))
				using (var fout = File.OpenWrite(database))
				{
					dbStream.CopyTo(fout);
					fout.Flush();
				}
			}

			Connection = new SQLiteConnection("data source=" + database);
			Connection.Open();

			using(var cmd = new SQLiteCommand(Connection))
			{
				cmd.CommandText = "insert into definition (definitionid, name) values (?,?)";
				DefinitionId = Guid.NewGuid();
				cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
				cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));
				//cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String, dom.version));
				//cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String, dom.author));

				cmd.Parameters[0].Value = DefinitionId;
				cmd.Parameters[1].Value = Path.GetFileName(pitfile);

				cmd.ExecuteNonQuery();
			}

			if (samplefolder.Contains('*'))
			{
				foreach (var file in Directory.GetFiles(Path.GetDirectoryName(samplefolder), Path.GetFileName(samplefolder)))
				{
					ProcessSample(file);
				}
			}
			else
			{
				foreach (var file in Directory.EnumerateFiles(samplefolder))
				{
					ProcessSample(file);
				}
			}

			Connection.Close();
		}

		SQLiteConnection Connection { get; set; }
		DataModel Model { get; set; }
		Guid DefinitionId { get; set; }
		Guid SampleId { get; set; }

		byte[] Hash(string filename)
		{
			using (var sha1 = new SHA1Managed())
			{
				return sha1.ComputeHash(File.ReadAllBytes(filename));
			}
		}

		bool FileAlreadyProcessed(string fileName)
		{
			using (var cmd = new SQLiteCommand(Connection))
			{
				cmd.CommandText = "select sampleid from sample where file = ?";
				SampleId = Guid.NewGuid();
				cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));
				cmd.Parameters[0].Value = fileName;

				var reader = cmd.ExecuteReader();
				return reader.NextResult();
			}
		}

		bool FileHashChanged(string fileName)
		{
			using (var cmd = new SQLiteCommand(Connection))
			{
				cmd.CommandText = "select hash from sample where file = ?";
				SampleId = Guid.NewGuid();
				cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));
				cmd.Parameters[0].Value = fileName;

				var reader = cmd.ExecuteReader();
				if (!reader.NextResult())
					throw new ApplicationException("Error, file not found");

				var fileHash = Hash(fileName);
				var dbHash = (byte[])reader[0];

				return fileHash.SequenceEqual<byte>(dbHash);
			}
		}

		Guid GetSampleId(string fileName)
		{
			using (var cmd = new SQLiteCommand(Connection))
			{
				cmd.CommandText = "select sampleid from sample where file = ?";
				SampleId = Guid.NewGuid();
				cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));
				cmd.Parameters[0].Value = fileName;

				var reader = cmd.ExecuteReader();
				if (!reader.NextResult())
					throw new ApplicationException("Error, file not found");

				return reader.GetGuid(0);
			}
		}

		DataModel ProcessSample(string fileName)
		{
			if (FileAlreadyProcessed(fileName))
			{
				if (FileHashChanged(fileName))
				{
					Console.WriteLine("Updating: " + fileName);

					// Remove old file information
					Guid sampleId = GetSampleId(fileName);

					using (var cmd = Connection.CreateCommand())
					{
						cmd.CommandText = "delete from sample where sampleid = ?";
						cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
						cmd.Parameters[0].Value = sampleId;
						cmd.ExecuteNonQuery();

						cmd.CommandText = "delete from element where sampleid = ?";
						cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
						cmd.Parameters[0].Value = sampleId;
						cmd.ExecuteNonQuery();

						cmd.CommandText = "delete from sampleelement where sampleid = ?";
						cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
						cmd.Parameters[0].Value = sampleId;
						cmd.ExecuteNonQuery();
					}
				}
				else
				{
					Console.WriteLine("Skipping: " + fileName);
					return null;
				}
			}
			else
				Console.WriteLine("Processing: " + fileName);

			try
			{
				var data = new BitStream(File.ReadAllBytes(fileName));
				var cracker = new DataCracker();
				var crackedModel = ObjectCopier.Clone<DataModel>(Model);

				cracker.CrackData(crackedModel, data);

				using (var cmd = new SQLiteCommand(Connection))
				{
					cmd.CommandText = "insert into sample (sampleid, file, definitionid, hash) values (?,?,?,?)";
					SampleId = Guid.NewGuid();
					cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
					cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));
					cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
					cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Binary));

					cmd.Parameters[0].Value = SampleId;
					cmd.Parameters[1].Value = fileName;
					cmd.Parameters[2].Value = DefinitionId;
					cmd.Parameters[3].Value = Hash(fileName);

					cmd.ExecuteNonQuery();
				}

				ProcessContainer(crackedModel);

				return crackedModel;
			}
			catch (CrackingFailure ex)
			{
				Console.WriteLine("Error cracking \"" + ex.element.fullName + "\".");
			}

			return null;
		}

		private static byte[] ToByteArray(BitwiseStream data)
		{
			data.SeekBits(0, SeekOrigin.Begin);

			int len = 0;
			int count = (int)data.Length;
			int offset = 0;
			byte[] buffer = new byte[count];

			while ((len = data.Read(buffer, offset, count)) != 0)
			{
				offset += len;
				count -= len;
			}

			System.Diagnostics.Debug.Assert(count == 0);

			return buffer;
		}

		void ProcessContainer(DataElementContainer container)
		{
			object elementId;
			var name = container.fullName;

			//Console.WriteLine(name);

			using (var cmd = new SQLiteCommand(Connection))
			{
				cmd.CommandText = "select elementid from element where name = ?";
				cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String, name));
				elementId = cmd.ExecuteScalar();

				if (elementId == null)
				{
					elementId = Guid.NewGuid();
					cmd.CommandText = "insert into element (elementid, name, sampleid) values (?, ?, ?)";
					cmd.Parameters.Clear();
					cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
					cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));
					cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));

					cmd.Parameters[0].Value = (Guid) elementId;
					cmd.Parameters[1].Value = name;
					cmd.Parameters[2].Value = SampleId;
					
					cmd.ExecuteNonQuery();
				}

				cmd.CommandText = "insert into SampleElement (SampleElementId, SampleId, ElementId, Data) values (?,?,?,?)";
				cmd.Parameters.Clear();
				cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
				cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
				cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
				cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Binary));

				cmd.Parameters[0].Value = Guid.NewGuid();
				cmd.Parameters[1].Value = SampleId;
				cmd.Parameters[2].Value = elementId;
				cmd.Parameters[3].Value = ToByteArray(container.Value);

				cmd.ExecuteNonQuery();
			}

			foreach (var child in container)
			{
				if (!(child is DataElementContainer))
				{
					using (var cmd = new SQLiteCommand(Connection))
					{
						cmd.CommandText = "select elementid from element where name = ?";
						cmd.Parameters.Clear();
						cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));
						cmd.Parameters[0].Value = child.name;
						elementId = cmd.ExecuteScalar();

						if (elementId == null)
						{
							elementId = Guid.NewGuid();
							cmd.CommandText = "insert into element (elementid, name, sampleid) values (?, ?, ?)";
							cmd.Parameters.Clear();
							cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
							cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));
							cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));

							cmd.Parameters[0].Value = (Guid)elementId;
							cmd.Parameters[1].Value = child.name;
							cmd.Parameters[2].Value = SampleId;

							cmd.ExecuteNonQuery();
						}

						cmd.CommandText = "insert into SampleElement (SampleElementId, SampleId, ElementId, Data) values (?,?,?,?)";
						cmd.Parameters.Clear();
						cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
						cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
						cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
						cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Binary));

						cmd.Parameters[0].Value = Guid.NewGuid();
						cmd.Parameters[1].Value = SampleId;
						cmd.Parameters[2].Value = elementId;
						cmd.Parameters[3].Value = ToByteArray(child.Value);

						cmd.ExecuteNonQuery();
					}
				}
				else
				{
					ProcessContainer(child as DataElementContainer);
				}
			}
		}
	}
}

// end
