namespace Forge;

using System.Collections.Immutable;
using static LexToken.TokenType;
using static ParseTree.ConstructType;

public class Parser
{
	public static ParseTree Parse(ImmutableDictionary<int, ImmutableHashSet<LexToken>> tokens)
	{
		if (tokens.IsEmpty)
			return new(File, [], (0, 0));

		int idx = tokens.Keys.Min();

		List<ParseTree> definitions = [];

		while (tokens.TryGetValue(idx, out var tokenSet))
		{
			void setIdx(int newIdx) => (idx, tokenSet) = (newIdx, tokens.TryGetValue(newIdx, out var newSet) ? newSet : []);
			void step(LexToken pastToken) => setIdx(pastToken.Extents.End);

			ParseNode parseTypeExpr(ParseNode? passForwards = null)
			{
				ParseNode retval;

				if (tokenSet.SingleOrDefault(static s => s.Type is Identifier && s.Lexeme is "ptr") is LexToken ptrToken)
				{
					// Pointer
					step(ptrToken);
					retval = new ParseTree(PointerType, passForwards is null ? [] : [passForwards], (passForwards?.Extents.Start ?? ptrToken.Extents.Start, ptrToken.Extents.End));
				}
				else if (tokenSet.SingleOrDefault(static s => s.Type is Identifier) is LexToken typeId)
				{
					// Type identifier
					step(typeId);
					retval = new ParseLeaf(typeId);
				}
				else
					throw new NotImplementedException();

				if (tokenSet.Any(static s => s.Type is Identifier && s.Lexeme is "ptr"))
					return parseTypeExpr(retval);
				else
					return retval;
			}

			ParseNode parsePattern()
			{
				ParseNode retval;

				if (tokenSet.SingleOrDefault(static s => s.Type is Identifier) is LexToken patternId)
				{
					// Identifier
					step(patternId);
					retval = new ParseLeaf(patternId);
				}
				else if (tokenSet.SingleOrDefault(static s => s.Type is Integer or Decimal or Character or String or Boolean or Poison) is LexToken patternLiteral)
				{
					// Literal
					step(patternLiteral);
					retval = new ParseLeaf(patternLiteral);
				}
				else if (tokenSet.Any(static s => (s.Type, s.Lexeme) is (Parenthesis, "(")))
					// Record pattern
					retval = parseRecordPattern();
				else
					throw new NotImplementedException();

				while (tokenSet.SingleOrDefault(static s => s.Type is Colon) is LexToken colonKw)
				{
					// Type tag
					step(colonKw);
					var typeExpr = parseTypeExpr();

					retval = new ParseTree(TypeTag, [retval, typeExpr], (retval.Extents.Start, typeExpr.Extents.End));
				}

				return retval;
			}

			ParseNode parseBody()
			{
				if (tokenSet.SingleOrDefault(static s => s.Type is EqualSign) is LexToken exprEqualSign)
				{
					step(exprEqualSign);
					var expr = parseExpr();

					if (tokenSet.SingleOrDefault(static s => s.Type is Semicolon) is not LexToken endSemi)
						throw new ArgumentException("Expected a semicolon at the end of the definition.", nameof(tokens));

					step(endSemi);
					return expr with { Extents = (exprEqualSign.Extents.Start, endSemi.Extents.End) };
				}
				else if (tokenSet.Any(static s => (s.Type, s.Lexeme) is (CurlyBracket, "{")))
					return parseBlock();
				else
					throw new ArgumentException("Missing a block body for the definition.", nameof(tokens));
			}

			ParseTree parseBlock()
			{
				if (tokenSet.SingleOrDefault(static s => (s.Type, s.Lexeme) is (CurlyBracket, "{")) is not LexToken openBrace)
					throw new ArgumentException("Blocks must start with a curly brace!");

				step(openBrace);
				List<ParseNode> statements = [];

				while (tokenSet.Count > 0 && !tokenSet.Any(static s => (s.Type, s.Lexeme) is (CurlyBracket, "}")))
				{
					ParseNode statement;

					if (tokenSet.Any(static s => s.Type is CurlyBracket))
						// Nested block
						statement = parseBlock();
					else if (tokenSet.SingleOrDefault(static s => (s.Type, s.Lexeme) is (Keyword, "return")) is LexToken returnKw)
					{
						step(returnKw);
						ParseNode retExpr = parseExpr();

						if (tokenSet.SingleOrDefault(static s => s.Type is Semicolon) is not LexToken closingSemi)
							throw new ArgumentException("Return statement must end in a semicolon.", nameof(tokens));

						statement = new ParseTree(ReturnStatement, [retExpr], (returnKw.Extents.Start, closingSemi.Extents.End));
					}
					else if (tokenSet.SingleOrDefault(static s => (s.Type, s.Lexeme) is (Keyword, "unreachable")) is LexToken unreachableKw)
					{
						step(unreachableKw);
						statement = new ParseLeaf(unreachableKw);

						if (tokenSet.SingleOrDefault(static s => s.Type is Semicolon) is not LexToken closingSemi)
							throw new ArgumentException("Unreachable statement must end in a semicolon.", nameof(tokens));

						// TODO: Figure out where to put the extents of the semicolon.
						step(closingSemi);
					}
					else
						throw new NotImplementedException();

					statements.Add(statement);
				}

				if (tokenSet.Count is 0 || tokenSet.SingleOrDefault(static s => (s.Type, s.Lexeme) is (CurlyBracket, "}")) is not LexToken closeBrace)
					throw new ArgumentException("Blocks must end with a curly brace!", nameof(tokens));

				step(closeBrace);
				return new(Block, [.. statements], (openBrace.Extents.Start, closeBrace.Extents.End));
			}

			ParseNode parseExpr()
			{
				ParseNode exprItem;

				if (tokenSet.SingleOrDefault(static s => s.Type is Identifier or Integer or Decimal or Character or String or Boolean or Poison) is LexToken idHeadOrLiteral)
				{
					// Qualified identifiers, literals, and procedure calls.
					var priorStateForQualifiedId = (idx, tokenSet);
					step(idHeadOrLiteral);

					if (idHeadOrLiteral.Type is Identifier && tokenSet.Any(static s => (s.Type, s.Lexeme) is (Parenthesis, "(")))
					{
						// Procedure call.
						ParseLeaf procId = new(idHeadOrLiteral);
						ParseTree argument = parseRecordExpr();

						return new ParseTree(ProcedureCall, [procId, argument], (procId.Extents.Start, argument.Extents.End));
					}
					else if (idHeadOrLiteral.Type is not Identifier && !tokenSet.Any(static s => s.Type is Dot))
						// Literal
						return new ParseLeaf(idHeadOrLiteral);
					else
					{
						// Qualified identifier
						(idx, tokenSet) = priorStateForQualifiedId;
						return parseQualifiedId();
					}

					throw new NotImplementedException();
				}
				else if (tokenSet.Any(static s => (s.Type, s.Lexeme) is (Parenthesis, "(")))
					// Record expression.
					exprItem = parseRecordExpr();
				else if (tokenSet.SingleOrDefault(static s => (s.Type, s.Lexeme) is (Keyword, "if")) is LexToken ifKw)
				{
					// Conditional
					step(ifKw);
					var condition = parseExpr();
					var consequent = tokenSet.Any(static s => (s.Type, s.Lexeme) is (CurlyBracket, "{")) ? parseBlock() : parseExpr();

					if (tokenSet.SingleOrDefault(static s => (s.Type, s.Lexeme) is (Keyword, "else")) is null)
						throw new ArgumentException("Conditional must have an else branch.", nameof(tokens));

					if (tokenSet.Any(static s => (s.Type, s.Lexeme) is (CurlyBracket, "{")))
					{
						var alternative = parseBlock();
						return new ParseTree(Conditional, [condition, consequent, alternative], (ifKw.Extents.Start, alternative.Extents.End));
					}
					else
					{
						var alternative = parseExpr();
						if (tokenSet.SingleOrDefault(static s => s.Type is Semicolon) is not LexToken closingSemi)
							throw new ArgumentException("Missing closing semicolon after expression in else block of conditional.", nameof(tokens));

						return new ParseTree(Conditional, [condition, consequent, alternative], (ifKw.Extents.Start, closingSemi.Extents.End));
					}
				}
				else if (tokenSet.SingleOrDefault(static s => (s.Type, s.Lexeme) is (Keyword, "map")) is LexToken mapKw)
				{
					// Map
					step(mapKw);
					var binding = parsePattern();

					if (tokenSet.SingleOrDefault(static s => (s.Type, s.Lexeme) is (Keyword, "over")) is null)
						throw new ArgumentException("Missing keyword over after pattern in map expression.", nameof(tokens));

					var collection = parseExpr();
					var transformation = parseBody();
					return new ParseTree(Map, [binding, collection, transformation], (mapKw.Extents.Start, transformation.Extents.End));
				}
				else
					throw new NotImplementedException();

				if (tokenSet.SingleOrDefault(static s => s.Type is Colon) is LexToken typeTagColon)
				{
					// Type tag.
					step(typeTagColon);
					var typeTag = parseTypeExpr();

					return new ParseTree(TypeTag, [exprItem, typeTag], (exprItem.Extents.Start, typeTag.Extents.End));
				}

				return exprItem;
			}

			ParseTree parseRecordPattern()
			{
				if (tokenSet.SingleOrDefault(static s => (s.Type, s.Lexeme) is (Parenthesis, "(")) is not LexToken openingParens)
					throw new ArgumentException("Record pattern must begin with an opening parenthesis.", nameof(tokens));

				List<ParseNode> subPatterns = [];

				while (tokenSet.SingleOrDefault(static s => s.Type is Parenthesis or Comma) is LexToken delimiter && (delimiter.Type, delimiter.Lexeme) is not (Parenthesis, ")"))
				{
					step(delimiter);

					if (tokenSet.Any(static s => (s.Type, s.Lexeme) is (Parenthesis, ")")))
						// Terminating commas _are_ legal.
						break;
					if (tokenSet.SingleOrDefault(static s => s.Type is Identifier or Integer or Decimal or Character or String or Boolean or Poison) is LexToken key)
					{
						// Either a key or a trivial pattern. Step over it and see if it's a key that needs to be handled.
						step(key);

						if (tokenSet.SingleOrDefault(static s => s.Type is EqualSign) is LexToken literalEqualSign)
						{
							// Yep; it's a key/pattern pair alright!
							step(literalEqualSign);
							var pattern = parsePattern();
							subPatterns.Add(new ParseTree(RecordPatternItem, [new ParseLeaf(key), pattern], (key.Extents.Start, pattern.Extents.End)));
						}
						else
							// Just a basic id or literal!
							subPatterns.Add(new ParseLeaf(key));
					}
					else
						// More complex sub-pattern. Probably a nested record or something like that.
						subPatterns.Add(parsePattern());
				}

				if (tokenSet.SingleOrDefault(static s => (s.Type, s.Lexeme) is (Parenthesis, ")")) is not LexToken closingParens)
					throw new ArgumentException("Record pattern must end with a closing parenthesis.", nameof(tokens));

				step(closingParens);

				return new(RecordPattern, [.. subPatterns], (openingParens.Extents.Start, closingParens.Extents.End));
			}

			ParseTree parseRecordExpr()
			{
				throw new NotImplementedException();
			}

			ParseNode parseQualifiedId()
			{
				if (tokenSet.SingleOrDefault(static s => s.Type is Identifier or Integer or Decimal or Character or String or Boolean or Poison) is not LexToken key)
					throw new ArgumentException("Identifier must have a key.", nameof(tokens));

				if (!tokenSet.Any(static s => s.Type is Dot))
				{
					// Bare identifier
					if (key.Type is not Identifier)
						throw new ArgumentException("Cannot use an unqualified literal as an identifier.", nameof(tokens));

					return new ParseLeaf(key);
				}

				ParseNode retval = new ParseLeaf(key);

				while (tokenSet.SingleOrDefault(static s => s.Type is Dot) is LexToken dotLiteral)
				{
					step(dotLiteral);

					if (tokenSet.SingleOrDefault(static s => s.Type is Identifier or Integer or Decimal or Character or String or Boolean or Poison) is not LexToken member)
						throw new ArgumentException("Dot in identifier must be followed by another key.", nameof(tokens));

					step(member);
					retval = new ParseTree(QualifiedIdentifier, [retval, new ParseLeaf(member)], (retval.Extents.Start, member.Extents.End));
				}

				return retval;
			}

			// Parse a definition.
			if (tokenSet.SingleOrDefault(static s => s.Type is Modifier) is LexToken modifierToken)
				throw new NotImplementedException();

			if (tokenSet.SingleOrDefault(static s => s.Type is Keyword && s.Lexeme is "type") is LexToken typeKwToken)
			{
				// Parse a type definition.
				step(typeKwToken);

				if (tokenSet.SingleOrDefault(static s => s.Type is Identifier) is not LexToken typeId)
					throw new ArgumentException("Type definition must have an identifier.", nameof(tokens));

				step(typeId);

				if (tokenSet.SingleOrDefault(static s => s.Type is EqualSign) is not LexToken equalsToken)
					throw new ArgumentException("Type identifier must be followed by an equals sign.", nameof(tokens));

				step(equalsToken);
				var typeExpr = parseTypeExpr();

				if (tokenSet.SingleOrDefault(static s => s.Type is Semicolon) is not LexToken semicolonToken)
					throw new ArgumentException("Type definitions must end in a semicolon.", nameof(tokens));

				step(semicolonToken);
				definitions.Add(new(TypeDefinition, [
					new ParseTree(Modifiers, [], (typeKwToken.Extents.Start, typeKwToken.Extents.Start)),
					new ParseLeaf(typeId),
					typeExpr
				], (typeKwToken.Extents.Start, semicolonToken.Extents.End)));
			}
			else if (tokenSet.SingleOrDefault(static s => (s.Type, s.Lexeme) is (Keyword, "let") || s.Type is Identifier) is LexToken defType)
			{
				step(defType);
				var bindPattern = parsePattern();

				if (bindPattern is ParseLeaf { Token.Type: Identifier } && tokenSet.Any(static s => (s.Type, s.Lexeme) is (Parenthesis, "(")))
				{
					// Procedure definition
					var parameter = parsePattern();
					var body = parseBody();
					definitions.Add(new(ProcedureDefinition, [
						new ParseTree(Modifiers, [], (defType.Extents.Start, defType.Extents.Start)),
						new ParseLeaf(defType),
						bindPattern,
						parameter,
						body
					], (defType.Extents.Start, body.Extents.End)));
				}
				else
				{
					// Value definition
					var body = parseBody();
					definitions.Add(new(ValueDefinition, [
						new ParseTree(Modifiers, [], (defType.Extents.Start, defType.Extents.Start)),
						new ParseLeaf(defType),
						bindPattern,
						body
					], (defType.Extents.Start, body.Extents.End)));
				}
			}
			else
				throw new NotImplementedException();
		}

		return new(File, [.. definitions], (definitions.Min(static d => d.Extents.Start), definitions.Max(static d => d.Extents.End)));
	}
}

public abstract record ParseNode((int Start, int End) Extents)
{
	public abstract string ToTestComparisonString();
}

public sealed record ParseTree(ParseTree.ConstructType Construct, ParseNode[] Children, (int Start, int End) Extents) : ParseNode(Extents)
{
	public enum ConstructType
	{
		File,
		ValueDefinition,
		ProcedureDefinition,
		TypeDefinition,
		Modifiers,
		Pattern,
		RecordPattern,
		RecordPatternItem,
		TypeTag,
		QualifiedIdentifier,
		ProcedureCall,
		RecordExpression,
		RecordExpressionItem,
		Conditional,
		Map,
		TypeRecord,
		TypeRecordItem,
		PointerType,
		Block,
		ReturnStatement
	}

	public override string ToTestComparisonString() => $"{Construct}({string.Join(", ", Children.Select(static c => c.ToTestComparisonString()))})";
}
public sealed record ParseLeaf(LexToken Token) : ParseNode(Token.Extents)
{
	public override string ToTestComparisonString() => $"{Token.Type}({Token.Lexeme.Trim()})";
}
