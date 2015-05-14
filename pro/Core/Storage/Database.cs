using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
#if MONO
using Mono.Data.Sqlite;
using SQLiteConnection = Mono.Data.Sqlite.SqliteConnection;
using SQLiteCommand = Mono.Data.Sqlite.SqliteCommand;
#else
using System.Data.SQLite;
#endif
using System.Linq;
using Peach.Core;
using Dapper;

namespace Peach.Pro.Core.Storage
{
	public abstract class Database : IDisposable
	{
		class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
		{
			public override DateTime Parse(object value)
			{
				// Both Mono.Data.Sqlite and System.Data.SQLite ADO.NET implementations
				// return DateTimes with DateTimeKind.Unspecified.  However, if a timezone
				// is specified in the database, System.Data.SQLite will convert it
				// to local time (but leave the type as Unspecified) and Mono.Data.Sqlite
				// will fail to parse and throw an exception.

				// To work around this we are intentionally saving DateTimes w/o any time zone
				// indication, meaning on both ADO.NET providers they are going to come
				// in as DateTimeKind.Unspecified and actually be in UTC.
				// Explicitly mark them as UTC and then return them as Local.

				var dt = Convert.ToDateTime(value);

				System.Diagnostics.Debug.Assert(dt.Kind == DateTimeKind.Unspecified);

				return DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
			}

			public override void SetValue(IDbDataParameter parameter, DateTime value)
			{
				// Mono.Data.Sqlite does not support time zones in its ISO8601
				// DateTime parsing code.  This means that Mono.Data.Sqlite does not
				// include the UTC timezone identifier in the database when saving, whereas
				// System.Data.SQLite does.  In order to make sure our database can be read
				// by both Mono.Data.Sqlite and System.Data.SQLite we need to manually convert
				// times to a compatible ISO8601 UTC time w/o a time zone marker and
				// insert as a string.

				parameter.DbType = DbType.String;
				parameter.Value = value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");
			}
		}

		static Database()
		{
			// Use custom type handler for storing DateTime objects in sqlite.
			SqlMapper.AddTypeHandler(new DateTimeHandler());

			// Work around an issue with Dapper where custom type handlers
			// don't get called when sending values into sqlite (SetValue codepath).
			// https://github.com/StackExchange/dapper-dot-net/issues/206
			// https://github.com/StackExchange/dapper-dot-net/pull/177

			var fi = typeof(SqlMapper).GetField("typeMap", BindingFlags.Static | BindingFlags.NonPublic);
			if (fi == null)
				throw new InvalidOperationException("SqlMapper is missing typeMap member.");

			var typeMap = (Dictionary<Type, DbType>)fi.GetValue(null);

			typeMap.Remove(typeof(DateTime));
			typeMap.Remove(typeof(DateTime?));
		}

		public string Path { get; private set; }
		public SQLiteConnection Connection { get; private set; }

		protected abstract IEnumerable<Type> Schema { get; }
		protected abstract IEnumerable<string> Scripts { get; }

		protected Database(string path, bool useWal)
		{
			Path = path;

			var parts = new List<string>
			{
				"Data Source=\"{0}\"".Fmt(Path),
				"Foreign Keys=True",
			};
			if (useWal)
				parts.Add("PRAGMA journal_mode=WAL");

			var cnnString = string.Join(";", parts);
			Connection = new SQLiteConnection(cnnString);
			Connection.Open();

			if (!IsInitialized)
				Initialize();
		}

		public void Dispose()
		{
			if (Connection != null)
				Connection.Dispose();
		}

		public void Initialize()
		{
			SqliteInitializer.InitializeDatabase(Connection, Schema, Scripts);
		}

		public bool IsInitialized
		{
			get
			{
				var fi = new System.IO.FileInfo(Path);
				return fi.Exists && fi.Length > 0;
			}
		}

		public IEnumerable<T> LoadTable<T>()
		{
			var type = typeof(T);

			var attr = type.GetCustomAttributes(typeof(TableAttribute), true)
				.OfType<TableAttribute>()
				.FirstOrDefault();
	
			var table = (attr != null) ? attr.Name : type.Name;
			var sql = "SELECT * FROM {0}".Fmt(table);
			return Connection.Query<T>(sql);
		}

		public static void Dump<T>(IEnumerable<T> data)
		{
			var type = typeof(T);

			var columns = type.GetProperties()
				.Where(pi => !pi.HasAttribute<NotMappedAttribute>())
				.ToList();

			var maxWidth = new int[columns.Count];
			var header = new string[columns.Count];
			var rows = new List<string[]> { header };

			for (var i = 0; i < columns.Count; i++)
			{
				var pi = columns[i];
				header[i] = pi.Name;
				maxWidth[i] = pi.Name.Length;
			}

			foreach (var item in data)
			{
				var row = new string[columns.Count];
				var values = columns.Select(pi =>
				{
					var value = pi.GetValue(item, null);
					return value == null ? "<NULL>" : value.ToString();
				}).ToArray();
				for (var i = 0; i < values.Length; i++)
				{
					var value = values[i];
					row[i] = value;
					maxWidth[i] = Math.Max(maxWidth[i], value.Length);
				}
				rows.Add(row);
			}

			var fmts = maxWidth
				.Select((t, i) => "{0},{1}".Fmt(i, t))
				.Select(fmt => "{" + fmt + "}")
				.ToList();
			var finalFmt = string.Join("|", fmts);
			foreach (object[] row in rows)
			{
				Console.Error.WriteLine(finalFmt, row);
			}
		}

		internal DateTime SelectDateTime(string commandText)
		{
			using (var cmd = new SQLiteCommand(commandText, Connection))
			{
				return Convert.ToDateTime(cmd.ExecuteScalar());
			}
		}
	}
}
