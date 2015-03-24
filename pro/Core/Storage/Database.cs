using System;
using System.Collections.Generic;
#if MONO
using Mono.Data.Sqlite;
using SQLiteConnection = Mono.Data.Sqlite.SqliteConnection;
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
		public string Path { get; private set; }
		public SQLiteConnection Connection { get; private set; }

		protected abstract IEnumerable<Type> Schema { get; }
		protected abstract IEnumerable<string> Scripts { get; }

		protected Database(string path)
		{
			Path = path;

			var parts = new[]
			{
				"Data Source=\"{0}\"".Fmt(Path),
				"Foreign Keys=True",
				"PRAGMA journal_mode=WAL",
			};

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
			var sql = "SELECT * FROM {0}".Fmt(typeof(T).Name);
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
	}
}
