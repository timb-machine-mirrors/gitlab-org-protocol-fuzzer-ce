﻿
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

#if MONO
using Mono.Data.Sqlite;

using SQLiteCommand = Mono.Data.Sqlite.SqliteCommand;
using SQLiteConnection = Mono.Data.Sqlite.SqliteConnection;
using SQLiteParameter = Mono.Data.Sqlite.SqliteParameter;
#else
using System.Data.SQLite;
#endif

using Peach.Core.Dom;
using Peach.Core;

using NLog;

namespace Peach.Enterprise.Mutators
{
	[Mutator("SampleNinjaMutator")]
	[Description("Will use existing samples to generate mutated files.")]
	public class SampleNinjaMutator : Mutator
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		int _count = 0;
		uint pos = 0;
		Guid ElementId = Guid.Empty;

		string NinjaDB = null;

		public SampleNinjaMutator(DataElement obj)
			: base(obj)
        {
            name = "SampleNinja";

			var pitFile = GetPitFile(obj);
			NinjaDB = Path.Combine(
				Path.GetFullPath(pitFile),
				Path.GetFileName(pitFile) + ".ninja");

            using (var Connection = new SQLiteConnection("data source=" + NinjaDB))
            {
                Connection.Open();

                // Get the total number of elements we can generate for this data element.
                using (var cmd = new SQLiteCommand(Connection))
                {
                    cmd.CommandText = @"
select from count('x'), se.elementid
	from definition d, sample s, samplelement se, element e
	where d.Name = ?
	and e.name = ?
	and s.definitionid = d.definitionid
	and se.sampleid = s.sampleid
	and se.elementid = e.elementid
";

                    cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));
                    cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));
					cmd.Parameters[0].Value = GetPitFile(obj);
                    cmd.Parameters[1].Value = obj.fullName;

					using (var reader = cmd.ExecuteReader())
					{
						reader.NextResult();
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
			var ninjaDb = Path.Combine(
				Path.GetFullPath(pitFile),
				Path.GetFileName(pitFile) + ".ninja");

			// If our database doesn't exist JETTISON!
			if (!File.Exists(ninjaDb))
			{
				logger.Trace("ninja database not found, disabling mutator. \"" + ninjaDb + "\".");
				return false;
			}

			if (obj.isMutable)
			{
				using (var Connection = new SQLiteConnection("data source=" + ninjaDb))
				{
					Connection.Open();

					// Get the total number of elements we can generate for this data element.
					using (var cmd = new SQLiteCommand(Connection))
					{
						cmd.CommandText = @"
	select from count('x') 
		from definition d, sample s, samplelement se, element e
		where d.Name = ?
		and e.name = ?
		and s.definitionid = d.definitionid
		and se.sampleid = s.sampleid
		and se.elementid = e.elementid
	";

						cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));
						cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.String));
						cmd.Parameters[0].Value = GetPitFile(obj);
						cmd.Parameters[1].Value = obj.fullName;

						if ((int)cmd.ExecuteScalar() > 0)
						{
							logger.Trace("element \"" + obj.fullName + "\" not found in ninja db, not enabling.");
							return true;
						}
					}
				}
			}

			return false;
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
						"select from se.data from samplelement se "+
						"where se.elementid = ? LIMIT 1 OFFSET " + pos + ";";

					cmd.Parameters.Add(new SQLiteParameter(System.Data.DbType.Guid));
					cmd.Parameters[0].Value = ElementId;

					var reader = cmd.ExecuteReader();
					if (!reader.NextResult())
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
			int index = context.Random.Next(count);
			obj.MutatedValue = new Variant(GetAt((uint)index));
			obj.mutationFlags = MutateOverride.Default;
			obj.mutationFlags |= MutateOverride.TypeTransform;
		}

		private static string GetPitFile(DataElement elem)
		{
			var root = elem.getRoot() as DataModel;
			var dom = root.action.parent.parent.parent as Peach.Core.Dom.Dom;
			return dom.context.config.pitFile;
		}
	}
}

// end
