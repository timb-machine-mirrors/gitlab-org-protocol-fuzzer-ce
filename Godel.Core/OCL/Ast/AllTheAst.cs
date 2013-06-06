
////
//// Copyright (c) Deja vu Security
////

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Reflection;
//using System.Linq.Expressions;

//using Irony.Ast;
//using Irony.Parsing;
//using Irony.Interpreter;
//using Irony.Interpreter.Ast;

//using NLog;

//namespace Godel.Core.OCL.Ast
//{
//    public class OclAstNode : AstNode
//    {
//        public AstNode RecursiveAddChild(AstNode parent, ParseTreeNode treeNode)
//        {
//            var parentNode = parent.AddChild(string.Empty, treeNode);

//            foreach (ParseTreeNode node in treeNode.ChildNodes)
//            {
//                RecursiveAddChild(parentNode, node);
//            }

//            return parentNode;
//        }

//        public void RecursiveSetNoAstNode(ParseTreeNode treeNode)
//        {
//            treeNode.Term.Flags |= TermFlags.NoAstNode;
//            foreach (ParseTreeNode node in treeNode.ChildNodes)
//                RecursiveSetNoAstNode(node);
//        }

//        public void DisplayTree(ParseTreeNode node, int depth = 0)
//        {
//            for(int i = 0; i<depth;i++)
//                Console.Write("  ");

//            Console.WriteLine("- " + node.ToString());

//            depth++;
//            foreach (var child in node.ChildNodes)
//                DisplayTree(child, depth);
//        }
//    }

//    public class OclExpressionNode : OclAstNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public bool hasLetExpression = false;
//        public LabelTarget ReturnTarget = Expression.Label();

//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            var nodes = treeNode.GetMappedChildNodes();

//            // Handle let?
//            if (nodes[0].ChildNodes.Count > 0)
//            {
//                var let = new LetExpressionNode();
//                let.Init(context, nodes[0].ChildNodes[0]);
//                ChildNodes.Add(let);
//                hasLetExpression = true;
//            }

//            // Should always have an expression
//            var expression = new ExpressionNode();
//            expression.Init(context, nodes[1]);
//            ChildNodes.Add(expression);

//            AsString = "OCL Expression";
//            if (hasLetExpression)
//            {
//                AsString += " (Has Let)";
//            }

//            if (ChildNodes.Count == 0)
//            {
//                AsString += " (Empty)";
//            }
//            else
//                ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
//        }

//        protected override object DoEvaluate(ScriptThread thread)
//        {
//            logger.Trace("DoEvaluate");
//            return ChildNodes[0].Evaluate(thread);
//        }
//    }

//    public class ConstraintNode : OclAstNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public ContextNode Context;
//        public List<ConstraintDefinitionNode> Constraints = new List<ConstraintDefinitionNode>();

//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            RecursiveSetNoAstNode(treeNode.ChildNodes[0]);

//            var contextNode = new ContextNode();
//            contextNode.Init(context, treeNode.ChildNodes[0]);
//            ChildNodes.Add(contextNode);

//            var constraintDef = new ConstraintDefinitionNode();
//            constraintDef.Init(context, treeNode.ChildNodes[1]);
//            ChildNodes.Add(constraintDef);
//            Constraints.Add(constraintDef);

//            for (int i = 4; i < treeNode.ChildNodes[1].ChildNodes.Count; i++ )
//            {
//                constraintDef = new ConstraintDefinitionNode();
//                constraintDef.Init(context, treeNode.ChildNodes[1].ChildNodes[i]);
//                ChildNodes.Add(constraintDef);
//                Constraints.Add(constraintDef);
//            }

//            AsString = "Constraint";
//        }
//    }

//    public class ContextNode : OclAstNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public string Name;

//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            RecursiveSetNoAstNode(treeNode.ChildNodes[0]);

//            //Stereotype = treeNode.ChildNodes[1].ChildNodes[0].ChildNodes[0].Token.Text;
//            Name = treeNode.ChildNodes[1].ChildNodes[0].ChildNodes[0].Token.Text;

//            AsString = "Context (" + Name + ")";
//        }
//    }

//    public class ConstraintDefinitionNode : OclAstNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public string Name = null;
//        public string Stereotype;

//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            RecursiveSetNoAstNode(treeNode.ChildNodes[0]);

//            Stereotype = treeNode.ChildNodes[0].ChildNodes[0].Token.Text;

//            try
//            {
//                Name = treeNode.ChildNodes[1].ChildNodes[0].ChildNodes[0].Token.Text;
//            }
//            catch
//            {
//            }

//            var oclExpressionNode = new OclExpressionNode();
//            oclExpressionNode.Init(context, treeNode.ChildNodes[3]);
//            ChildNodes.Add(oclExpressionNode);

//            if(Name != null)
//                AsString = "Constraint Definintion (" + Stereotype + ":" + Name + ")";
//            else
//                AsString = "Constraint Definintion (" + Stereotype + ")";
//        }

//        protected override object DoEvaluate(ScriptThread thread)
//        {
//            return ChildNodes[0].Evaluate(thread);
//        }
//    }

//    public class LetExpressionNode : OclAstNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            // Todo - Add all logic here :)

//            AsString = "Let Expression";
//            if (ChildNodes.Count == 0)
//            {
//                AsString += " (Empty)";
//            }
//            else
//                ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
//        }
//    }

//    public class ExpressionNode : OclAstNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            var nodes = treeNode.ChildNodes[0].GetMappedChildNodes();

//            // Handle first expression
//            var relationalExpression = new RelationalExpressionNode();
//            relationalExpression.Init(context, nodes[0]);

//            if (nodes.Count > 1 && nodes[1].ChildNodes.Count > 0)
//            {
//                // Handle tree
//                var logicalNode = LogicalExpressionNode.LogicalExpressionFactory(context, nodes[1].ChildNodes[0]);
//                logicalNode.Left = relationalExpression;
//                logicalNode.Right = HandleRecusiveOptList(context, nodes[1]);
//                ChildNodes.Add(logicalNode);
//            }
//            else
//                ChildNodes.Add(relationalExpression);

//            AsString = "Expression";
//            if (ChildNodes.Count == 0)
//            {
//                AsString += " (Empty)";
//            }
//            else
//                ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
//        }

//        public OclAstNode HandleRecusiveOptList(AstContext context, ParseTreeNode optList)
//        {
//            var relationalExpression = new RelationalExpressionNode();
//            relationalExpression.Init(context, optList.ChildNodes[1]);

//            if (optList.ChildNodes.Count == 3)
//            {
//                var logicalNode = LogicalExpressionNode.LogicalExpressionFactory(context, optList.ChildNodes[2].ChildNodes[0]);
//                logicalNode.Left = relationalExpression;
//                logicalNode.Right = HandleRecusiveOptList(context, optList.ChildNodes[2]);

//                return logicalNode;
//            }

//            return relationalExpression;
//        }

//        protected override object DoEvaluate(ScriptThread thread)
//        {
//            return ChildNodes[0].Evaluate(thread);
//        }
//    }

//    public abstract class LogicalExpressionNode : OclAstNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        AstNode _left = null;
//        AstNode _right = null;

//        public AstNode Left
//        {
//            get
//            {
//                return _left;
//            }
//            set
//            {
//                if (_left != null)
//                    ChildNodes.Remove(_left);

//                _left = value;
//                ChildNodes.Add(_left);
//            }
//        }

//        public AstNode Right
//        {
//            get
//            {
//                return _right;
//            }
//            set
//            {
//                if (_right != null)
//                    ChildNodes.Remove(_right);

//                _right = value;
//                ChildNodes.Add(_right);
//            }
//        }
//        public string Operator;

//        public static LogicalExpressionNode LogicalExpressionFactory(AstContext context, ParseTreeNode logicalOperator)
//        {
//            var opt = logicalOperator.ChildNodes[0].Token.Text;
//            LogicalExpressionNode node = null;

//            switch(opt.ToLower())
//            {
//                case "and":
//                    node = new LogicalAndExpressionNode();
//                    break;
//                case "or":
//                    node = new LogicalOrExpressionNode();
//                    break;
//                case "xor":
//                    node = new LogicalXorExpressionNode();
//                    break;
//                case "implies":
//                    node = new LogicalImpliesExpressionNode();
//                    break;
//            }

//            node.Init(context, logicalOperator);

//            return node;
//        }

//        public abstract void Init(AstContext context, ParseTreeNode treeNode, AstNode Left, AstNode Right);
//    }

//    public class LogicalAndExpressionNode : LogicalExpressionNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            Operator = "and";
//            AsString = "And";
//        }

//        public override void Init(AstContext context, ParseTreeNode treeNode, AstNode Left, AstNode Right)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            this.Left = Left;
//            this.Right = Right;

//            Operator = treeNode.Token.Text;

//            ChildNodes.Add(Left);
//            ChildNodes.Add(Right);

//            AsString = "And";
//        }

//        protected override object DoEvaluate(ScriptThread thread)
//        {
//            return Expression.And((Expression)Left.Evaluate(thread), (Expression)Right.Evaluate(thread));
//        }
//    }

//    public class LogicalOrExpressionNode : LogicalExpressionNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            Operator = "or";
//            AsString = "Or";
//        }

//        public override void Init(AstContext context, ParseTreeNode treeNode, AstNode Left, AstNode Right)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            this.Left = Left;
//            this.Right = Right;

//            Operator = treeNode.Token.Text;

//            ChildNodes.Add(Left);
//            ChildNodes.Add(Right);

//            AsString = "Or";
//        }

//        protected override object DoEvaluate(ScriptThread thread)
//        {
//            return Expression.Or((Expression)Left.Evaluate(thread), (Expression)Right.Evaluate(thread));
//        }
//    }

//    public class LogicalXorExpressionNode : LogicalExpressionNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            Operator = "xor";
//            AsString = "Xor";
//        }

//        public override void Init(AstContext context, ParseTreeNode treeNode, AstNode Left, AstNode Right)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            this.Left = Left;
//            this.Right = Right;

//            Operator = treeNode.Token.Text;

//            ChildNodes.Add(Left);
//            ChildNodes.Add(Right);

//            AsString = "Xor";
//        }

//        protected override object DoEvaluate(ScriptThread thread)
//        {
//            var args = new Expression[] {
//                    (Expression)Left.Evaluate(thread), 
//                    (Expression)Right.Evaluate(thread)
//                };

//            return Expression.Call(
//                typeof(KnownMethods).GetMethod("xor"), 
//                args);
//        }
//    }

//    public class LogicalImpliesExpressionNode : LogicalExpressionNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            Operator = "implies";
//            AsString = "Implies";
//        }

//        public override void Init(AstContext context, ParseTreeNode treeNode, AstNode Left, AstNode Right)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            this.Left = Left;
//            this.Right = Right;

//            Operator = treeNode.Token.Text;

//            ChildNodes.Add(Left);
//            ChildNodes.Add(Right);

//            AsString = "Implies";
//        }

//        protected override object DoEvaluate(ScriptThread thread)
//        {
//            // Should return last value when executed.  At least I hope so!
//            return Expression.IfThen(
//                Expression.Equal((Expression)Left.Evaluate(thread), Expression.Constant(true)),
//                Expression.IfThenElse(Expression.Equal((Expression)Right.Evaluate(thread), Expression.Constant(false)),
//                    Expression.Constant(false),
//                    Expression.Constant(true)));
//        }
//    }

//    public class RelationalExpressionNode : OclAstNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public OclAstNode Left;
//        public OclAstNode Right = null;
//        public string Operand = null;

//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            var add = new AdditiveExpressionNode();
//            add.Init(context, treeNode.ChildNodes[0]);
//            Left = add;
//            ChildNodes.Add(Left);

//            if (treeNode.ChildNodes[1].ChildNodes.Count > 0 && treeNode.ChildNodes[1].ChildNodes[0].ChildNodes.Count > 0)
//            {
//                add = new AdditiveExpressionNode();
//                add.Init(context, treeNode.ChildNodes[1].ChildNodes[0].ChildNodes[1]);
//                Right = add;
//                Operand = treeNode.ChildNodes[1].ChildNodes[0].ChildNodes[0].ChildNodes[0].Token.Text;
//                ChildNodes.Add(Right);
//            }

//            AsString = "Relational Expression";
//            if (Operand != null)
//                AsString += " (" + Operand + ")";

//            if (ChildNodes.Count == 0)
//                AsString += " (Empty)";
//            else
//                ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
//        }

//        protected override object DoEvaluate(ScriptThread thread)
//        {
//            if (Operand == null)
//                return ChildNodes[0].Evaluate(thread);
			
//            // Evaluate both sides of the expression first.

//            var left = (Expression)Left.Evaluate(thread);
//            var right = (Expression)Right.Evaluate(thread);

//            // Perform type conversion if required.

//            var leftTypeCode = Type.GetTypeCode(left.Type);
//            var rightTypeCode = Type.GetTypeCode(right.Type);

//            if (leftTypeCode > rightTypeCode)
//            {
//                right = Expression.Convert(right, left.Type);
//            }
//            else
//            {
//                left = Expression.Convert(left, right.Type);
//            }

//            // Now do evaulation.

//            switch (Operand)
//            {
//                case "=":
//                    return Expression.Equal(left, right);
//                case "<":
//                    return Expression.LessThan(left, right);
//                case "<=":
//                    return Expression.LessThanOrEqual(left, right);
//                case ">":
//                    return Expression.GreaterThan(left, right);
//                case ">=":
//                    return Expression.GreaterThanOrEqual(left, right);
//                case "!=":
//                    return Expression.NotEqual(left, right);
//            }

//            throw new ArgumentException();
//        }
//    }

//    public class AdditiveExpressionNode : OclAstNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public OclAstNode Left;
//        public OclAstNode Right = null;
//        public string Operand = null;

//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            var mult = new MultiplicativeExpressionNode();
//            mult.Init(context, treeNode.ChildNodes[0]);
//            Left = mult;
//            ChildNodes.Add(Left);

//            if (treeNode.ChildNodes[1].ChildNodes.Count > 0 && treeNode.ChildNodes[1].ChildNodes[0].ChildNodes.Count > 0)
//            {
//                mult = new MultiplicativeExpressionNode();
//                mult.Init(context, treeNode.ChildNodes[1].ChildNodes[1]);
//                Right = mult;
//                Operand = treeNode.ChildNodes[1].ChildNodes[0].ChildNodes[0].Token.Text;
//                ChildNodes.Add(Right);
//            }

//            AsString = "Additive Expression";
//            if (Operand != null)
//                AsString += " (" + Operand + ")";
//            if (ChildNodes.Count == 0)
//            {
//                AsString += " (Empty)";
//            }
//            else
//                ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
//        }

//        protected override object DoEvaluate(ScriptThread thread)
//        {
//            if (Operand == null)
//                return ChildNodes[0].Evaluate(thread);

//            // Evaluate both sides of the expression first.

//            var left = (Expression)Left.Evaluate(thread);
//            var right = (Expression)Right.Evaluate(thread);

//            // Perform type conversion if required.

//            var leftTypeCode = Type.GetTypeCode(left.Type);
//            var rightTypeCode = Type.GetTypeCode(right.Type);

//            if (leftTypeCode > rightTypeCode)
//            {
//                right = Expression.Convert(right, left.Type);
//            }
//            else
//            {
//                left = Expression.Convert(left, right.Type);
//            }

//            // Now do evaulation.

//            if (Operand == "+")
//                return Expression.Add(left, right);

//            return Expression.Subtract(left, right);
//        }
//    }

//    public class MultiplicativeExpressionNode : OclAstNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public OclAstNode Left;
//        public OclAstNode Right;
//        public string Operand = null;

//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            var unary = new UnaryExpressionNode();
//            unary.Init(context, treeNode.ChildNodes[0]);
//            Left = unary;
//            ChildNodes.Add(Left);

//            if (treeNode.ChildNodes.Count > 1 && treeNode.ChildNodes[1].ChildNodes.Count > 0 && treeNode.ChildNodes[1].ChildNodes[0].ChildNodes.Count > 0)
//            {
//                unary = new UnaryExpressionNode();
//                unary.Init(context, treeNode.ChildNodes[1].ChildNodes[0].ChildNodes[1]);
//                Right = unary;
//                Operand = treeNode.ChildNodes[1].ChildNodes[0].ChildNodes[0].ChildNodes[0].Token.Text;
//                ChildNodes.Add(Right);
//            }

//            AsString = "Multiplicative Expression";
//            if (Operand != null)
//                AsString += " (" + Operand + ")";
//            if (ChildNodes.Count == 0)
//            {
//                AsString += " (Empty)";
//            }
//            else
//                ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
//        }

//        protected override object DoEvaluate(ScriptThread thread)
//        {
//            if (Operand == null)
//                return ChildNodes[0].Evaluate(thread);

//            // Evaluate both sides of the expression first.

//            var left = (Expression)Left.Evaluate(thread);
//            var right = (Expression)Right.Evaluate(thread);

//            // Perform type conversion if required.

//            var leftTypeCode = Type.GetTypeCode(left.Type);
//            var rightTypeCode = Type.GetTypeCode(right.Type);

//            if (leftTypeCode > rightTypeCode)
//            {
//                right = Expression.Convert(right, left.Type);
//            }
//            else
//            {
//                left = Expression.Convert(left, right.Type);
//            }

//            // Now do evaulation.

//            if (Operand == "*")
//                return Expression.Multiply(left, right);

//            return Expression.Divide(left, right);
//        }
//    }

//    public class UnaryExpressionNode : OclAstNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public OclAstNode Left;
//        public OclAstNode Right;
//        public string Operand = null;

//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            if (treeNode.ChildNodes[0].ToString() == "postfixExpression")
//            {
//                var post = new PostfixExpressionNode();
//                post.Init(context, treeNode.ChildNodes[0]);
//                ChildNodes.Add(post);
//            }
//            else if (treeNode.ChildNodes.Count == 1)
//            {
//                var post = new PostfixExpressionNode();
//                post.Init(context, treeNode.ChildNodes[0].ChildNodes[0]);
//                ChildNodes.Add(post);
//            }
//            else
//            {
//                Operand = "-";
//                var post = new PostfixExpressionNode();
//                post.Init(context, treeNode.ChildNodes[0].ChildNodes[1]);
//                ChildNodes.Add(post);
//            }

//            AsString = "Unary Expression";
//            if (Operand != null)
//                AsString += " (" + Operand + ")";
//            if (ChildNodes.Count == 0)
//            {
//                AsString += " (Empty)";
//            }
//            else
//                ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
//        }

//        protected override object DoEvaluate(ScriptThread thread)
//        {
//            if (Operand == null)
//                return ChildNodes[0].Evaluate(thread);

//            var left = (Expression)ChildNodes[0].Evaluate(thread);

//            if(Operand == "-")
//                return Expression.MakeUnary(
//                    System.Linq.Expressions.ExpressionType.Negate, 
//                    left, 
//                    left.Type);

//            return Expression.MakeUnary(
//                System.Linq.Expressions.ExpressionType.Not,
//                left,
//                left.Type);
//        }
//    }

//    public class PostfixExpressionNode : OclAstNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public string Operand = null;

//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            var prime = new PrimaryExpressionNode();
//            prime.Init(context, treeNode.ChildNodes[0]);
//            ChildNodes.Add(prime);

//            if (treeNode.ChildNodes.Count == 2)
//            {
//                List<PropertyCallNode> callNodes = HandleRecusivePropertyCall(context, treeNode.ChildNodes[1]);
//                foreach (var node in callNodes)
//                {
//                    ChildNodes.Add(node);
//                }
//            }

//            AsString = "Postfix Expression";
//            if (Operand != null)
//                AsString += " (" + Operand + ")";
//            if (ChildNodes.Count == 0)
//            {
//                AsString += " (Empty)";
//            }
//            else
//                ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
//        }

//        public List<PropertyCallNode> HandleRecusivePropertyCall(AstContext context, ParseTreeNode optList)
//        {
//            List<PropertyCallNode> propertyCallList = new List<PropertyCallNode>();
			
//            if (optList.ChildNodes.Count == 0)
//                return propertyCallList;

//            var propertyCallNode = new PropertyCallNode();
//            propertyCallNode.Init(context, optList.ChildNodes[1]);
//            propertyCallList.Add(propertyCallNode);

//            if (optList.ChildNodes.Count > 2)
//            {
//                for (int cnt = 2; cnt < optList.ChildNodes.Count; cnt++)
//                    propertyCallList.AddRange(HandleRecusivePropertyCall(context, optList.ChildNodes[cnt]));
//            }

//            return propertyCallList;
//        }

//        protected override object DoEvaluate(ScriptThread thread)
//        {
//            Expression expression = ChildNodes[0].Evaluate(thread) as Expression;

//            // If it's not a global its a literal
//            if (thread.App.Globals.ContainsKey(expression.ToString().Replace("\"", "")))
//            {

//                var obj = thread.App.Globals[expression.ToString().Replace("\"", "")];
//                expression = Expression.Constant(obj);

//                for (int i = 1; i < ChildNodes.Count; i++)
//                {
//                    var prop = ChildNodes[i].Evaluate(thread);
//                    if (prop is Expression)
//                    {
//                        var name = prop.ToString();
//                        name = name.Replace("\"", "");

//                        try
//                        {
//                            expression = Expression.Property(expression, name);
//                        }
//                        catch(ArgumentException)
//                        {
//                            expression = Expression.Field(expression, name);
//                        }
//                    }
//                    else if (prop is PropertyCallNode)
//                    {
//                        PropertyCallNode call = prop as PropertyCallNode;

//                        if (call.IsMethod)
//                        {
//                            var name = call.Property.Replace("\"", "");
//                            string[] knownMethods = { "abs", "floor", "concat", "size", "substring" };

//                            List<Expression> args = new List<Expression>();
//                            foreach (ExpressionNode a in call.Arguments)
//                                args.Add((Expression)a.Evaluate(thread));

//                            if (knownMethods.Contains(name))
//                            {
//                                args.Insert(0, expression);

//                                expression = Expression.Call(
//                                    null,
//                                    typeof(KnownMethods).GetMethod(name),
//                                    args.ToArray());
//                            }
//                            else
//                            {
//                                expression = Expression.Call(
//                                    expression,
//                                    name,
//                                    null,
//                                    args.ToArray());
//                            }
//                        }
//                        else
//                        {
//                            throw new ArgumentOutOfRangeException("Shouldn't be here!");
//                        }
//                    }
//                }
//            }

//            return expression;
//        }
//    }

//    /// <summary>
//    /// Methods defined by the lanauge are implemented in this class
//    /// </summary>
//    public class KnownMethods
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public static int abs(int num)
//        {
//            return Math.Abs(num);
//        }

//        public static double floor(double num)
//        {
//            return Math.Floor(num);
//        }

//        public static int size(string str)
//        {
//            return str.Length;
//        }

//        public static string concat(string str1, string str2)
//        {
//            return str1 + str2;
//        }

//        public static string substring(string str1, int start, int len)
//        {
//            return str1.Substring(start, len);
//        }

//        public static int xor(int i1, int i2)
//        {
//            return i1 ^ i2;
//        }
//    }

//    public class PrimaryExpressionNode : OclAstNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            switch(treeNode.ChildNodes[0].ToString())
//            {
//                case "literalCollection":
//                    break;
//                case "literal":
//                    var lit = new LiteralNode();
//                    lit.Init(context, treeNode.ChildNodes[0]);
//                    ChildNodes.Add(lit);
//                    break;
//                case "propertyCall":
//                    var prop = new PropertyCallNode();
//                    prop.Init(context, treeNode.ChildNodes[0]);
//                    ChildNodes.Add(prop);
//                    break;
//                case "ifExpression":
//                    throw new NotImplementedException();
//                    //break;
//                default:
//                    //expression
//                    var expr = new ExpressionNode();
//                    expr.Init(context, treeNode.ChildNodes[1]);
//                    ChildNodes.Add(expr);
//                    break;
//            }

//            AsString = "Primary Expression";
//            if (ChildNodes.Count == 0)
//            {
//                AsString += " (Empty)";
//            }
//            else
//                ChildNodes[ChildNodes.Count - 1].Flags |= AstNodeFlags.IsTail;
//        }

//        protected override object DoEvaluate(ScriptThread thread)
//        {
//            thread.CurrentNode = this;  //standard prolog
//            object result = null;
			
//            for (int i = 0; i < ChildNodes.Count; i++)
//            {
//                result = ChildNodes[i].Evaluate(thread);
//            }

//            thread.CurrentNode = Parent; //standard epilog
//            return result; //return result of last statement
//        }
//    }

//    public enum LiteralType
//    {
//        Unknown,
//        String,
//        Number,
//        Enum
//    }

//    public class LiteralNode : OclAstNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public object Value;
//        public LiteralType LiteralType = LiteralType.Unknown;

//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            switch (treeNode.ChildNodes[0].Term.Name)
//            {
//                case "STRING":
//                    LiteralType = Ast.LiteralType.String;
//                    Value = treeNode.ChildNodes[0].Token.Value;
//                    break;
//                case "NUMBER":
//                    LiteralType = Ast.LiteralType.Number;
					
//                    try
//                    {
//                        Value = int.Parse(treeNode.ChildNodes[0].Token.Value.ToString());
//                    }
//                    catch(FormatException)
//                    {
//                        Value = double.Parse(treeNode.ChildNodes[0].Token.Value.ToString());
//                    }

//                    break;
//                default:
//                    LiteralType = Ast.LiteralType.Enum;
//                    throw new NotImplementedException();
//            }

//            AsString = "Literal (" + LiteralType.ToString() + ")";
//        }

//        protected override object DoEvaluate(ScriptThread thread)
//        {
//            return Expression.Constant(Value);
//        }

//        public override bool IsConstant()
//        {
//            return true;
//        }
//    }

//    public class PropertyCallNode : OclAstNode
//    {
//        static NLog.Logger logger = LogManager.GetCurrentClassLogger();
//        public string Property = "Unknown";
//        public object BoolValue = null;
//        public bool IsBoolean = false;
//        public bool IsProperty = false;
//        public bool IsMethod = true;
//        public List<ExpressionNode> Arguments = new List<ExpressionNode>();

//        public override void Init(AstContext context, ParseTreeNode treeNode)
//        {
//            base.Init(context, treeNode);
//            logger.Trace("Init");

//            Property = treeNode.ChildNodes[0].ChildNodes[0].Token.Text;

//            if(treeNode.ChildNodes[1].ChildNodes[0].ChildNodes.Count > 0)
//                Property += treeNode.ChildNodes[1].ChildNodes[0].ChildNodes[0].ChildNodes[0].Token.Text;

//            // Are we a method?
//            if (treeNode.ChildNodes[3].ChildNodes[0].ChildNodes.Count > 0)
//            {
//                IsMethod = true;

//                // Do we have parameters?
//                var paramList = treeNode.ChildNodes[3].ChildNodes[0].ChildNodes[0].ChildNodes[1].ChildNodes[0];

//                if (paramList.ChildNodes.Count > 0)
//                {
//                    foreach (ParseTreeNode item in paramList.ChildNodes[0].ChildNodes)
//                    {
//                        // item should be "expression"
//                        var expression = new ExpressionNode();
//                        expression.Init(context, item);
//                        Arguments.Add(expression);
//                    }
//                }
//            }
//            else if (Property == "false")
//            {
//                IsBoolean = true;
//                BoolValue = false;
//                Property = "Boolean";
//            }
//            else if (Property == "true")
//            {
//                IsBoolean = true;
//                BoolValue = true;
//                Property = "Boolean";
//            }
//            else
//            {
//                IsProperty = true;
//            }

//            AsString = "Property Call (" + Property + ")";
//        }

//        protected override object DoEvaluate(ScriptThread thread)
//        {
//            if(IsBoolean)
//                return Expression.Constant(BoolValue);

//            if(IsProperty)
//                return Expression.Constant(Property);

//            return this;
//        }

//        public override bool IsConstant()
//        {
//            return IsBoolean;
//        }
//    }
//}

//// end
