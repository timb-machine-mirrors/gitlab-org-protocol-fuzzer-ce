
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
	public class ConstraintDefinitionNode : OclAstNode
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public string Name = null;
		public string Stereotype;

		public override void Init(AstContext context, ParseTreeNode treeNode)
		{
			base.Init(context, treeNode);
			logger.Trace("Init");

			RecursiveSetNoAstNode(treeNode.ChildNodes[0]);

			Stereotype = treeNode.ChildNodes[0].ChildNodes[0].Token.Text;

			try
			{
				Name = treeNode.ChildNodes[1].ChildNodes[0].ChildNodes[0].Token.Text;
			}
			catch
			{
			}

			var oclExpressionNode = new OclExpressionNode();
			oclExpressionNode.Init(context, treeNode.ChildNodes[3]);
			ChildNodes.Add(oclExpressionNode);

			if (Name != null)
				AsString = "Constraint Definintion (" + Stereotype + ":" + Name + ")";
			else
				AsString = "Constraint Definintion (" + Stereotype + ")";
		}

		protected override object DoEvaluate(ScriptThread thread)
		{
			return ChildNodes[0].Evaluate(thread);
		}
	}
}

// end
