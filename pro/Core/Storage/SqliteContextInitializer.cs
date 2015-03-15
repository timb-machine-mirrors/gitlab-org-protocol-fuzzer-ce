using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using Peach.Core;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Collections;

namespace Peach.Pro.Core.Storage
{
	class SqliteContextInitializer<T> : IDatabaseInitializer<T>
		where T : DbContext
	{
		bool _dbExists;
		HashSet<Type> _visited = new HashSet<Type>();

		public SqliteContextInitializer(string dbPath)
		{
			_dbExists = File.Exists(dbPath);
		}

		public void InitializeDatabase(T context)
		{
			if (_dbExists)
				return;

			using (var xact = context.Database.BeginTransaction())
			{
				typeof(T).GetProperties().ForEach(p =>
				{
					var type = p.PropertyType;
					if (type.IsGenericType && 
						type.GetGenericTypeDefinition() == typeof(DbSet<>))
					{
						ProcessType(context, type.GetGenericArguments().First());
					}
				});
				xact.Commit();
			}
		}

		private void ProcessType(T context, Type type)
		{
			if (_visited.Contains(type))
				return;
			_visited.Add(type);
			CreateTable(context, type).ForEach(t => ProcessType(context, t));
		}

		private ICollection<Type> CreateTable(T context, Type type)
		{
			var subTypes = new List<Type>();

			var columnTmpl = "    [{0}] {1} {2}"; // name, type, decl
			var tableTmpl = "CREATE TABLE [{0}] (\n{1}\n);";

			var columns = new List<string>();
			type.GetProperties().ForEach(p =>
			{
				if (HasAttribute<NotMappedAttribute>(p))
					return;

				if (IsCollection(p))
				{
					subTypes.Add(p.PropertyType.GetGenericArguments().First());
					return;
				}

				// check for this attribute after checking for IsCollection
				// since it's ok for [ForeignKey] to be used on a navigation property.
				if (HasAttribute<ForeignKeyAttribute>(p))
					return;

				var columnType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
				var sqlType = GetSqlType(p, columnType);

				var sqlDecls = new List<string>();
				if (IsPK(p))
					sqlDecls.Add("PRIMARY KEY");
				if (IsAutoIncrement(p, columnType))
					sqlDecls.Add("AUTOINCREMENT");
				if (!IsNullable(p))
					sqlDecls.Add("NOT NULL");

				columns.Add(columnTmpl.Fmt(p.Name, sqlType, string.Join(" ", sqlDecls)));
			});

			var sql = tableTmpl.Fmt(type.Name, string.Join(",\n", columns));
			context.Database.ExecuteSqlCommand(sql);

			return subTypes;
		}

		const string SqlInt = "INTEGER";
		const string SqlReal = "REAL";
		const string SqlText = "TEXT";
		const string SqlBlob = "BLOB";

		static Dictionary<Type, string> SqlTypeMap = new Dictionary<Type, string>
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
			{ typeof(Guid), SqlText },
			{ typeof(DateTime), SqlText },
			
			{ typeof(byte[]), SqlBlob },
		};

		private bool IsPK(PropertyInfo pi)
		{
			return pi.Name.ToLower() == "id" || HasAttribute<KeyAttribute>(pi);
		}

		private bool IsAutoIncrement(PropertyInfo pi, Type columnType)
		{
			var attrs = pi.GetCustomAttributes(typeof(DatabaseGeneratedAttribute), true);
			var attr = attrs.FirstOrDefault() as DatabaseGeneratedAttribute;
			var identity = attr != null ? attr.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity : false;
			return IsPK(pi) && SqlTypeMap[columnType] == SqlInt && identity;
		}

		private bool IsNullable(PropertyInfo pi)
		{
			if (IsPK(pi))
				return false;
			var type = pi.PropertyType;
			if (type == typeof(string))
				return true;
			return type.IsGenericType &&
				type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		private bool HasAttribute<TAttr>(PropertyInfo pi)
		{
			return pi.GetCustomAttributes(typeof(TAttr), true).Any();
		}

		private string GetSqlType(PropertyInfo pi, Type columnType)
		{
			if (columnType.IsEnum)
				return SqlInt;
			string sqlType;
			if (!SqlTypeMap.TryGetValue(columnType, out sqlType))
				Debug.Assert(false, "Missing column type map",
					"Missing column type map for property {0} with type {1}",
					pi.Name,
					columnType.Name);
			return sqlType;
		}

		private bool IsCollection(PropertyInfo pi)
		{
			var type = pi.PropertyType;
			return
				type.IsInstanceOfType(typeof(ICollection)) ||
				(type.IsGenericType() &&
					type.GetGenericTypeDefinition().IsAssignableFrom(typeof(ICollection<>)));
		}
	}
}
