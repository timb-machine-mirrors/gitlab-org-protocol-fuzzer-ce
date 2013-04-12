
//
// Copyright (c) Deja vu Security
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Irony.Parsing;
using Irony.Interpreter;
using Irony.Interpreter.Ast;

using Godel.Core.OCL.Parser;

namespace Godel.Core.OCL
{
	/// <summary>
	/// OCL Factory for creating OclContext instances from OCL scripts
	/// </summary>
	public class Ocl
	{
		private Ocl()
		{
		}

		/// <summary>
		/// Convert OCL script into list of OclContexts
		/// </summary>
		/// <param name="script"></param>
		/// <returns></returns>
		public static List<OclContext> ParseOcl(string script)
		{
			var language = new LanguageData(new OCLGrammar());
			var runtime = new LanguageRuntime(language);
			var app = new ScriptApp(runtime);
			app.ParserMode = ParseMode.File;
			app.RethrowExceptions = true;

			var parser = new Irony.Parsing.Parser(language);
			var parseTree = parser.Parse(script);

			List<OclContext> list = new List<OclContext>();

			foreach (AstNode node in ((AstNode)parseTree.Root.AstNode).ChildNodes)
			{
				list.Add(new OclContext(node as Ast.ConstraintNode, app));
			}

			return list;
		}
	}
}
