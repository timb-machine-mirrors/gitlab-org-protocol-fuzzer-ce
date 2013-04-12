
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
	public class PrimaryExpressionNode : OclAstNode
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public override void Init(AstContext context, ParseTreeNode treeNode)
		{
			base.Init(context, treeNode);
			logger.Trace("Init");

			switch (treeNode.ChildNodes[0].ToString())
			{
				case "literalCollection":
					break;
				case "literal":
					var lit = new LiteralNode();
					lit.Init(context, treeNode.ChildNodes[0]);
					ChildNodes.Add(lit);
					break;
				case "propertyCall":
					var prop = new PropertyCallNode();
					prop.Init(context, treeNode.ChildNodes[0]);
					ChildNodes.Add(prop);
					break;
				case "ifExpression":
					var ifthenelse = new IfThenElseNode();
					ifthenelse.Init(context, treeNode.ChildNodes[0]);
					ChildNodes.Add(ifthenelse);
					break;
				default:
					//expression
					var expr = new ExpressionNode();
					expr.Init(context, treeNode.ChildNodes[1]);
					ChildNodes.Add(expr);
					break;
			}

			AsString = "Primary Expression";
			if (ChildNodes.Count == 0)
			{
				AsString += " (Empty)";
			}
			else
				ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
		}

		protected override object DoEvaluate(ScriptThread thread)
		{
			thread.CurrentNode = this;  //standard prolog
			object result = null;

			for (int i = 0; i < ChildNodes.Count; i++)
			{
				result = ChildNodes[i].Evaluate(thread);
			}

			thread.CurrentNode = Parent; //standard epilog
			return result; //return result of last statement
		}
	}
}

// end
