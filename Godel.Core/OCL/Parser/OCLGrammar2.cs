using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony;
using Irony.Parsing;
using Irony.Parsing.Construction;
using Irony.Ast;
using Irony.Interpreter;
using Irony.Interpreter.Ast;

namespace Godel.Core.OCL.Parser
{
	[Language("OCL", "2.3", "Object Constraint Language")]
	public class OCLGrammar2 : Grammar
	{
		public OCLGrammar2()
			: base(false) // case insensitive
		{
			var ExpressionInOclCS = new NonTerminal("ExpressionInOclCS");
			var OclExpressionCS = new NonTerminal("OclExpressionCS");
			var VariableExpCS = new NonTerminal("VariableExpCS");
			var simpleNameCS = new NonTerminal("simpleNameCS");
			var pathNameCS = new NonTerminal("pathNameCS");
			var LiteralExpCS = new NonTerminal("LiteralExpCS");
			var EnumLiteralExpCS = new NonTerminal("EnumLiteralExpCS");
			var CollectionLiteralExpCS = new NonTerminal("CollectionLiteralExpCS");
			var CollectionTypeIdentifierCS = new NonTerminal("CollectionTypeIdentifierCS");
			var CollectionLiteralPartsCS = new NonTerminal("CollectionLiteralPartsCS");
			var CollectionLiteralPartCS = new NonTerminal("CollectionLiteralPartCS");
			var CollectionRangeCS = new NonTerminal("CollectionRangeCS");
			var PrimitiveLiteralExpCS = new NonTerminal("PrimitiveLiteralExpCS");
			var TupleLiteralExpCS = new NonTerminal("TupleLiteralExpCS");
			var IntegerLiteralExpCS = new NonTerminal("IntegerLiteralExpCS");
			var RealLiteralExpCS = new NonTerminal("RealLiteralExpCS");
			var StringLiteralExpCS = new NonTerminal("StringLiteralExpCS");
			var BooleanLiteralExpCS = new NonTerminal("BooleanLiteralExpCS");
			var CallExpCS = new NonTerminal("CallExpCS");
			var LoopExpCS = new NonTerminal("LoopExpCS");
			var IteratorExpCS = new NonTerminal("IteratorExpCS");
			var IterateExpCS = new NonTerminal("IterateExpCS");
			var VariableDeclarationCS = new NonTerminal("VariableDeclarationCS");
			var TypeCS = new NonTerminal("TypeCS");
			var collectionTypeCS = new NonTerminal("collectionTypeCS");
			var tupleTypeCS = new NonTerminal("tupleTypeCS");
			var variableDeclarationListCS = new NonTerminal("variableDeclarationListCS");
			var FeatureCallExpCS = new NonTerminal("FeatureCallExpCS");
			var OperationCallExpCS = new NonTerminal("OperationCallExpCS");
			var PropertyCallExpCS = new NonTerminal("PropertyCallExpCS");
			var NavigationCallExpCS = new NonTerminal("NavigationCallExpCS");
			var AssociationClassCallExpCS = new NonTerminal("AssociationClassCallExpCS");
			var isMarkedPreCS = new NonTerminal("isMarkedPreCS");
			var argumentsCS = new NonTerminal("argumentsCS");
			var LetExpCS = new NonTerminal("LetExpCS");
			var LetExpSubCS = new NonTerminal("LetExpSubCS");
			var OclMessageExpCS = new NonTerminal("OclMessageExpCS");
			var OclMessageArgumentsCS = new NonTerminal("OclMessageArgumentsCS");
			var OclMessageArgCS = new NonTerminal("OclMessageArgCS");
			var IfExpCS = new NonTerminal("IfExpCS");
			var NullLiteralExpCS = new NonTerminal("NullLiteralExpCS");
			var InvalidLiteralExpCS = new NonTerminal("InvalidLiteralExpCS");

			var lineComment = new CommentTerminal("line_comment", "--", "\n", "\r\n");
			NonGrammarTerminals.Add(lineComment);

			var NAME = new IdentifierTerminal("NAME", IdOptions.IsNotKeyword);
			var STRING = new StringLiteral("STRING", "'", StringOptions.AllowsAllEscapes);
			var NUMBER = new NumberLiteral("NUMBER", NumberOptions.AllowLetterAfter);

			this.Root = ExpressionInOclCS;
			this.LanguageFlags = Irony.Parsing.LanguageFlags.CreateAst | Irony.Parsing.LanguageFlags.Default;

			ExpressionInOclCS.Rule = OclExpressionCS;
			OclExpressionCS.Rule = CallExpCS | VariableExpCS | LiteralExpCS | LetExpCS | OclMessageExpCS | IfExpCS;
			VariableExpCS.Rule = simpleNameCS;
			simpleNameCS.Rule = NAME;
			pathNameCS.Rule = MakePlusRule(pathNameCS, ToTerm("::"), simpleNameCS);
			LiteralExpCS.Rule = EnumLiteralExpCS | CollectionLiteralExpCS | TupleLiteralExpCS | PrimitiveLiteralExpCS;
			EnumLiteralExpCS.Rule = pathNameCS + "::" + simpleNameCS;
			CollectionLiteralExpCS.Rule = CollectionTypeIdentifierCS + "{" + CollectionLiteralPartsCS.Q() + "}";
			CollectionTypeIdentifierCS.Rule = ToTerm("Set") | "Bag" | "Sequence" | "Collection" | "OrderedSet";
			CollectionLiteralPartsCS.Rule = MakeListRule(CollectionLiteralPartsCS, ToTerm(","), CollectionLiteralPartCS);
			CollectionLiteralPartCS.Rule = CollectionRangeCS | OclExpressionCS;
			CollectionRangeCS.Rule = OclExpressionCS + "," + OclExpressionCS;
			PrimitiveLiteralExpCS.Rule = IntegerLiteralExpCS | RealLiteralExpCS | StringLiteralExpCS | BooleanLiteralExpCS;
			TupleLiteralExpCS.Rule = ToTerm("Tuple") + "{" + variableDeclarationListCS + "}";
			IntegerLiteralExpCS.Rule = NUMBER;
			RealLiteralExpCS.Rule = NUMBER;
			StringLiteralExpCS.Rule = STRING;
			BooleanLiteralExpCS.Rule = ToTerm("true") | "false";
			CallExpCS.Rule = FeatureCallExpCS | LoopExpCS;
			LoopExpCS.Rule = IteratorExpCS | IterateExpCS;
			IteratorExpCS.Rule =
				(OclExpressionCS + "->" + simpleNameCS +
					ToTerm("(") + (VariableDeclarationCS + (ToTerm(",") + VariableDeclarationCS).Q() + "|").Q() +
					OclExpressionCS + ")") |
				(OclExpressionCS + "." + simpleNameCS + "(" + argumentsCS.Q() + ")") |
				(OclExpressionCS + "." + simpleNameCS) |
				(OclExpressionCS + "." + simpleNameCS + ("[" + argumentsCS + "]").Q()) |
				(OclExpressionCS + "." + simpleNameCS + ("[" + argumentsCS + "]").Q());
			IterateExpCS.Rule = OclExpressionCS + "->" + "iterate" +
				"(" + (VariableDeclarationCS + ";").Q() + VariableDeclarationCS + "|" +
				OclExpressionCS + ")";
			VariableDeclarationCS.Rule = simpleNameCS + (ToTerm(":") + TypeCS).Q() +
				(ToTerm("=") + OclExpressionCS).Q();
			TypeCS.Rule = pathNameCS | collectionTypeCS | tupleTypeCS;
			collectionTypeCS.Rule = CollectionTypeIdentifierCS + "(" + TypeCS + ")";
			tupleTypeCS.Rule = "Tuple" + "(" + variableDeclarationListCS.Q() + ")";
			variableDeclarationListCS.Rule = MakePlusRule(variableDeclarationListCS, ToTerm(","), VariableDeclarationCS);
			FeatureCallExpCS.Rule = OperationCallExpCS | PropertyCallExpCS | NavigationCallExpCS;
			OperationCallExpCS.Rule =
				(OclExpressionCS + simpleNameCS + OclExpressionCS) |
				(OclExpressionCS + "->" + simpleNameCS + "(" + argumentsCS.Q() + ")") |
				(OclExpressionCS + "." + simpleNameCS + "(" + argumentsCS.Q() + ")") |
				(simpleNameCS + "(" + argumentsCS.Q() + ")") |
				(pathNameCS + "(" + argumentsCS.Q() + ")") |
				(simpleNameCS + OclExpressionCS) |
				(OclExpressionCS + "." + pathNameCS + "::" + simpleNameCS + "(" + argumentsCS.Q() + ")") |
				(OclExpressionCS + "." + pathNameCS + "::" + simpleNameCS + isMarkedPreCS + "(" + argumentsCS.Q() + ")");
			PropertyCallExpCS.Rule =
				(OclExpressionCS + "." + simpleNameCS + isMarkedPreCS.Q()) |
				(simpleNameCS + isMarkedPreCS.Q()) |
				pathNameCS |
				(OclExpressionCS + "." + pathNameCS + "::" + simpleNameCS + isMarkedPreCS.Q());
			NavigationCallExpCS.Rule = PropertyCallExpCS | AssociationClassCallExpCS;
			AssociationClassCallExpCS.Rule =
				(OclExpressionCS + "." + simpleNameCS + (ToTerm("[") + argumentsCS + "]").Q() + isMarkedPreCS.Q()) |
				(simpleNameCS + (ToTerm("[") + argumentsCS + "]").Q() + isMarkedPreCS.Q());
			isMarkedPreCS.Rule =  ToTerm("@pre");
			argumentsCS.Rule = OclExpressionCS + (ToTerm(".") + argumentsCS).Q();
			LetExpCS.Rule = ToTerm("let") + VariableDeclarationCS + LetExpSubCS;
			LetExpSubCS.Rule =
				(ToTerm(",") + VariableDeclarationCS + LetExpSubCS) |
				(ToTerm("in") + OclExpressionCS);
			OclMessageExpCS.Rule =
				(OclExpressionCS + "^^" + simpleNameCS + "(" + OclMessageArgumentsCS.Q() + ")") |
				(OclExpressionCS + "^" + simpleNameCS + "(" + OclMessageArgumentsCS.Q() + ")");
			OclMessageArgumentsCS.Rule = MakePlusRule(OclMessageArgumentsCS, ToTerm(","), OclMessageArgCS);
			OclMessageArgCS.Rule =
				("?" + (ToTerm(":") + TypeCS).Q()) |
				OclExpressionCS;
			IfExpCS.Rule = ToTerm("if") + OclExpressionCS + "then" + OclExpressionCS + "else" + OclExpressionCS + "endif";
			NullLiteralExpCS.Rule = ToTerm("null");
			InvalidLiteralExpCS.Rule = ToTerm("invalid");

			RegisterOperators(1, "@pre");
			RegisterOperators(2, "^", "^^");
			RegisterOperators(3, ".", "->");
			RegisterOperators(4, "not", "-");
			RegisterOperators(5, "*", "/");
			RegisterOperators(6, "+", "-");
			RegisterOperators(7, "if", "then", "else", "endif");
			RegisterOperators(8, "<", ">", "<=", ">=");
			RegisterOperators(9, "=", "<>");
			RegisterOperators(10, "and");
			RegisterOperators(11, "or");
			RegisterOperators(12, "xor");
			RegisterOperators(13, "implies");
			RegisterOperators(14, "let-in");
		}
	}
}
