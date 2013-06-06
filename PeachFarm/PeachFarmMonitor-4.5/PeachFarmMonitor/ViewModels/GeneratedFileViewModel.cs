using PeachFarm.Common.Mongo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PeachFarmMonitor.ViewModels
{
	public class GeneratedFileViewModel : GeneratedFile
	{
		public GeneratedFileViewModel(GeneratedFile file)
		{
			this.Name = file.Name;
			this.GridFsLocation = file.GridFsLocation;
		}
	}
}