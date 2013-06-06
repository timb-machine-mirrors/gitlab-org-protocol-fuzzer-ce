
//
// Copyright (c) Deja vu Security
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;

using Irony.Ast;
using Irony.Parsing;
using Irony.Interpreter;
using Irony.Interpreter.Ast;

using NLog;

namespace Godel.Core.OCL.Ast
{
	/// <summary>
	/// Methods defined by the lanauge are implemented in this class
	/// </summary>
	public class KnownMethods
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public static int abs(int num)
		{
			return Math.Abs(num);
		}

		public static double floor(double num)
		{
			return Math.Floor(num);
		}

		public static int size(string str)
		{
			return str.Length;
		}

		public static string concat(string str1, string str2)
		{
			return str1 + str2;
		}

		public static string substring(string str1, int start, int len)
		{
			return str1.Substring(start, len);
		}

		public static int xor(int i1, int i2)
		{
			return i1 ^ i2;
		}
	}
}

// end
