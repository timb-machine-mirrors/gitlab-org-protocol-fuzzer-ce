using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	public class Report
	{
		public string Title
		{
			get { return "Fuzzing Report for " + Job.Name; }
		}

		public string Author
		{
			// TODO: Use AssemblyProduct
			get { return "Peach Fuzzer v" + Job.PeachVersion; }
		}

		public string Company
		{
			// TODO: Use AssemblyCompanyAttribute
			get { return "Peach Fuzzer, LLC"; }
		}

		public string Result
		{
			get { return Job.FaultCount == 0 ? "PASSED" : "FAILED"; }
		}

		public Job Job { get; set; }

		public ICollection<BucketDetail> BucketDetails { get; set; }
		public ICollection<MutatorMetric> MutatorMetrics { get; set; }
		public ICollection<ElementMetric> ElementMetrics { get; set; }
		public ICollection<StateMetric> StateMetrics { get; set; }
		public ICollection<DatasetMetric> DatasetMetrics { get; set; }
		public ICollection<BucketMetric> BucketMetrics { get; set; }
	}
}
