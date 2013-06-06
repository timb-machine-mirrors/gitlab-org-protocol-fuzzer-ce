
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
	public class PropertyCallNode : OclAstNode
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public string Property = "Unknown";
		public object BoolValue = null;
		public bool IsBoolean = false;
		public bool IsProperty = false;
		public bool IsMethod = false;
		public bool IsItemAccess = false; // [5]
		public List<ExpressionNode> Arguments = new List<ExpressionNode>();

		public override void Init(AstContext context, ParseTreeNode treeNode)
		{
			base.Init(context, treeNode);
			logger.Trace("Init");

			Property = treeNode.ChildNodes[0].ChildNodes[0].Token.Text;

			if (treeNode.ChildNodes[1].ChildNodes[0].ChildNodes.Count > 0)
				Property += treeNode.ChildNodes[1].ChildNodes[0].ChildNodes[0].ChildNodes[0].Token.Text;

			// Are we a method?
			if (treeNode.ChildNodes[3].ChildNodes[0].ChildNodes.Count > 0)
			{
				IsMethod = true;

				// Do we have parameters?
				var paramList = treeNode.ChildNodes[3].ChildNodes[0].ChildNodes[0].ChildNodes[1].ChildNodes[0];

				if (paramList.ChildNodes.Count > 0)
				{
					foreach (ParseTreeNode item in paramList.ChildNodes[0].ChildNodes)
					{
						// item should be "expression"
						var expression = new ExpressionNode();
						expression.Init(context, item);
						Arguments.Add(expression);
					}
				}
			}
			// Is this an array access?
			else if (treeNode.ChildNodes.Count > 2 &&
				treeNode.ChildNodes[2].ChildNodes.Count > 0 &&
				treeNode.ChildNodes[2].ChildNodes[0].ChildNodes.Count > 0 &&
				treeNode.ChildNodes[2].ChildNodes[0].ChildNodes[0].ChildNodes.Count > 1 &&
				treeNode.ChildNodes[2].ChildNodes[0].ChildNodes[0].ChildNodes[0].Term.Name == "[")
			{
				var argument = treeNode.ChildNodes[2].ChildNodes[0].ChildNodes[0].ChildNodes[1].ChildNodes[0];

				// item should be "expression"
				var expression = new ExpressionNode();
				expression.Init(context, argument);
				Arguments.Add(expression);

				IsItemAccess = true;
			}
			else if (Property == "false")
			{
				IsBoolean = true;
				BoolValue = false;
				Property = "Boolean";
			}
			else if (Property == "true")
			{
				IsBoolean = true;
				BoolValue = true;
				Property = "Boolean";
			}
			else
			{
				IsProperty = true;
			}

			AsString = "Property Call (" + Property + ")";
		}

		protected override object DoEvaluate(ScriptThread thread)
		{
			if (IsBoolean)
				return Expression.Constant(BoolValue);

			if (IsProperty)
				return Expression.Constant(Property);

			return this;
		}

		public override bool IsConstant()
		{
			return IsBoolean;
		}
	}
}

// end
