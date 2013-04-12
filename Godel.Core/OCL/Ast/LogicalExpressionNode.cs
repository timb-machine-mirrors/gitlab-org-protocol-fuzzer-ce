
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
	public abstract class LogicalExpressionNode : OclAstNode
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		AstNode _left = null;
		AstNode _right = null;

		public AstNode Left
		{
			get
			{
				return _left;
			}
			set
			{
				if (_left != null)
					ChildNodes.Remove(_left);

				_left = value;
				ChildNodes.Add(_left);
			}
		}

		public AstNode Right
		{
			get
			{
				return _right;
			}
			set
			{
				if (_right != null)
					ChildNodes.Remove(_right);

				_right = value;
				ChildNodes.Add(_right);
			}
		}
		public string Operator;

		public static LogicalExpressionNode LogicalExpressionFactory(AstContext context, ParseTreeNode logicalOperator)
		{
			var opt = logicalOperator.ChildNodes[0].Token.Text;
			LogicalExpressionNode node = null;

			switch (opt.ToLower())
			{
				case "and":
					node = new LogicalAndExpressionNode();
					break;
				case "or":
					node = new LogicalOrExpressionNode();
					break;
				case "xor":
					node = new LogicalXorExpressionNode();
					break;
				case "implies":
					node = new LogicalImpliesExpressionNode();
					break;
			}

			node.Init(context, logicalOperator);

			return node;
		}

		public abstract void Init(AstContext context, ParseTreeNode treeNode, AstNode Left, AstNode Right);
	}
}

// end
