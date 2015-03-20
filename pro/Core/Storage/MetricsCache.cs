using System.Collections.Generic;
using System.Linq;

namespace Peach.Pro.Core.Storage
{
	internal class MetricsCache
	{
		interface IMetricCache
		{
			long Add(JobContext db, string name);
		}

		class MetricCache<T> : IMetricCache
			where T : Metric, new()
		{
			readonly Dictionary<string, T> _map = new Dictionary<string, T>();

			public MetricCache(JobContext db)
			{
				_map = db.LoadTable<T>().ToDictionary(x => x.Name);
			}

			public long Add(JobContext db, string name)
			{
				T entity;
				if (!_map.TryGetValue(name, out entity))
				{
					entity = new T { Name = name };
					db.InsertMetric(entity);
					_map.Add(name, entity);
				}
				return entity.Id;
			}
		}

		readonly Dictionary<string, IMetricCache> _metrics;

		public MetricsCache(JobContext db)
		{
			_metrics = new Dictionary<string, IMetricCache>
			{
				{ typeof(State).Name, new MetricCache<State>(db) },
				{ typeof(Action).Name, new MetricCache<Action>(db) },
				{ typeof(Parameter).Name, new MetricCache<Parameter>(db) },
				{ typeof(Element).Name, new MetricCache<Element>(db) },
				{ typeof(Mutator).Name, new MetricCache<Mutator>(db) },
				{ typeof(Dataset).Name, new MetricCache<Dataset>(db) },
			};
		}

		public long Add<T>(JobContext db, string name)
		{
			return _metrics[typeof(T).Name].Add(db, name);
		}
	}
}
