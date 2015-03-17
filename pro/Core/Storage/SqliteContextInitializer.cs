using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Linq;
using Peach.Core;
using Dapper;

#if MONO
using Mono.Data.Sqlite;
using SQLiteConnection = Mono.Data.Sqlite.SqliteConnection;
#else
using System.Data.SQLite;
#endif

namespace Peach.Pro.Core.Storage
{
	class SqliteContextInitializer
	{
		static SqliteContextInitializer()
		{
			//SQLiteLog.Enabled = true;
			//SQLiteLog.Log += (s, e) => Console.WriteLine("SQLiteLog: {0}", e.Message);
		}

		readonly bool _dbExists;

		public SqliteContextInitializer(string dbPath)
		{
			_dbExists = File.Exists(dbPath);
		}

		public void InitializeDatabase(
			SQLiteConnection cnn,
			IEnumerable<Type> types)
		{
			if (_dbExists)
				return;

			using (var xact = cnn.BeginTransaction())
			{
				try
				{
					CreateDatabase(cnn, types);
					xact.Commit();
				}
				catch (Exception)
				{
					xact.Rollback();
					throw;
				}
			}
		}

		class Index
		{
			public string Name { get; set; }
			public string Table { get; set; }
			public List<string> Columns { get; set; }
		}

		const string TableTmpl = "CREATE TABLE [{0}] (\n{1}\n);";
		const string ColumnTmpl = "    [{0}] {1} {2}"; // name, type, decl
		const string PrimaryKeyTmpl = "    PRIMARY KEY ({0})";
		//const string foreignKeyTmpl = "    FOREIGN KEY ({0}) REFERENCES {1} ({2})";
		const string IndexTmpl = "CREATE INDEX {0} ON {1} ({2});";

		void CreateDatabase(SQLiteConnection cnn, IEnumerable<Type> types)
		{
			Console.WriteLine("CreateDatabase");

			var indicies = new Dictionary<string, Index>();
			//var foreignKeys = new Dictionary<string, string>();

			foreach (var type in types)
			{
				var defs = new List<string>();
				var keys = new HashSet<string>();

				foreach (var pi in type.GetProperties())
				{
					if (pi.HasAttribute<NotMappedAttribute>())
						continue;

					var decls = new HashSet<string>();

					if (!pi.IsNullable())
						decls.Add("NOT NULL");

					foreach (var attr in pi.AttributesOfType<IndexAttribute>())
					{
						if (attr.IsUnique)
							decls.Add("UNIQUE");

						if (string.IsNullOrEmpty(attr.Name))
							continue;

						Index index;
						if (!indicies.TryGetValue(attr.Name, out index))
						{
							index = new Index
							{
								Name = attr.Name,
								Table = type.Name,
								Columns = new List<string>(),
							};
							indicies.Add(index.Name, index);
						}
						index.Columns.Add(pi.Name);
					}

					defs.Add(ColumnTmpl.Fmt(
						pi.Name,
						pi.GetSqlType(cnn),
						string.Join(" ", decls)));

					if (pi.IsPrimaryKey())
						keys.Add(pi.Name);

					//foreach (var attr in pi.AttributesOfType<ForeignKeyAttribute>())
					//{
					//	foreignKeys.Add(attr.TargetEntity.Name, attr.TargetProperty);
					//}
				}

				if (keys.Any())
					defs.Add(string.Format(PrimaryKeyTmpl, string.Join(", ", keys)));

				var sql = TableTmpl.Fmt(type.Name, string.Join(",\n", defs));
				cnn.Execute(sql);

				//foreach (var item in foreignKeys)
				//{
				//	defs.Add(foreignKeyTmpl.Fmt(
				//		string.Join(", ", thisKeys),
				//		assoc.Constraint.FromRole.Name,
				//		string.Join(", ", thatKeys)));
				//}
			}

			foreach (var index in indicies.Values)
			{
				var columns = string.Join(", ", index.Columns);
				var sql = IndexTmpl.Fmt(index.Name, index.Table, columns);
				cnn.Execute(sql);
			}
		}
	}

	static class Extensions
	{
		const string SqlInt = "INTEGER";
		const string SqlReal = "REAL";
		const string SqlText = "TEXT";
		const string SqlBlob = "BLOG";

		static readonly Dictionary<Type, string> SqlTypeMap = new Dictionary<Type, string>
		{
			{ typeof(Boolean), SqlInt },

			{ typeof(Byte), SqlInt },
			{ typeof(SByte), SqlInt },
			{ typeof(Int16), SqlInt },
			{ typeof(Int32), SqlInt },
			{ typeof(Int64), SqlInt },
			{ typeof(UInt16), SqlInt },
			{ typeof(UInt32), SqlInt },
			{ typeof(UInt64), SqlInt },
			{ typeof(TimeSpan), SqlInt },
			{ typeof(DateTimeOffset), SqlInt },

			{ typeof(Single), SqlReal },
			{ typeof(Double), SqlReal },
			{ typeof(Decimal), SqlReal },

			{ typeof(string), SqlText },
			{ typeof(DateTime), SqlText },
			
			{ typeof(byte[]), SqlBlob },		
			{ typeof(Guid), SqlBlob },
		};

		public static bool HasAttribute<TAttr>(this PropertyInfo pi)
		{
			return pi.GetCustomAttributes(typeof(TAttr), true).Any();
		}

		public static IEnumerable<TAttr> AttributesOfType<TAttr>(this PropertyInfo pi)
		{
			return pi.GetCustomAttributes(true).OfType<TAttr>();
		}

		public static bool IsPrimaryKey(this PropertyInfo pi)
		{
			return pi.HasAttribute<KeyAttribute>();
		}

		public static bool IsNullable(this PropertyInfo pi)
		{
			if (IsPrimaryKey(pi))
				return false;
			var type = pi.PropertyType;
			if (type == typeof(string))
				return true;
			return type.IsGenericType &&
				type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		public static string GetSqlType(this PropertyInfo pi, SQLiteConnection cnn)
		{
			var type = pi.PropertyType;

			if (type.IsEnum)
				return SqlInt;

			if (type.IsGenericType &&
				type.GetGenericTypeDefinition() == typeof(Nullable<>))
				type = type.GetGenericArguments().First();

			string sqlType;
			if (!SqlTypeMap.TryGetValue(type, out sqlType))
				Debug.Assert(false, "Missing column type map",
					"Missing column type map for property {0} with type {1}",
					pi.Name,
					type.Name);
			return sqlType;
		}
	}
}
