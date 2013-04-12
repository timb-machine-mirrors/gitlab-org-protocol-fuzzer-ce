
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
	public class ConstraintNode : OclAstNode
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public ContextNode Context;
		public List<ConstraintDefinitionNode> Constraints = new List<ConstraintDefinitionNode>();

		public override void Init(AstContext context, ParseTreeNode treeNode)
		{
			base.Init(context, treeNode);
			logger.Trace("Init");

			RecursiveSetNoAstNode(treeNode.ChildNodes[0]);

			Context = new ContextNode();
			Context.Init(context, treeNode.ChildNodes[0]);
			ChildNodes.Add(Context);

			var constraintDef = new ConstraintDefinitionNode();
			constraintDef.Init(context, treeNode.ChildNodes[1]);
			ChildNodes.Add(constraintDef);
			Constraints.Add(constraintDef);

			for (int i = 4; i < treeNode.ChildNodes[1].ChildNodes.Count; i++)
			{
				constraintDef = new ConstraintDefinitionNode();
				constraintDef.Init(context, treeNode.ChildNodes[1].ChildNodes[i]);
				ChildNodes.Add(constraintDef);
				Constraints.Add(constraintDef);
			}

			AsString = "Constraint";
		}
	}
}

// end
