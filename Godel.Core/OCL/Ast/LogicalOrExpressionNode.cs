
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
	public class LogicalOrExpressionNode : LogicalExpressionNode
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public override void Init(AstContext context, ParseTreeNode treeNode)
		{
			base.Init(context, treeNode);
			logger.Trace("Init");

			Operator = "or";
			AsString = "Or";
		}

		public override void Init(AstContext context, ParseTreeNode treeNode, AstNode Left, AstNode Right)
		{
			base.Init(context, treeNode);
			logger.Trace("Init");

			this.Left = Left;
			this.Right = Right;

			Operator = treeNode.Token.Text;

			ChildNodes.Add(Left);
			ChildNodes.Add(Right);

			AsString = "Or";
		}

		protected override object DoEvaluate(ScriptThread thread)
		{
			return Expression.Or((Expression)Left.Evaluate(thread), (Expression)Right.Evaluate(thread));
		}
	}
}

// end
