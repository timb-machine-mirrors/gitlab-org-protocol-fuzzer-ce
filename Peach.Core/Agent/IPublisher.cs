using Peach.Core.Dom;
using System.Collections.Generic;
using System.IO;

namespace Peach.Core.Agent
{
	public interface IPublisher
	{
		uint Iteration { set; }
		bool IsControlIteration { set; }
		string Result { get; }
		Stream Stream { get; }

		void Start();
		void Stop();
		void Open();
		void Close();
		void Accept();
		Variant Call(string method, List<ActionParameter> args);
		void SetProperty(string property, Variant value);
		Variant GetProperty(string property);
		void Output(DataModel data);
		void Input();
		void WantBytes(long count);
	}
}
