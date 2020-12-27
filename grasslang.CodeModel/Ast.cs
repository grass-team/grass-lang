﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace grasslang.CodeModel
{
    public class Ast
    {
        public List<Node> Root = new List<Node>();
    }
    public class Node : ICloneable
    {
        public string Type = "";
        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    public class Statement : Node
    {

    }
    public class BlockStatement : Statement
    {
        public List<Node> Body = new List<Node>();
    }
    public class ExpressionStatement : Statement
    {
        public Expression Expression = null;

        public ExpressionStatement(Expression expression)
        {
            Expression = expression;
        }
    }

    public class Expression : Node
    {
        public static IdentifierExpression Void
        {
            get
            {
                return new IdentifierExpression(null, "void");
            }
        }
        public static IdentifierExpression Null
        {
            get
            {
                return new IdentifierExpression(null, "null");
            }
        }
    }
    public class PrefixExpression : Expression
    {
        public Token Token = null;
        public Expression Expression = null;

        public PrefixExpression(Token token, Expression expression)
        {
            Token = token;
            Expression = expression;
        }

        public PrefixExpression() { }
    }
    
    public class InfixExpression : Expression
    {
        public Token Operator = null;
        public Expression Left = null;
        public Expression Right = null;
        public InfixExpression(Token token, Expression leftExpression, Expression rightExpression)
        {
            Operator = token;
            Left = leftExpression;
            Right = rightExpression;
        }

        public InfixExpression()
        {
            
        }
    }

    public class TextExpression : Expression
    {
        public string Literal;
    }

    /// <summary>
    /// "xxx" or 'xxx'
    /// </summary>
    [DebuggerDisplay("StringLiteral = \"{Value}\"")]
    public class StringLiteral : Expression
    {
        public Token Token = null;
        public string Value = null;

        public StringLiteral(Token token, string value)
        {
            Token = token;
            Value = value;
        }
    }

    /// <summary>
    /// xxx.xxx().xxx
    /// </summary>
    [DebuggerDisplay("PathExpression = \"{Literal}\"")]
    public class PathExpression : TextExpression
    {
        public List<Expression> Path = new List<Expression>();
        // 生成新的PathExpression并剪裁其Path属性
        public PathExpression SubPath(int start, int length = 0)
        {
            PathExpression nextPathExpression = Clone() as PathExpression;
            List<Expression> nextPath = nextPathExpression.Path;
            if(length == 0)
            {
                length = nextPath.Count - start;
            } else if(length < 0)
            {
                length = nextPath.Count + length;
            }
            nextPath = nextPath.GetRange(start, length);
            nextPathExpression.Path = nextPath;
            return nextPathExpression;
        }
        public int Length
        {
            get
            {
                return Path.Count;
            }
        }
    }

    /// <summary>
    /// identifiers
    /// </summary>
    [DebuggerDisplay("IdentifierExpression = \"{Literal}\"")]
    public class IdentifierExpression : TextExpression
    {
        public Token Token = null;

        public IdentifierExpression(Token token, string literal)
        {
            Token = token;
            Literal = literal;
        }
    }

    /// <summary>
    /// foo(bar);
    /// </summary>
    [DebuggerDisplay("CallExpression = \"{FunctionName.Literal}\"")]
    public class CallExpression : Expression
    {
        public IdentifierExpression Function;
        public List<Expression> Parameters;
    }


    /// <summary>
    /// xxx: type = value;
    /// </summary>
    [DebuggerDisplay("DefinitionExpression = \"{Name.Literal} : {Type.Literal}\"")]
    public class DefinitionExpression : Expression
    {
        public IdentifierExpression Name;
        public Expression Value = null;
        public TextExpression ObjType;
        public DefinitionExpression()
        {

        }
        public DefinitionExpression(IdentifierExpression Name, TextExpression Type, Expression Value = null)
        {
            this.Name = Name;
            this.ObjType = Type;
            this.Value = Value;
        }
    }

    /// <summary>
    /// let xxx: type = value;
    /// </summary>
    public class LetStatement : Statement
    {
        public DefinitionExpression Definition;

        public LetStatement()
        {
            
        }
        public LetStatement(DefinitionExpression Definition)
        {
            this.Definition = Definition;
        }
    }

    /// <summary>
    /// return Value;
    /// </summary>
    public class ReturnStatement : Statement
    {
        public Expression Value = null;

        public ReturnStatement()
        {
            
        }
        public ReturnStatement(Expression value)
        {
            this.Value = value;
        }
    }

    /// <summary>
    /// fn func(): return type {}
    /// </summary>
    public class FunctionLiteral : Expression
    {
        public IdentifierExpression FunctionName = null;
        public List<DefinitionExpression> Parameters = new List<DefinitionExpression>();
        public BlockStatement Body = null;
        public TextExpression ReturnType;
        public bool Anonymous = false;
    }

    /// <summary>
    /// xxx = 123
    /// </summary>
    public class AssignExpression : Expression
    {
        public TextExpression Left;
        public Expression Right;
    }

    public class SubscriptExpression : Expression
    {
        public Expression Body;
        public Expression Subscript;
    }

    public class InternalCode : Expression
    {
        public Token Token = null;
        public string Value = null;

        public InternalCode(Token token, string value)
        {
            Token = token;
            Value = value;
        }
    }

    /// <summary>
    /// if Condition {
    ///
    /// } else {
    ///
    /// }
    /// </summary>
    public class IfExpression : Expression
    {
        public Expression Condition;
        public BlockStatement Consequence;
        public BlockStatement Alternative;
    }

    /// <summary>
    /// while Condition {
    ///
    /// }
    /// </summary>
    public class WhileExpression : Expression
    {
        public Expression Condition;
        public BlockStatement Consequence;
    }

    /// <summary>
    /// loop {
    ///
    /// }
    /// </summary>
    public class LoopExpression : Expression
    {
        public BlockStatement Process;
    }

    /// <summary>
    /// some numbers
    /// </summary>
    [DebuggerDisplay("NumberLiteral = {Value}")]
    public class NumberLiteral : Expression
    {
        public Token Token;
        public string Value;

        public NumberLiteral(Token token, string value)
        {
            Token = token;
            Value = value;
        }
    }


    /// <summary>
    /// true or false
    /// </summary>
    public class BooleanLiteral : Expression
    {
        public bool Value = false;

        public BooleanLiteral(bool value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// new xxxxx()
    /// </summary>
    public class NewExpression : Expression
    {
        public Expression ctorCall;
    }
}