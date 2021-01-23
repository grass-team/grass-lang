﻿using System;
using System.Collections.Generic;

namespace grasslang.CodeModel
{
    public class Parser
    {
        public enum Priority
        {
            Lowest = 0,
            Assign = 1, // =
            Equals = 2, // ==, !=
            LessGreater = 3, // < ,>

            Index = 4, // array[0], map[0]
            Sum = 5, //+,-
            Product = 6,//*,/
            Prefix = 7, // !,-,+
            Call = 8, // func() 
        }
        public static Dictionary<Token.TokenType, Priority> PriorityMap = new Dictionary<Token.TokenType, Priority>
        {
            {Token.TokenType.Plus, Priority.Sum},
            {Token.TokenType.Minus, Priority.Sum},
            {Token.TokenType.Asterisk, Priority.Product},
            {Token.TokenType.Slash, Priority.Product},

            {Token.TokenType.LeftParen, Priority.Call},
            {Token.TokenType.LeftBrack, Priority.Index},
            {Token.TokenType.Identifier, Priority.Prefix },

            {Token.TokenType.Equal, Priority.Equals },
            {Token.TokenType.Dot, Priority.Equals}
        };
        public static Priority QueryPriority(Token token)
        {
            return QueryPriority(token.Type);
        }
        public static Priority QueryPriority(Token.TokenType type)
        {
            if (PriorityMap.ContainsKey(type))
            {
                return PriorityMap[type];
            }
            return Priority.Lowest;
        }

        // some properties about lexer
        public Lexer Lexer = null;
        private Token current
        {
            get
            {
                return Lexer.CurrentToken();
            }
        }
        private Token peek
        {
            get
            {
                return Lexer.PeekToken();
            }
        }
        private Token NextToken()
        {
            return Lexer.GetNextToken();
        }

        // parse to ast.
        public Ast BuildAst()
        {
            Ast result = new Ast();
            NextToken();
            while (Lexer.PeekToken().Type != Token.TokenType.Eof)
            {
                if(current.Type == Token.TokenType.Semicolon)
                {
                    NextToken();
                }
                Statement statement = parseStatement();
                if (statement != null)
                {
                    result.Root.Add(statement);
                }
                NextToken();
            }
            return result;
        }

        private Dictionary<Token.TokenType, Func<Expression>> prefixParserMap = new Dictionary<Token.TokenType, Func<Expression>>();
        private Func<Expression> getPrefixParserFunction(Token.TokenType type)
        {
            if (prefixParserMap.ContainsKey(type))
            {
                return prefixParserMap[type];
            }
            return null;
        }
        private Dictionary<Token.TokenType, Func<Expression, Expression>> infixParserMap = new Dictionary<Token.TokenType, Func<Expression, Expression>>();
        private Func<Expression, Expression> getInfixParserFunction(Token.TokenType type)
        {
            if (infixParserMap.ContainsKey(type))
            {
                return infixParserMap[type];
            }
            return null;
        }
        public void InitParser()
        {
            // add parser functions to map

            // prefix
            prefixParserMap[Token.TokenType.Function] = parseFunctionLiteral;
            prefixParserMap[Token.TokenType.Identifier] = parseIdentifierExpression;
            prefixParserMap[Token.TokenType.String] = parseStringExpression;
            prefixParserMap[Token.TokenType.Number] = parseNumberExpression;
            prefixParserMap[Token.TokenType.If] = parseIfExpression;
            prefixParserMap[Token.TokenType.While] = parseWhileExpression;
            prefixParserMap[Token.TokenType.Loop] = parseLoopExpression;
            prefixParserMap[Token.TokenType.True] = parseBooleanLiteral;
            prefixParserMap[Token.TokenType.False] = parseBooleanLiteral;
            prefixParserMap[Token.TokenType.New] = parseNewExpression;


            prefixParserMap[Token.TokenType.Plus] = parsePrefixExpression;
            prefixParserMap[Token.TokenType.Minus] = parsePrefixExpression;

            prefixParserMap[Token.TokenType.Class] = parseClassLiteral;
            prefixParserMap[Token.TokenType.Internal] = parseInternalCode;

            // infix
            infixParserMap[Token.TokenType.Colon] = parseDefinitionExpression;
            infixParserMap[Token.TokenType.Dot] = parsePathExpression;

            infixParserMap[Token.TokenType.Plus] = parseInfixExpression;
            infixParserMap[Token.TokenType.Minus] = parseInfixExpression;
            infixParserMap[Token.TokenType.Asterisk] = parseInfixExpression;
            infixParserMap[Token.TokenType.Slash] = parseInfixExpression;

            infixParserMap[Token.TokenType.Equal] = parseInfixExpression;
            infixParserMap[Token.TokenType.NotEqual] = parseInfixExpression;

            infixParserMap[Token.TokenType.LeftParen] = parseCallExpression;
            infixParserMap[Token.TokenType.Assign] = parseAssignExpression;
        }
        private Expression parseInternalCode()
        {
            return new InternalCode(current, current.Literal);
        }
        private Expression parseNewExpression()
        {
            NewExpression result = new NewExpression();
            NextToken();
            // parse the call of the constructor.
            Expression protoExpression = parseExpression(Priority.Lowest);
            if (protoExpression is CallExpression or PathExpression)
            {
                result.ctorCall = protoExpression;
            } else
            {
                throw new Exception("Syntax error, new xxx();");
            }
            return result;
        }
        private Expression parseBooleanLiteral()
        {
            return new BooleanLiteral(current.Type == Token.TokenType.True);
        }

        private Statement parseStatement()
        {
            switch (current.Type)
            {
                case Token.TokenType.Let:
                    {
                        return parseLetStatement();
                    }
                case Token.TokenType.Return:
                    {
                        return parseReturnStatement();
                    }
                case Token.TokenType.Import:
                    {
                        return parseImportStatement();
                    }
            }
            // parse expression
            return parseExpressionStatement();
        }
        private ImportStatement parseImportStatement()
        {
            NextToken();
            ImportStatement import = new ImportStatement();
            // parse return value.
            if(parseExpression(Priority.Lowest) is TextExpression textExpression)
            {
                import.Target = textExpression;
            } else
            {
                throw null;
            }
            return import;
        }
        private LetStatement parseLetStatement()
        {
            NextToken();
            LetStatement let = new LetStatement();
            
            if (parseExpression(Priority.Lowest) is DefinitionExpression definition)
            {
                let.Definition = definition;
            } else
            {
                return null;
            }

            return let;
        }
        private ReturnStatement parseReturnStatement()
        {
            NextToken();
            ReturnStatement returnStmt = new ReturnStatement();
            // parse return value.
            returnStmt.Value = parseExpression(Priority.Lowest);
            return returnStmt;
        }
        private Expression parseFunctionLiteral()
        {
            FunctionLiteral function = new FunctionLiteral();
            NextToken();


            // parse function name
            if (parseIdentifierExpression() is IdentifierExpression functionName)
            {
                function.FunctionName = functionName;
                NextToken();
            } else
            {
                function.Anonymous = true;
            }

            // parse function parameters
            if (current.Type != Token.TokenType.LeftParen)
            {
                return null;
            }
            function.Parameters = parseFunctionParameters();

            // parse function return type
            if (peek.Type == Token.TokenType.LeftBrace)
            {
                function.ReturnType = Expression.Void;
            } else if (peek.Type == Token.TokenType.Colon)
            {
                NextToken();
                NextToken();
                if (parseExpression(Priority.Lowest) is TextExpression type)
                {
                    function.ReturnType = type;
                }
            }
            NextToken();

            // parse function body
            if (parseBlockStatement() is BlockStatement block)
            {
                function.Body = block;
            }
            return function;
        }
        private List<DefinitionExpression> parseFunctionParameters()
        {
            List<DefinitionExpression> result = new List<DefinitionExpression>();
            if (peek.Type == Token.TokenType.RightParen)
            {
                // nothing in parameters.
                NextToken();
                return result;
            }

            do
            {
                NextToken();
                if (parseExpression(Priority.Lowest) is DefinitionExpression param)
                {
                    result.Add(param);
                    NextToken();
                } else
                {
                    return null;
                }
            } while (current.Type != Token.TokenType.RightParen);
            return result;
        }

        private BlockStatement parseBlockStatement()
        {
            if (current.Type != Token.TokenType.LeftBrace)
            {
                // check for a flag
                return null;
            }
            BlockStatement block = new BlockStatement();
            // parse statements in this block
            while (peek.Type != Token.TokenType.RightBrace)
            {
                NextToken();
                block.Body.Add(parseStatement());

                if (peek.Type != Token.TokenType.Semicolon && current.Type != Token.TokenType.RightBrace)
                {
                    return null;
                } else if (peek.Type == Token.TokenType.Semicolon)
                {
                    NextToken();
                }
            }
            NextToken();
            return block;
        }

        private ExpressionStatement parseExpressionStatement()
        {
            // parse expression
            Expression expression = parseExpression(Priority.Lowest);
            if (current.Type != Token.TokenType.RightBrace && peek.Type != Token.TokenType.Semicolon)
            {
                return null;
            }
            return new ExpressionStatement(expression);
        }
        private Expression parseExpression(Priority priority)
        {
            // find parser function in the map named prefixFunc
            Func<Expression> prefixFunc = getPrefixParserFunction(current.Type);
            if (prefixFunc == null)
            {
                // can't found the result, the syntax has some wrong
                return null;
            }
            Expression left = prefixFunc();

            while (peek.Type != Token.TokenType.Semicolon
                   && priority <= QueryPriority(peek.Type))
            {
                // find parser function in the map named infixFunc 
                Func<Expression, Expression> infixFunc = getInfixParserFunction(peek.Type);
                if (infixFunc == null)
                {
                    // can't found.
                    return left;
                }
                NextToken();
                left = infixFunc(left);
            }
            return left;
        }
        private IdentifierExpression parseIdentifierExpression()
        {
            // check for flag, and return the result
            return current.Type != Token.TokenType.Identifier ?
                null : new IdentifierExpression(current, current.Literal);
        }
        private StringLiteral parseStringExpression()
        {
            // check for flag, and return the result
            return current.Type != Token.TokenType.String ?
                null : new StringLiteral(current, current.Literal);
        }
        private NumberLiteral parseNumberExpression()
        {
            // check for flag, and return the result
            return current.Type != Token.TokenType.Number ?
                null : new NumberLiteral(current, current.Literal);
        }
        private PathExpression parsePathExpression(Expression left)
        {
            PathExpression path;
            // if the type of left is PathExpression
            if (left is PathExpression leftPath)
            {
                // push to the last of leftPath
                path = leftPath;
            } else
            {
                // create a PathExpression
                path = new PathExpression();
                path.Path.Add(left);
            }
            NextToken();
            path.Path.Add(parseExpression(Priority.Index));
            return path;
        }
        private IfExpression parseIfExpression()
        {
            IfExpression ifexpr = new IfExpression();
            NextToken();
            // parse condition
            ifexpr.Condition = parseExpression(Priority.Lowest);
            if (peek.Type != Token.TokenType.LeftBrace)
            {
                // parse block
                return null;
            }
            NextToken();
            ifexpr.Consequence = parseBlockStatement();
            return ifexpr;
        }
        private WhileExpression parseWhileExpression()
        {
            WhileExpression whileexpr = new WhileExpression();
            NextToken();
            whileexpr.Condition = parseExpression(Priority.Lowest);
            if (peek.Type != Token.TokenType.LeftBrace)
            {
                return null;
            }
            NextToken();
            whileexpr.Consequence = parseBlockStatement();
            return whileexpr;
        }
        private LoopExpression parseLoopExpression()
        {
            LoopExpression loopexpr = new LoopExpression();
            if (peek.Type != Token.TokenType.LeftBrace)
            {
                return null;
            }
            NextToken();
            loopexpr.Process = parseBlockStatement();
            return loopexpr;
        }
        private ClassLiteral parseClassLiteral()
        {
            ClassLiteral classLiteral = new ClassLiteral();
            NextToken();
            if(parseExpression(Priority.Equals) is IdentifierExpression typeName)
            {
                classLiteral.TypeName = typeName;
            } else
            {
                return null;
            }
            if(peek.Type == Token.TokenType.Colon)
            {

            }
            NextToken();
            if(parseBlockStatement() is BlockStatement block)
            {
                classLiteral.Body = block;
            }
            return classLiteral;
        }
        private PrefixExpression parsePrefixExpression()
        {
            PrefixExpression prefix = new PrefixExpression();
            prefix.Token = current;
            NextToken();
            prefix.Expression = parseExpression(Priority.Prefix);
            return prefix;
        }
        private InfixExpression parseInfixExpression(Expression left)
        {
            InfixExpression infix = new InfixExpression();
            infix.Left = left;
            infix.Operator = current;
            NextToken();
            infix.Right = parseExpression(QueryPriority(infix.Operator));
            return infix;
        }
        private CallExpression parseCallExpression(Expression left)
        {
            CallExpression call = new CallExpression();
            if(left is IdentifierExpression name)
            {
                call.Function = name;
            } else
            {
                throw new Exception("The name of the function is somewhat incorrect.");
            }
            call.Parameters = parseCallParameters();
            return call;
        }
        private AssignExpression parseAssignExpression(Expression left)
        {
            AssignExpression assign = new AssignExpression();
            assign.Left = left as TextExpression;
            NextToken();
            assign.Right = parseExpression(Priority.Assign);
            return assign;
        }
        private List<Expression> parseCallParameters()
        {
            List<Expression> result = new List<Expression>();
            if(peek.Type == Token.TokenType.RightParen)
            {
                NextToken();
                return result;
            }
            while (current.Type != Token.TokenType.RightParen)
            {
                NextToken();
                if (parseExpression(Priority.Lowest) is Expression param)
                {
                    result.Add(param);
                    if (peek.Type is not Token.TokenType.Comma and not Token.TokenType.RightParen)
                    {
                        return null;
                    }
                    NextToken();
                }
                else
                {
                    return null;
                }
            }
            return result;
        }
        private DefinitionExpression parseDefinitionExpression(Expression left)
        {
            DefinitionExpression definition = new DefinitionExpression();
            if (left is IdentifierExpression Name)
            {
                definition.Name = Name;
            } else
            {
                return null;
            }
            NextToken();
            if (parseExpression(Priority.Equals) is TextExpression type)
            {
                definition.ObjType = type;
            }
            else
            {
                return null;
            }
            // parse value
            if (peek.Type == Token.TokenType.Assign)
            {
                NextToken();
                NextToken();
                Expression value = parseExpression(Priority.Lowest);
                if (value == null)
                {
                    return null;
                }
                else
                {
                    definition.Value = value;
                }
            }
            return definition;
        }
    }
}
