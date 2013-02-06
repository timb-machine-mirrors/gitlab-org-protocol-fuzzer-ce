using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Xml.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Builders;
using System.Security.Cryptography;

namespace PeachFarm.Common.Messages
{
	public partial class StartPeachResponse
	{

		public StartPeachResponse(StartPeachRequest request)
		{
			this.JobID = request.JobID;
			this.PitFileName = request.PitFileName;
			this.Success = true;
			this.UserName = request.UserName;
		}
	}

	public partial class StopPeachResponse
	{
		public StopPeachResponse(StopPeachRequest request)
		{
			this.JobID = request.JobID;
			this.PitFileName = request.PitFileName;
			this.Success = true;
			this.UserName = request.UserName;
		}
	}
}
