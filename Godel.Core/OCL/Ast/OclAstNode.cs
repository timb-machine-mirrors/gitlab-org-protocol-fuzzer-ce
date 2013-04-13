
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
	public class OclAstNode : AstNode
	{
		public AstNode RecursiveAddChild(AstNode parent, ParseTreeNode treeNode)
		{
			var parentNode = parent.AddChild(string.Empty, treeNode);

			foreach (ParseTreeNode node in treeNode.ChildNodes)
			{
				RecursiveAddChild(parentNode, node);
			}

			return parentNode;
		}

		public void RecursiveSetNoAstNode(ParseTreeNode treeNode)
		{
			treeNode.Term.Flags |= TermFlags.NoAstNode;
			foreach (ParseTreeNode node in treeNode.ChildNodes)
				RecursiveSetNoAstNode(node);
		}

		public void DisplayTree(ParseTreeNode node, int depth = 0)
		{
			for (int i = 0; i < depth; i++)
				Console.Write("  ");

			Console.WriteLine("- " + node.ToString());

			depth++;
			foreach (var child in node.ChildNodes)
				DisplayTree(child, depth);
		}
	}
}

// end
