
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
	public class OclExpressionNode : OclAstNode
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public bool hasLetExpression = false;
		public LabelTarget ReturnTarget = Expression.Label();

		public override void Init(AstContext context, ParseTreeNode treeNode)
		{
			base.Init(context, treeNode);
			logger.Trace("Init");

			var nodes = treeNode.GetMappedChildNodes();

			// Handle let?
			if (nodes[0].ChildNodes.Count > 0)
			{
				var let = new LetExpressionNode();
				let.Init(context, nodes[0].ChildNodes[0]);
				ChildNodes.Add(let);
				hasLetExpression = true;
			}

			// Should always have an expression
			var expression = new ExpressionNode();
			expression.Init(context, nodes[1]);
			ChildNodes.Add(expression);

			AsString = "OCL Expression";
			if (hasLetExpression)
			{
				AsString += " (Has Let)";
			}

			if (ChildNodes.Count == 0)
			{
				AsString += " (Empty)";
			}
			else
				ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
		}

		protected override object DoEvaluate(ScriptThread thread)
		{
			logger.Trace("DoEvaluate");
			return ChildNodes[0].Evaluate(thread);
		}
	}
}

// end
