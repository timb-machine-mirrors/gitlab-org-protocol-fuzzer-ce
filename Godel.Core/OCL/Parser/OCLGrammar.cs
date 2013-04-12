
//
// Copyright (c) Deja vu Security
//

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

using Godel.Core.OCL.Ast;

namespace Godel.Core.OCL.Parser
{
	[Language("OCL", "2.2", "Object Constraint Language")]
	public class OCLGrammar : Grammar
	{
		public OCLGrammar() : base(false) // case insensitive
		{
			//CaseSensitive = false;

			var oclFile = new NonTerminal("oclFile");
			var packageName = new NonTerminal("packageName");
			var oclExpressions = new NonTerminal("oclExpressions", typeof(StatementListNode));
			var oclExpression_opt_let = new NonTerminal("oclExpression_opt_let");
			var constraint = new NonTerminal("constraint", typeof(ConstraintNode));
			var constraintDefList = new NonTerminal("constraintDefList", typeof(ConstraintDefinitionNode));
			var contextDeclaration = new NonTerminal("contextDeclaration", typeof(ContextNode));
			var constraintLetList = new NonTerminal("constraintLetList");
			var classifierContext = new NonTerminal("classifierContext", typeof(EmptyStatementNode));
			var operationContext = new NonTerminal("operationContext");
			var stereotype = new NonTerminal("stereotype");
			var operationName = new NonTerminal("operationName");
			var formalParameterList = new NonTerminal("formalParameterList");
			var formalParameterList_opt_type = new NonTerminal("formalParameterList_opt_type");
			var typeSpecifier = new NonTerminal("typeSpecifier");
			var collectionType = new NonTerminal("collectionType");
			var oclExpression = new NonTerminal("oclExpression", typeof(StatementListNode));
			var returnType = new NonTerminal("returnType");
			var expression = new NonTerminal("expression", typeof(ExpressionNode));
			var letExpression = new NonTerminal("letExpression");
			var ifExpression = new NonTerminal("ifExpression", typeof(IfNode));
			var logicalExpression = new NonTerminal("logicalExpression");
			var relationalExpression = new NonTerminal("relationalExpression");
			var logicalExpressionOptList = new NonTerminal("logicalExpressionOptList");
			var additiveExpression = new NonTerminal("additiveExpression", typeof(AdditiveExpressionNode));
			var additiveExpressionList = new NonTerminal("additiveExpressionList");
			var multiplicativeExpression = new NonTerminal("multiplicativeExpression", typeof(MultiplicativeExpressionNode));
			var multiplicativeExpressionList = new NonTerminal("multiplicativeExpressionList");
			var unaryExpression = new NonTerminal("unaryExpression", typeof(UnaryOperationNode));
			var postfixExpression = new NonTerminal("postfixExpression");
			var primaryExpression = new NonTerminal("primayExpression");
			var propertyCallParameters = new NonTerminal("propertyCallParameters");
			var featureCallParameters = new NonTerminal("featureCallParameters");
			var literal = new NonTerminal("literal", typeof(LiteralValueNode));
			var enumLiteral = new NonTerminal("enumLiteral");
			var enumLiteralList = new NonTerminal("enumLiteralList");
			var simpleTypeSpecifier = new NonTerminal("simpleTypeSpecifier");
			var literalCollection = new NonTerminal("literalCollection");
			var collectionItem = new NonTerminal("collectionItem");
			var collectionList = new NonTerminal("collectionList");
			var propertyCall = new NonTerminal("propertyCall");
			var propertyCallList = new NonTerminal("propertyCallList", typeof(FunctionCallNode));
			var qualifiers = new NonTerminal("qualifiers");
			var declarator = new NonTerminal("declarator");
			var declaratorNameList = new NonTerminal("declaratorNameList");
			var pathName = new NonTerminal("pathName");
			var timeExpression = new NonTerminal("timeExpression");
			var actualParameterList = new NonTerminal("actualParameterList");
			var logicalOperator = new NonTerminal("logicalOperator");
			var collectionKind = new NonTerminal("collectionKind");
			var relationalOperator = new NonTerminal("realtionalOperator");
			var addOperator = new NonTerminal("addOperator");
			var multiplyOperator = new NonTerminal("multiplyOperator");
			var unaryOperator = new NonTerminal("unaryOperator");
			
			var lineComment = new CommentTerminal("line_comment", "--", "\n", "\r\n");
			NonGrammarTerminals.Add(lineComment);

			var NAME = new IdentifierTerminal("NAME", IdOptions.IsNotKeyword);
			var STRING = new StringLiteral("STRING", "'", StringOptions.AllowsAllEscapes);
			var NUMBER = new NumberLiteral("NUMBER", NumberOptions.AllowLetterAfter);

			NAME.AstConfig.NodeCreator = new AstNodeCreator(Identifier_AstNodeCreator);

			oclFile.Rule = MakePlusRule(oclFile, null, ToTerm("package") + packageName + oclExpressions + ToTerm("endpackage"));
			packageName.Rule = pathName;
			oclExpressions.Rule = MakeStarRule(oclExpressions, null, constraint);
			oclExpression.SetFlag(TermFlags.AstDelayChildren);
			
			constraint.Rule = contextDeclaration + constraintDefList;
			constraint.SetFlag(TermFlags.AstDelayChildren);
			
			constraintDefList.Rule = MakePlusRule(constraintDefList, null,
				(ToTerm("def") + NAME.Q() + ":" + ToTerm(":") + typeSpecifier).Q() + "=" + expression |
				(stereotype + NAME.Q() + ":" + oclExpression));
			constraintLetList.Rule = MakeStarRule(constraintLetList, null, letExpression);

			contextDeclaration.Rule = ToTerm("context") + (operationContext | classifierContext);
			classifierContext.Rule = (NAME + ":" + NAME) | NAME;
			operationContext.Rule = NAME + "::" + operationName + "(" + formalParameterList + ")" + (":" + returnType).Q();
			stereotype.Rule = ToTerm("pre") | ToTerm("post") | ToTerm("inv");
			operationName.Rule = NAME | "=" | "+" | "-" | "<" | "<=" | ">=" | ">" | "\\" | "*" | "!=" | "implies" | "not" | "or" | "xor" | "and";
			formalParameterList.Rule = (NAME + ":" + typeSpecifier + 
				formalParameterList_opt_type).Q();
			formalParameterList_opt_type.Rule = MakeStarRule(formalParameterList_opt_type, null, ("," + NAME + ":" + typeSpecifier));
			typeSpecifier.Rule = simpleTypeSpecifier | collectionType;
			collectionType.Rule = collectionType + "(" + simpleTypeSpecifier + ")";
			oclExpression.Rule = (oclExpression_opt_let + "in").Q() + expression;
			//oclExpression.Rule = expression;
			oclExpression_opt_let.Rule = MakeStarRule(oclExpression_opt_let, null, letExpression);
			returnType.Rule = typeSpecifier;
			expression.Rule = logicalExpression;

			letExpression.Rule = ToTerm("let") + NAME +
				("(" + formalParameterList + ")").Q() +
				(ToTerm(":") + typeSpecifier).Q() + "=" + expression;
			
			ifExpression.Rule = ToTerm("if") + expression + ToTerm("then") + expression + PreferShiftHere() + ToTerm("else") + expression + ToTerm("endif");

			//logicalExpression.Rule = MakePlusRule(logicalExpression, logicalOperator, relationalExpression);
			logicalExpression.Rule = relationalExpression + logicalExpressionOptList;
			logicalExpressionOptList.Rule = MakeStarRule(logicalExpressionOptList, null, logicalOperator + relationalExpression);

			relationalExpression.Rule = additiveExpression + (relationalOperator + additiveExpression).Q();
			additiveExpression.Rule = multiplicativeExpression + additiveExpressionList;
			additiveExpressionList.Rule = MakeStarRule(additiveExpressionList, null, addOperator+multiplicativeExpression);
			multiplicativeExpression.Rule = unaryExpression + multiplicativeExpressionList.Q();
			multiplicativeExpressionList.Rule = MakePlusRule(multiplicativeExpressionList, null, multiplyOperator + unaryExpression);
			unaryExpression.Rule = (unaryOperator + postfixExpression) | postfixExpression;
			postfixExpression.Rule = primaryExpression + propertyCallList;
			propertyCallList.Rule = MakeStarRule(propertyCallList, null, (ToTerm(".") | ToTerm("->"))+propertyCall);

			//primaryExpression.Rule = 
			//    literalCollection | 
			//    (str | n | (NAME + "::" + NAME + MakeStarRule(primaryExpression, null, ("::" + NAME))) => literal | 
			//    propertyCall | 
			//    "(" + expression + ")" | 
			//    ifExpression;
			primaryExpression.Rule = 
				literalCollection |
				literal | 
				propertyCall | 
				"(" + expression + ")" | 
				ifExpression;

			propertyCallParameters.Rule = pathName + timeExpression.Q() + qualifiers.Q() + featureCallParameters.Q();
			//featureCallParameters.Rule = ToTerm("(") + (PreferShiftHere() + declarator).Q() + actualParameterList + ToTerm(")");
			featureCallParameters.Rule = ToTerm("(") + actualParameterList.Q() + ToTerm(")");

			literal.Rule = STRING | NUMBER | enumLiteral;
			enumLiteral.Rule = NAME + PreferShiftHere() + "::" + enumLiteralList;
			enumLiteralList.Rule = MakePlusRule(enumLiteralList, ToTerm("::"), NAME);

			simpleTypeSpecifier.Rule = pathName;
			literalCollection.Rule = collectionKind + "{" + collectionList.Q() + "}";

			collectionItem.Rule = expression + (ToTerm("..") + expression).Q();
			collectionList.Rule = MakePlusRule(collectionList, ToTerm(",") + collectionItem);
			//propertyCall.Rule = pathName + timeExpression.Q() + qualifiers.Q() + propertyCallParameters.Q();
			propertyCall.Rule = pathName + timeExpression.Q() + qualifiers.Q() + featureCallParameters.Q();
			qualifiers.Rule = ToTerm("[") + actualParameterList + "]";
			//declarator.Rule = NAME + MakeStarRule(declarator, null, "," + NAME) +
			//    (":" + simpleTypeSpecifier).Q() +
			//    (";" + NAME + ":" + typeSpecifier + "=" + expression).Q() +
			//    "|";
			declaratorNameList.Rule = MakePlusRule(declaratorNameList, ToTerm(","), PreferShiftHere() + NAME);
			declarator.Rule = declaratorNameList + (ToTerm(":") + simpleTypeSpecifier).Q() + "|";

			pathName.Rule = MakePlusRule(pathName, ToTerm("::"), NAME); //PreferShiftHere()+
			timeExpression.Rule = ToTerm("@pre");
			actualParameterList.Rule = MakePlusRule(actualParameterList, ToTerm(","), expression);
			logicalOperator.Rule = ToTerm("and") | "or" | "xor" | "implies";
			collectionKind.Rule = ToTerm("Set") | "Bag" | "Sequence" | "Collection";
			relationalOperator.Rule = ToTerm("=") | ">" | "<" | "<=" | ">=" | "!=";
			addOperator.Rule = ToTerm("+") | "-";
			multiplyOperator.Rule = ToTerm("*") | "/";
			unaryOperator.Rule = ToTerm("-") | "not";

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

			//MarkPunctuation("if", "then", "else", "endif", "def", "context", ":", "(", ")", "pre", "post", "inv", "let", "[", "]");
			//MarkPunctuation("and", "or", "xor", "implies");
			MarkReservedWords("and", "or", "xor", "implies");
			RegisterBracePair("(", ")");

			this.LanguageFlags = Irony.Parsing.LanguageFlags.CreateAst;


			this.Root = oclExpressions;
		}

		public void Identifier_AstNodeCreator(AstContext context, ParseTreeNode parseNode)
		{
			parseNode.AstNode = new IdentifierNode();
			(parseNode.AstNode as IdentifierNode).Init(context, parseNode);
		}
	}
}
