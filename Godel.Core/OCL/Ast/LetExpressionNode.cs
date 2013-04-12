
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
	public class LetExpressionNode : OclAstNode
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public override void Init(AstContext context, ParseTreeNode treeNode)
		{
			base.Init(context, treeNode);
			logger.Trace("Init");

			// Todo - Add all logic here :)

			AsString = "Let Expression";
			if (ChildNodes.Count == 0)
			{
				AsString += " (Empty)";
			}
			else
				ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
		}
	}
}

// end
