
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
	public class ContextNode : OclAstNode
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public string Name = "Unknown";

		public override void Init(AstContext context, ParseTreeNode treeNode)
		{
			base.Init(context, treeNode);
			logger.Trace("Init");

			RecursiveSetNoAstNode(treeNode.ChildNodes[0]);

			//Stereotype = treeNode.ChildNodes[1].ChildNodes[0].ChildNodes[0].Token.Text;
			Name = treeNode.ChildNodes[1].ChildNodes[0].ChildNodes[0].Token.Text;

			AsString = "Context (" + Name + ")";
		}
	}
}

// end
