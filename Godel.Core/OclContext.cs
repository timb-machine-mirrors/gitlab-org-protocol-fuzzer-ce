using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

using Irony.Parsing;
using Irony.Interpreter;
using Irony.Interpreter.Ast;

using Godel.Core.OCL.Parser;
using Godel.Core.OCL;

namespace Godel.Core.OCL
{
	/// <summary>
	/// OCL Context
	/// </summary>
	public class OclContext
	{
		public string Name {get{return _context.Context.Name;}}
		public bool ControlOnly { get; set; }

		Ast.ConstraintNode _context;
		ScriptApp _app;

		List<AstNode> _invNodes = new List<AstNode>();
		List<AstNode> _preNodes = new List<AstNode>();
		List<AstNode> _postNodes = new List<AstNode>();

		internal OclContext(Ast.ConstraintNode context, ScriptApp app)
		{
			_context = context;
			_app = app;
			ControlOnly = false;

			foreach (Ast.ConstraintDefinitionNode node in context.Constraints)
			{
				switch (node.Stereotype.ToLower())
				{
					case "inv":
						_invNodes.Add(node);
						break;
					case "pre":
						_preNodes.Add(node);
						break;
					case "post":
						_postNodes.Add(node);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// Execute all invariant constraints
		/// </summary>
		/// <param name="self">Our self instance</param>
		/// <returns></returns>
		public bool Inv(object self, object preSelf, object context = null)
		{
			var thread = new ScriptThread(_app);
			thread.App.Globals["self"] = self;
			thread.App.Globals["self@pre"] = preSelf;
			thread.App.Globals["context"] = context;

			bool ret = true;

			foreach (AstNode node in _invNodes)
				ret = ret && RunExpression((Expression)node.Evaluate(thread));

			return ret;
		}

		/// <summary>
		/// Execute all pre constraints
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public bool Pre(object self, object preSelf, object context = null)
		{
			var thread = new ScriptThread(_app);
			thread.App.Globals["self"] = self;
			thread.App.Globals["self@pre"] = preSelf;
			thread.App.Globals["context"] = context;

			bool ret = true;

			foreach (AstNode node in _preNodes)
				ret = ret && RunExpression((Expression)node.Evaluate(thread));

			return ret;
		}

		/// <summary>
		/// Execute all post constraints
		/// </summary>
		/// <param name="self"></param>
		/// <returns></returns>
		public bool Post(object self, object preSelf, object context = null)
		{
			var thread = new ScriptThread(_app);
			thread.App.Globals["self"] = self;
			thread.App.Globals["self@pre"] = preSelf;
			thread.App.Globals["context"] = context;

			bool ret = true;

			foreach (AstNode node in _postNodes)
				ret = ret && RunExpression((Expression)node.Evaluate(thread));

			return ret;
		}

		protected bool RunExpression(Expression expression)
		{
			//var expression = ChildNodes[0].Evaluate(thread) as Expression;
			var block = Expression.Block(expression);
            try
            {
                var ret = Expression.Lambda<Func<bool>>(block).Compile()();
                return (bool)ret;
            }
            catch
            {
                //TODO more descriptive error message, when ocl is bad
                throw;
            }
		}
	}
}
