using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Sql;
using System.Data.SQLite;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Reflection;

using Peach.Core;
using Peach.Core.IO;
using Peach.Core.Dom;
using Peach.Core.Cracker;
using Peach.Core.Analyzers;

namespace PeachSampleNinja
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine();
			Console.WriteLine(">> Peach Sample Ninja -- Sample Scanner");
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

		public Program(string pitfile, string datamodel, string samplefolder)
		{
			var parser = new PitParser();
			var parserArgs = new Dictionary<string, object>();
			var dom = parser.asParser(parserArgs, pitfile);

			Model = dom.dataModels[datamodel];

			var database = Path.Combine(
				Path.GetDirectoryName(pitfile),
				Path.GetFileNameWithoutExtension(pitfile) + ".db");

			if (!File.Exists(database))
			{
				// Create database
				var assembly = Assembly.GetExecutingAssembly();
				using(var dbStream = assembly.GetManifestResourceStream("SampleNinja.db"))
				using (var fout = File.OpenWrite(database))
				{
					dbStream.CopyTo(fout);
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

			foreach (var file in Directory.EnumerateFiles(samplefolder))
			{
				var crackedModel = ProcessSample(file);
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

		/// <summary>
		/// Check to see if a sample file has already been processed.
		/// </summary>
		/// <remarks>
		/// Checks file name and file hash.
		/// </remarks>
		/// <param name="fileName"></param>
		/// <returns></returns>
		bool FileAlreadyProcessed(string fileName)
		{
			using (var cmd = new SQLiteCommand(Connection))
			{
				cmd.CommandText = "select sampleid, hash from sample where file = ?";
				SampleId = Guid.NewGuid();
				cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));
				cmd.Parameters[0].Value = fileName;

				var reader = cmd.ExecuteReader();
				if (!reader.NextResult())
					return false;

				var fileHash = Hash(fileName);
				var dbHash = (byte[])reader[0];

				return fileHash.SequenceEqual<byte>(dbHash);
			}
		}

		DataModel ProcessSample(string fileName)
		{
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

		void ProcessContainer(DataElementContainer container)
		{
			object elementId;
			var name = container.fullName;

			Console.WriteLine(name);

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
				cmd.Parameters[3].Value = container.Value.Value;

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
						cmd.Parameters[3].Value = child.Value.Value;

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
