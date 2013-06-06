
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
	public class LiteralNode : OclAstNode
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public object Value;
		public LiteralType LiteralType = LiteralType.Unknown;

		public override void Init(AstContext context, ParseTreeNode treeNode)
		{
			base.Init(context, treeNode);
			logger.Trace("Init");

			switch (treeNode.ChildNodes[0].Term.Name)
			{
				case "STRING":
					LiteralType = Ast.LiteralType.String;
					Value = treeNode.ChildNodes[0].Token.Value;
					break;
				case "NUMBER":
					LiteralType = Ast.LiteralType.Number;

					try
					{
						Value = int.Parse(treeNode.ChildNodes[0].Token.Value.ToString());
					}
					catch (FormatException)
					{
						Value = double.Parse(treeNode.ChildNodes[0].Token.Value.ToString());
					}

					break;
				default:
					LiteralType = Ast.LiteralType.Enum;
					throw new NotImplementedException();
			}

			AsString = "Literal (" + LiteralType.ToString() + ")";
		}

		protected override object DoEvaluate(ScriptThread thread)
		{
			return Expression.Constant(Value);
		}

		public override bool IsConstant()
		{
			return true;
		}
	}

	public enum LiteralType
	{
		Unknown,
		String,
		Number,
		Enum
	}

}

// end
