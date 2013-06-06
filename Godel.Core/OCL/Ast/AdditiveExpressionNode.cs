
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
	public class AdditiveExpressionNode : OclAstNode
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public OclAstNode Left;
		public OclAstNode Right = null;
		public string Operand = null;

		public override void Init(AstContext context, ParseTreeNode treeNode)
		{
			base.Init(context, treeNode);
			logger.Trace("Init");

			var mult = new MultiplicativeExpressionNode();
			mult.Init(context, treeNode.ChildNodes[0]);
			Left = mult;
			ChildNodes.Add(Left);

			if (treeNode.ChildNodes[1].ChildNodes.Count > 0 && treeNode.ChildNodes[1].ChildNodes[0].ChildNodes.Count > 0)
			{
				mult = new MultiplicativeExpressionNode();
				mult.Init(context, treeNode.ChildNodes[1].ChildNodes[1]);
				Right = mult;
				Operand = treeNode.ChildNodes[1].ChildNodes[0].ChildNodes[0].Token.Text;
				ChildNodes.Add(Right);
			}

			AsString = "Additive Expression";
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

			// Evaluate both sides of the expression first.

			var left = (Expression)Left.Evaluate(thread);
			var right = (Expression)Right.Evaluate(thread);

			// Perform type conversion if required.

			var leftTypeCode = Type.GetTypeCode(left.Type);
			var rightTypeCode = Type.GetTypeCode(right.Type);

			if (leftTypeCode > rightTypeCode)
			{
				right = Expression.Convert(right, left.Type);
			}
			else
			{
				left = Expression.Convert(left, right.Type);
			}

			// Now do evaulation.

			if (Operand == "+")
				return Expression.Add(left, right);

			return Expression.Subtract(left, right);
		}
	}
}

// end
