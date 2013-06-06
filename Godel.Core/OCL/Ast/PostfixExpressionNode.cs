
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
	public class PostfixExpressionNode : OclAstNode
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public string Operand = null;

		public override void Init(AstContext context, ParseTreeNode treeNode)
		{
			base.Init(context, treeNode);
			logger.Trace("Init");

			var prime = new PrimaryExpressionNode();
			prime.Init(context, treeNode.ChildNodes[0]);
			ChildNodes.Add(prime);

			if (treeNode.ChildNodes.Count == 2)
			{
				List<PropertyCallNode> callNodes = HandleRecusivePropertyCall(context, treeNode.ChildNodes[1]);
				foreach (var node in callNodes)
				{
					ChildNodes.Add(node);
				}
			}

			AsString = "Postfix Expression";
			if (Operand != null)
				AsString += " (" + Operand + ")";
			if (ChildNodes.Count == 0)
			{
				AsString += " (Empty)";
			}
			else
				ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
		}

		public List<PropertyCallNode> HandleRecusivePropertyCall(AstContext context, ParseTreeNode optList)
		{
			List<PropertyCallNode> propertyCallList = new List<PropertyCallNode>();

			if (optList.ChildNodes.Count == 0)
				return propertyCallList;

			var propertyCallNode = new PropertyCallNode();
			propertyCallNode.Init(context, optList.ChildNodes[1]);
			propertyCallList.Add(propertyCallNode);

			if (optList.ChildNodes.Count > 2)
			{
				for (int cnt = 2; cnt < optList.ChildNodes.Count; cnt++)
					propertyCallList.AddRange(HandleRecusivePropertyCall(context, optList.ChildNodes[cnt]));
			}

			return propertyCallList;
		}

		protected override object DoEvaluate(ScriptThread thread)
		{
			Expression expression = ChildNodes[0].Evaluate(thread) as Expression;

			// If it's not a global its a literal
			if (thread.App.Globals.ContainsKey(expression.ToString().Replace("\"", "")))
			{

				var obj = thread.App.Globals[expression.ToString().Replace("\"", "")];
				expression = Expression.Constant(obj);

				for (int i = 1; i < ChildNodes.Count; i++)
				{
					var prop = ChildNodes[i].Evaluate(thread);
					if (prop is Expression)
					{
						var name = prop.ToString();
						name = name.Replace("\"", "");

						try
						{
							expression = Expression.Property(expression, name);
						}
						catch (ArgumentException)
						{
							expression = Expression.Field(expression, name);
						}
					}
					else if (prop is PropertyCallNode)
					{
						PropertyCallNode call = prop as PropertyCallNode;

						if (call.IsMethod)
						{
							var name = call.Property.Replace("\"", "");
							string[] knownMethods = { "abs", "floor", "concat", "size", "substring" };

							List<Expression> args = new List<Expression>();
							foreach (ExpressionNode a in call.Arguments)
								args.Add((Expression)a.Evaluate(thread));

							if (knownMethods.Contains(name))
							{
								args.Insert(0, expression);

								expression = Expression.Call(
									null,
									typeof(KnownMethods).GetMethod(name),
									args.ToArray());
							}
							else
							{
								expression = Expression.Call(
									expression,
									name,
									null,
									args.ToArray());
							}
						}
						// object[foo]
						else if (call.IsItemAccess)
						{
							var name = call.Property;
							name = name.Replace("\"", "");

							try
							{
								expression = Expression.Property(expression, name);
							}
							catch (ArgumentException)
							{
								expression = Expression.Field(expression, name);
							}

							expression = Expression.Property(expression, "Item", (Expression)call.Arguments[0].Evaluate(thread));
						}
						else
						{
							throw new ArgumentOutOfRangeException("Shouldn't be here!");
						}
					}
				}
			}

			return expression;
		}
	}
}

// end
