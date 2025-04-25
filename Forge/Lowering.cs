/// <summary>Lowers parse trees into abstract syntax trees which can be type-checked and emitted as LLVM IR.</summary>
namespace Forge.Lowering;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

[method: DebuggerStepThrough()]
public abstract record SyntaxTree((int Start, int End) Extents)
{
	public static IEnumerable<Definition> ParseProgram(params ParseTree[] files)
	{
		SymbolTable rootSymbolTable = new();
		List<(SymbolTable FileSymbols, HashSet<Definition> Definitions)> definitions = [];

		foreach (ParseTree file in files)
		{
			if (file.Children.Length is 0)
				continue;

			SymbolTable symbols = new(rootSymbolTable);
			HashSet<Definition> fileDefinitions = [];

			foreach (ParseTree definition in file.Children.Cast<ParseTree>())
				fileDefinitions.Add(definition.Construct switch {
					ParseTree.ConstructType.ValueDefinition => ValueDefinition.Parse(definition),
					ParseTree.ConstructType.ProcedureDefinition => ProcedureDefinition.Parse(definition),
					ParseTree.ConstructType.TypeDefinition => TypeDefinition.Parse(definition),
					_ => throw new NotImplementedException()
				});

			definitions.Add((symbols, fileDefinitions));
		}

		return definitions.SelectMany(static d => d.Definitions);
	}
}

[method: DebuggerStepThrough()]
public abstract record Definition(Definition.Encapsulation Visibility, string? DefinedIdentifier, (int Start, int End) Extents) : SyntaxTree(Extents)
{
	public enum Encapsulation
	{
		Private,
		Internal,
		Public
	}
}

[method: DebuggerStepThrough()]
public sealed record ValueDefinition(Definition.Encapsulation Visibility, TypeExpression Type, Pattern BindingPattern, Expression Value, (int Start, int End) Extents) : Definition(Visibility, BindingPattern is PatternId pid ? pid.Identifier : null, Extents)
{
	public static ValueDefinition Parse(ParseTree definition)
	{
		if (definition.Construct is not ParseTree.ConstructType.ValueDefinition)
			throw new ArgumentException("Value definition expected.", nameof(definition));

		var (modifierStr, defType, bindPattern, value) = (
			((ParseTree)definition.Children[0]).Children.Cast<ParseLeaf>().SingleOrDefault()?.Token.Lexeme,
			TypeExpression.Parse(definition.Children[1]),
			Pattern.Parse(definition.Children[2]),
			Expression.Parse(definition.Children[3])
		);

		Encapsulation modifier = modifierStr switch {
			"public" => Encapsulation.Public,
			"internal" => Encapsulation.Internal,
			_ => Encapsulation.Private
		};

		return new(modifier, defType, bindPattern, value, definition.Extents);
	}
}

[method: DebuggerStepThrough()]
public sealed record ProcedureDefinition(Definition.Encapsulation Visibility, TypeExpression Type, string DefinedIdentifier, RecordPattern Parameter, Block Body, (int Start, int End) Extents) : Definition(Visibility, DefinedIdentifier, Extents)
{
	public static ProcedureDefinition Parse(ParseTree definition)
	{
		if (definition.Construct is not ParseTree.ConstructType.ProcedureDefinition)
			throw new ArgumentException("Procedure definition expected.", nameof(definition));

		var (modifierStr, defType, id, parameter, value) = (
			((ParseTree)definition.Children[0]).Children.Cast<ParseLeaf>().SingleOrDefault()?.Token.Lexeme,
			TypeExpression.Parse(definition.Children[1]),
			((ParseLeaf)definition.Children[2]).Token.Lexeme,
			RecordPattern.Parse(definition.Children[3]),
			Block.Parse(definition.Children[4])
		);

		Encapsulation modifier = modifierStr switch {
			"public" => Encapsulation.Public,
			"internal" => Encapsulation.Internal,
			_ => Encapsulation.Private
		};

		return new(modifier, defType, id, parameter, value, definition.Extents);
	}
}

[method: DebuggerStepThrough()]
public sealed record TypeDefinition(Definition.Encapsulation Visibility, string DefinedIdentifier, TypeExpression Definition, (int Start, int End) Extents) : Definition(Visibility, DefinedIdentifier, Extents)
{
	public static TypeDefinition Parse(ParseTree definition)
	{
		if (definition.Construct is not ParseTree.ConstructType.TypeDefinition)
			throw new ArgumentException("Type definition expected.", nameof(definition));

		var (modifierStr, id, def) = (
			((ParseTree)definition.Children[0]).Children.Cast<ParseLeaf>().SingleOrDefault()?.Token.Lexeme,
			((ParseLeaf)definition.Children[1]).Token.Lexeme,
			TypeExpression.Parse(definition.Children[2])
		);

		Encapsulation modifier = modifierStr switch {
			"public" => Encapsulation.Public,
			"internal" => Encapsulation.Internal,
			_ => Encapsulation.Private
		};

		return new(modifier, id, def, definition.Extents);
	}
}

[method: DebuggerStepThrough()]
public abstract record Expression((int Start, int End) Extents) : SyntaxTree(Extents)
{
	public static Expression Parse(ParseNode expr)
	{
		if (expr is ParseLeaf { Token.Type: LexToken.TokenType.Identifier } exprLeaf)
			return new Access(exprLeaf.Token.Lexeme, exprLeaf.Token.Extents);
		else if (expr is ParseLeaf { Token.Type: LexToken.TokenType.Integer or LexToken.TokenType.Decimal or LexToken.TokenType.Character or LexToken.TokenType.String or LexToken.TokenType.Boolean or LexToken.TokenType.Poison } exprLitLeaf)
			return Literal.Parse(exprLitLeaf);
		else if (expr is ParseTree { Construct: ParseTree.ConstructType.RecordExpression })
			return RecordExpression.Parse(expr);
		else
			throw new NotImplementedException();
	}
}

[method: DebuggerStepThrough()]
public sealed record Block(ImmutableArray<Statement> Statements, (int Start, int End) Extents) : SyntaxTree(Extents)
{
	public static Block Parse(ParseNode block)
	{
		if (block is ParseTree { Construct: ParseTree.ConstructType.Block } blk)
			return new([.. blk.Children.Select(static c => Statement.Parse(c))], blk.Extents);
		else
			return new([new ReturnStatement(Expression.Parse(block), block.Extents)], block.Extents);
	}
}

public abstract record Statement((int Start, int End) Extents) : SyntaxTree(Extents)
{
	public static Statement Parse(ParseNode stmt)
	{
		if (stmt is ParseTree { Construct: ParseTree.ConstructType.ReturnStatement } retStmt)
			return new ReturnStatement(Expression.Parse(retStmt.Children.Single()), retStmt.Extents);
		else if (stmt is ParseLeaf { Token.Type: LexToken.TokenType.Keyword, Token.Lexeme: "unreachable" } unreachableStmt)
			return new UnreachableStatement(stmt.Extents);
		else if (stmt is ParseTree { Construct: ParseTree.ConstructType.ValueDefinition } bindStmt)
			return new BindingStatement(ValueDefinition.Parse(bindStmt), bindStmt.Extents);
		else
			throw new NotImplementedException();
	}
}

public sealed record BindingStatement(ValueDefinition Binding, (int Start, int End) Extents) : Statement(Extents);
public sealed record ExpressionStatement(Expression Expression, (int Start, int End) Extents) : Statement(Extents);
public sealed record ReturnStatement(Expression Expression, (int Start, int End) Extents) : Statement(Extents);
public sealed record UnreachableStatement((int Start, int End) Extents) : Statement(Extents);


[method: DebuggerStepThrough()]
public abstract record TypeExpression((int Start, int End) Extents) : SyntaxTree(Extents)
{
	public static TypeExpression Parse(ParseNode expr)
	{
		if (expr is ParseLeaf exprLeaf)
		{
			if (exprLeaf.Token.Type is LexToken.TokenType.Identifier)
				return new TypeId(exprLeaf.Token.Lexeme, exprLeaf.Extents);
			else if (exprLeaf.Token.Type is LexToken.TokenType.Keyword && exprLeaf.Token.Lexeme is "let")
				return new InferredType(exprLeaf.Extents);
		}
		else if (expr is ParseTree { Construct: ParseTree.ConstructType.PointerType } ptrTree)
			return new PointerType(Parse(ptrTree.Children.Single()), ptrTree.Extents);
		else if (expr is ParseTree { Construct: ParseTree.ConstructType.TypeRecord } recordTree)
			return RecordType.Parse(recordTree);

		throw new NotImplementedException();
	}
}

[method: DebuggerStepThrough()]
public sealed record InferredType((int Start, int End) Extents) : TypeExpression(Extents);

[method: DebuggerStepThrough()]
public sealed record TypeId(string Identifier, (int Start, int End) Extents) : TypeExpression(Extents);

[method: DebuggerStepThrough()]
public sealed record PointerType(TypeExpression PointeeType, (int Start, int End) Extents) : TypeExpression(Extents);

[method: DebuggerStepThrough()]
public sealed record RecordType(ImmutableArray<(IRecordKey Key, TypeExpression Value)> Items, (int Start, int End) Extents) : TypeExpression(Extents)
{
	public new static RecordType Parse(ParseNode typeRecord)
	{
		if (typeRecord is not ParseTree { Construct: ParseTree.ConstructType.TypeRecord } type)
			throw new ArgumentException("Type must be a record.", nameof(typeRecord));

		List<(IRecordKey Key, TypeExpression Value)> items = [];

		foreach (ParseNode item in type.Children)
		{
			if (item is ParseTree { Construct: ParseTree.ConstructType.TypeRecordItem } tri)
				throw new NotImplementedException();
			else
				items.Add((new EmptyRecordKey(), TypeExpression.Parse(item)));
		}

		return new([.. items], type.Extents);
	}
}


public interface IRecordKey;
[method: DebuggerStepThrough()]
public sealed record EmptyRecordKey() : IRecordKey;

public record Access(string Identifier, (int Start, int End) Extents) : Expression(Extents), IRecordKey;

[method: DebuggerStepThrough()]
public abstract record Literal((int Start, int End) Extents) : Expression(Extents), IRecordKey
{
	public new static Literal Parse(ParseNode literal)
	{
		if (literal is not ParseLeaf leaf)
			throw new ArgumentException("Literal must be a leaf.", nameof(literal));

		return leaf.Token.Type switch {
			LexToken.TokenType.Integer => new Integer(int.Parse(leaf.Token.Lexeme), leaf.Extents),
			_ => throw new NotImplementedException()
		};
	}
}

[method: DebuggerStepThrough()]
public sealed record Integer(int Value, (int Start, int End) Extents) : Literal(Extents);

[method: DebuggerStepThrough()]
public sealed record RecordExpression(ImmutableArray<(IRecordKey Key, Expression Value)> Items, (int Start, int End) Extents) : Expression(Extents)
{
	public new static RecordExpression Parse(ParseNode expression)
	{
		if (expression is not ParseTree { Construct: ParseTree.ConstructType.RecordExpression } expr)
			throw new ArgumentException("Expression must be a record.", nameof(expression));

		List<(IRecordKey Key, Expression Value)> items = [];

		foreach (ParseNode item in expr.Children)
		{
			if (item is ParseLeaf { Token.Type: LexToken.TokenType.Identifier } idLeaf)
				items.Add((new EmptyRecordKey(), new Access(idLeaf.Token.Lexeme, idLeaf.Extents)));
			else if (item is ParseLeaf litLeaf)
				items.Add((new EmptyRecordKey(), Literal.Parse(litLeaf)));
			else throw new NotImplementedException();
		}

		return new([.. items], expr.Extents);
	}
}

[method: DebuggerStepThrough()]
public abstract record Pattern(TypeExpression? TypeTag, (int Start, int End) Extents) : SyntaxTree(Extents)
{
	public static Pattern Parse(ParseNode pattern)
	{
		if (pattern is ParseLeaf { Token.Type: LexToken.TokenType.Identifier } patLeaf)
			return new PatternId(patLeaf.Token.Lexeme, null, patLeaf.Token.Extents);
		else if (pattern is ParseLeaf { Token.Type: LexToken.TokenType.Integer or LexToken.TokenType.Decimal or LexToken.TokenType.Character or LexToken.TokenType.String or LexToken.TokenType.Boolean or LexToken.TokenType.Poison } patLitLeaf)
			return PatternLiteral.Parse(patLitLeaf);
		else if (pattern is ParseTree { Construct: ParseTree.ConstructType.RecordPattern })
			return RecordPattern.Parse(pattern);
		else if (pattern is ParseTree { Construct: ParseTree.ConstructType.TypeTag } tagTree)
			return Parse(tagTree.Children[0]) with { TypeTag = TypeExpression.Parse(tagTree.Children[1]) };
		else
			throw new ArgumentException("Patterns must be identifiers, literals, or records.", nameof(pattern));
	}
}

[method: DebuggerStepThrough()]
public sealed record PatternId(string Identifier, TypeExpression? TypeTag, (int Start, int End) Extents) : Pattern(TypeTag, Extents);

[method: DebuggerStepThrough()]
public sealed record PatternLiteral(Literal Value, TypeExpression? TypeTag, (int Start, int End) Extents) : Pattern(TypeTag, Extents)
{
	public new static PatternLiteral Parse(ParseNode literal)
	{
		if (literal is not ParseLeaf leaf)
			throw new ArgumentException("Literal must be a leaf.", nameof(literal));

		return leaf.Token.Type switch {
			LexToken.TokenType.Integer => new(new Integer(int.Parse(leaf.Token.Lexeme), leaf.Extents), null, leaf.Extents),
			_ => throw new NotImplementedException()
		};
	}
}

[method: DebuggerStepThrough()]
public sealed record RecordPattern(ImmutableArray<(IRecordKey Key, Pattern Value)> Items, TypeExpression? TypeTag, (int Start, int End) Extents) : Pattern(TypeTag, Extents)
{
	public new static RecordPattern Parse(ParseNode pattern)
	{
		if (pattern is not ParseTree { Construct: ParseTree.ConstructType.RecordPattern } pat)
			throw new ArgumentException("Pattern must be a record.", nameof(pattern));

		List<(IRecordKey Key, Pattern Value)> items = [];

		foreach (ParseNode item in pat.Children)
		{
			if (item is ParseLeaf { Token.Type: LexToken.TokenType.Identifier } idLeaf)
				items.Add((new EmptyRecordKey(), new PatternId(idLeaf.Token.Lexeme, null, idLeaf.Extents)));
			else if (item is ParseLeaf litLeaf)
				items.Add((new EmptyRecordKey(), PatternLiteral.Parse(litLeaf)));
			else throw new NotImplementedException();
		}

		return new([.. items], null, pat.Extents);
	}
}
