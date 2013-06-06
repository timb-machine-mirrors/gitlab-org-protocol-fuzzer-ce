
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
	public class IfThenElseNode : OclAstNode
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		
		OclAstNode ifNode;
		OclAstNode thenNode;
		OclAstNode elseNode;

		public override void Init(AstContext context, ParseTreeNode treeNode)
		{
			base.Init(context, treeNode);
			logger.Trace("Init");

			ifNode = new ExpressionNode();
			ifNode.Init(context, treeNode.ChildNodes[1]);

			thenNode = new ExpressionNode();
			thenNode.Init(context, treeNode.ChildNodes[3]);

			elseNode = new ExpressionNode();
			elseNode.Init(context, treeNode.ChildNodes[5]);

			ChildNodes.Add(ifNode);
			ChildNodes.Add(thenNode);
			ChildNodes.Add(elseNode);

			AsString = "If-Then-Else";
			if (ChildNodes.Count == 0)
			{
				AsString += " (Empty)";
			}
			else
				ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
		}

		protected override object DoEvaluate(ScriptThread thread)
		{
			return Expression.Condition(
				(Expression)ifNode.Evaluate(thread),
				(Expression)thenNode.Evaluate(thread),
				(Expression)elseNode.Evaluate(thread));
		}
	}
}

// end
