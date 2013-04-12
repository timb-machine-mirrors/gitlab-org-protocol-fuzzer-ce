
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
	public class ExpressionNode : OclAstNode
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public override void Init(AstContext context, ParseTreeNode treeNode)
		{
			base.Init(context, treeNode);
			logger.Trace("Init");

			var nodes = treeNode.ChildNodes[0].GetMappedChildNodes();

			// Handle first expression
			var relationalExpression = new RelationalExpressionNode();
			relationalExpression.Init(context, nodes[0]);

			if (nodes.Count > 1 && nodes[1].ChildNodes.Count > 0)
			{
				// Handle tree
				var logicalNode = LogicalExpressionNode.LogicalExpressionFactory(context, nodes[1].ChildNodes[0]);
				logicalNode.Left = relationalExpression;
				logicalNode.Right = HandleRecusiveOptList(context, nodes[1]);
				ChildNodes.Add(logicalNode);
			}
			else
				ChildNodes.Add(relationalExpression);

			AsString = "Expression";
			if (ChildNodes.Count == 0)
			{
				AsString += " (Empty)";
			}
			else
				ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
		}

		public OclAstNode HandleRecusiveOptList(AstContext context, ParseTreeNode optList)
		{
			var relationalExpression = new RelationalExpressionNode();
			relationalExpression.Init(context, optList.ChildNodes[1]);

			if (optList.ChildNodes.Count == 3)
			{
				var logicalNode = LogicalExpressionNode.LogicalExpressionFactory(context, optList.ChildNodes[2].ChildNodes[0]);
				logicalNode.Left = relationalExpression;
				logicalNode.Right = HandleRecusiveOptList(context, optList.ChildNodes[2]);

				return logicalNode;
			}

			return relationalExpression;
		}

		protected override object DoEvaluate(ScriptThread thread)
		{
			return ChildNodes[0].Evaluate(thread);
		}
	}
}

// end
