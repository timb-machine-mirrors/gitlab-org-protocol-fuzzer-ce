//
// Copyright (c) Peach Fuzzer, LLC
//

#if MONO
using Mono.Data.Sqlite;

using SQLiteCommand = Mono.Data.Sqlite.SqliteCommand;
using SQLiteConnection = Mono.Data.Sqlite.SqliteConnection;
using SQLiteParameter = Mono.Data.Sqlite.SqliteParameter;
#else
using System.Data.SQLite;
#endif
using System;
using System.IO;
using NLog;
using Peach.Core;
using Peach.Core.Dom;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("SampleNinja")]
	[Description("Will use existing samples to generate mutated files.")]
	public class SampleNinja : Mutator
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		readonly int _count = 0;
		uint pos = 0;
		readonly Guid ElementId = Guid.Empty;
		readonly string NinjaDB = null;

		public SampleNinja(DataElement obj)
			: base(obj)
		{
			var pitFile = GetPitFile(obj);
			NinjaDB = Path.GetFullPath(pitFile) + ".ninja";

			using (var Connection = new SQLiteConnection("data source=" + NinjaDB))
			{
				Connection.Open();

				// Get the total number of elements we can generate for this data element.
				using (var cmd = new SQLiteCommand(Connection))
				{
					cmd.CommandText = @"select count('x'), e.elementid from element e, sampleelement se where e.name = ? and se.elementid = e.elementid";

					cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));

					if(obj.parent is Peach.Core.Dom.Array)
						cmd.Parameters[0].Value = obj.parent.Name;
					else
						cmd.Parameters[0].Value = obj.Name;

					using (var reader = cmd.ExecuteReader())
					{
						if (!reader.Read())
							throw new PeachException(string.Format("Error, failed to find element in sample ninja db: [{0}]",
								obj.Name));

						_count = reader.GetInt32(0);
						ElementId = reader.GetGuid(1);
					}
				}
			}
		}

		public override uint mutation
		{
			get { return pos; }
			set { pos = value; }
		}

		public override int count
		{
			get { return _count; }
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			var pitFile = GetPitFile(obj);
			if (pitFile == null)
			{
				logger.Trace("no pit file specified in run configuration, disabing mutator.");
				return false;
			}

			var ninjaDb = Path.GetFullPath(pitFile) + ".ninja";

			// If our database doesn't exist JETTISON!
			if (!File.Exists(ninjaDb))
			{
				logger.Trace("ninja database not found, disabling mutator. \"" + ninjaDb + "\".");
				return false;
			}

			if (!obj.isMutable) return false;

			using (var Connection = new SQLiteConnection("data source=" + ninjaDb))
			{
				Connection.Open();

				// Get the total number of elements we can generate for this data element.
				using (var cmd = new SQLiteCommand(Connection))
				{
					cmd.CommandText = @"select count('x') from element e where e.name = ?";

					cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));

					// For arrays, normalize to wrapper name
					if (obj.parent is Peach.Core.Dom.Array)
						cmd.Parameters[0].Value = obj.parent.Name;
					else
						cmd.Parameters[0].Value = obj.Name;

					var ret = cmd.ExecuteScalar();

					if(ret != null && (Int64)ret > 0)
						return true;

					logger.Trace("Element \"" + obj.fullName + "\" not found in ninja db, not enabling.");
					return false;
				}
			}
		}

		public byte[] GetAt(uint index)
		{
			using (var Connection = new SQLiteConnection("data source=" + NinjaDB))
			{
				Connection.Open();

				// Get the total number of elements we can generate for this data element.
				using (var cmd = new SQLiteCommand(Connection))
				{
					cmd.CommandText = 
						"select se.data from sampleelement se "+
						"where se.elementid = ? LIMIT 1 OFFSET " + pos + ";";

					cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
					cmd.Parameters[0].Value = ElementId;

					var reader = cmd.ExecuteReader();
					if (!reader.Read())
						throw new PeachException(string.Format("SampleNinjaMutator error getting back row.  Position: {0} Count: {1}",
							pos, count));

					return (byte[])reader[0];
				}
			}
		}

		public override void sequentialMutation(DataElement obj)
		{
			obj.MutatedValue = new Variant(GetAt(pos));
			obj.mutationFlags = MutateOverride.Default;
			obj.mutationFlags |= MutateOverride.TypeTransform;
		}

		public override void randomMutation(DataElement obj)
		{
			var index = context.Random.Next(count);
			obj.MutatedValue = new Variant(GetAt((uint)index));
			obj.mutationFlags = MutateOverride.Default;
			obj.mutationFlags |= MutateOverride.TypeTransform;
		}

		private static string GetPitFile(DataElement elem)
		{
			var root = elem.getRoot() as DataModel;
			if (root == null)
				return null;

			var dom = root.actionData.action.parent.parent.parent;
			return dom.context.config.pitFile;
		}
	}
}
