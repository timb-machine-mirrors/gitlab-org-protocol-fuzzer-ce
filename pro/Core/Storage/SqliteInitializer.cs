using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using Peach.Core;
using Dapper;
using FileInfo = System.IO.FileInfo;
#if MONO
using Mono.Data.Sqlite;
using SQLiteConnection = Mono.Data.Sqlite.SqliteConnection;
#else
using System.Data.SQLite;
using System.ComponentModel;
#endif

namespace Peach.Pro.Core.Storage
{
	class SqliteInitializer
	{
		static SqliteInitializer()
		{
			//SQLiteLog.Enabled = true;
			//SQLiteLog.Log += (s, e) => Console.WriteLine("SQLiteLog: {0}", e.Message);
		}

		readonly bool _dbExists;

		public SqliteInitializer(string dbPath)
		{
			var fi = new FileInfo(dbPath);
			_dbExists = fi.Exists && fi.Length > 0;
		}

		public void InitializeDatabase(
			SQLiteConnection cnn,
			IEnumerable<Type> types,
			IEnumerable<string> scripts)
		{
			if (_dbExists)
				return;

			using (var xact = cnn.BeginTransaction())
			{
				try
				{
					CreateDatabase(cnn, types);

					scripts.ForEach(x => cnn.Execute(x));

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
			public bool IsUnique { get; set; }
			public string Name { get; set; }
			public string Table { get; set; }
			public List<string> Columns { get; set; }
		}

		class ForeignKey
		{
			public string TargetTable { get; set; }
			public HashSet<string> SourceColumns { get; set; }
			public HashSet<string> TargetColumns { get; set; }
		}

		const string TableTmpl = "CREATE TABLE {0} (\n{1}\n);";
		const string ColumnTmpl = "    {0} {1} {2}"; // name, type, decl
		const string DefaultTmpl = " DEFAULT ({0})";
		const string PrimaryKeyTmpl = "    PRIMARY KEY ({0})";
		const string ForeignKeyTmpl = "    FOREIGN KEY ({0}) REFERENCES {1} ({2})";
		const string IndexTmpl = "CREATE{0}INDEX {1} ON {2} ({3});";

		void CreateDatabase(SQLiteConnection cnn, IEnumerable<Type> types)
		{
			var indicies = new Dictionary<string, Index>();

			foreach (var type in types)
			{
				var defs = new List<string>();
				var keys = new HashSet<string>();
				var foreignKeys = new Dictionary<string, ForeignKey>();

				foreach (var pi in type.GetProperties())
				{
					if (pi.HasAttribute<NotMappedAttribute>())
						continue;

					var decls = new HashSet<string>();

					if (!pi.IsNullable())
						decls.Add("NOT NULL");
					if (pi.HasAttribute<UniqueAttribute>())
						decls.Add("UNIQUE");
					var defaultValue = pi.GetDefaultValue();
					if (defaultValue != null)
						decls.Add(DefaultTmpl.Fmt(defaultValue));

					defs.Add(ColumnTmpl.Fmt(
						pi.Name,
						pi.GetSqlType(cnn),
						string.Join(" ", decls)));

					if (pi.IsPrimaryKey())
						keys.Add(pi.Name);

					foreach (var attr in pi.AttributesOfType<IndexAttribute>())
					{
						Index index;
						if (!indicies.TryGetValue(attr.Name, out index))
						{
							index = new Index
							{
								IsUnique = attr.IsUnique,
								Name = attr.Name,
								Table = type.Name,
								Columns = new List<string>(),
							};
							indicies.Add(index.Name, index);
						}
						index.Columns.Add(pi.Name);
					}

					foreach (var attr in pi.AttributesOfType<ForeignKeyAttribute>())
					{
						var targetTable = attr.TargetEntity.Name;
						var targetColumn = attr.TargetProperty;

						ForeignKey foreignKey;
						if (!foreignKeys.TryGetValue(targetTable, out foreignKey))
						{
							foreignKey = new ForeignKey
							{
								TargetTable = targetTable,
								SourceColumns = new HashSet<string>(),
								TargetColumns = new HashSet<string>(),
							};
							foreignKeys.Add(targetTable, foreignKey);
						}
		
						foreignKey.SourceColumns.Add(pi.Name);
						foreignKey.TargetColumns.Add(targetColumn);
					}
				}

				if (keys.Any())
					defs.Add(string.Format(PrimaryKeyTmpl, string.Join(", ", keys)));

				foreach (var item in foreignKeys.Values)
				{
					defs.Add(ForeignKeyTmpl.Fmt(
						string.Join(", ", item.SourceColumns),
						item.TargetTable,
						string.Join(", ", item.TargetColumns)));
				}

				var sql = TableTmpl.Fmt(type.Name, string.Join(",\n", defs));
				cnn.Execute(sql);
			}

			foreach (var index in indicies.Values)
			{
				var columns = string.Join(", ", index.Columns);
				var isUnique = index.IsUnique ? " UNIQUE " : " ";
				var sql = IndexTmpl.Fmt(isUnique, index.Name, index.Table, columns);
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
		const string SqlDateTime = "DATETIME";

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
			{ typeof(DateTime), SqlDateTime },
			
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

		public static string GetDefaultValue(this PropertyInfo pi)
		{
			var attr = pi.GetCustomAttributes(true)
				.OfType<DefaultValueAttribute>()
				.SingleOrDefault();
			if (attr == null)
				return null;
			if (attr.Value is string)
				return "\"{0}\"".Fmt(attr.Value);
			return attr.Value.ToString();
		}
	}
}
