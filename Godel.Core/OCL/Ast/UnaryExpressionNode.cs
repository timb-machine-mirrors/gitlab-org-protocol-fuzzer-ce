
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
	public class UnaryExpressionNode : OclAstNode
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public OclAstNode Left;
		public OclAstNode Right;
		public string Operand = null;

		public override void Init(AstContext context, ParseTreeNode treeNode)
		{
			base.Init(context, treeNode);
			logger.Trace("Init");

			if (treeNode.ChildNodes[0].ToString() == "postfixExpression")
			{
				var post = new PostfixExpressionNode();
				post.Init(context, treeNode.ChildNodes[0]);
				ChildNodes.Add(post);
			}
			else if (treeNode.ChildNodes.Count == 1)
			{
				var post = new PostfixExpressionNode();
				post.Init(context, treeNode.ChildNodes[0].ChildNodes[0]);
				ChildNodes.Add(post);
			}
			else
			{
				System.Diagnostics.Debug.Assert(treeNode.ChildNodes[1].ToString() == "postfixExpression");

				Operand = "-";
				var post = new PostfixExpressionNode();
				post.Init(context, treeNode.ChildNodes[1]);
				ChildNodes.Add(post);
			}

			AsString = "Unary Expression";
			if (Operand != null)
				AsString += " (" + Operand + ")";
			if (ChildNodes.Count == 0)
			{
				AsString += " (Empty)";
			}
			else
				ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
		}

		protected override object DoEvaluate(ScriptThread thread)
		{
			if (Operand == null)
				return ChildNodes[0].Evaluate(thread);

			var left = (Expression)ChildNodes[0].Evaluate(thread);

			if (Operand == "-")
				return Expression.MakeUnary(
					System.Linq.Expressions.ExpressionType.Negate,
					left,
					left.Type);

			return Expression.MakeUnary(
				System.Linq.Expressions.ExpressionType.Not,
				left,
				left.Type);
		}
	}
}

// end
