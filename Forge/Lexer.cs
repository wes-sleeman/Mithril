namespace Forge;

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using static LexToken.TokenType;

public static partial class Lexer
{
	public static ImmutableDictionary<int, ImmutableHashSet<LexToken>> Lex(string program)
	{
		Dictionary<int, ImmutableHashSet<LexToken>> retval = [];

		for (int leftIdx = 0; leftIdx < program.Length;)
		{
			HashSet<LexToken> current = [];

			string testString = program[leftIdx..];

			Match m;

			foreach ((Regex regex, LexToken.TokenType type) in (IEnumerable<(Regex, LexToken.TokenType)>)[
				(IntLiteral(), Integer),
				(DecimalLiteral(), Decimal),
				(IdentifierRegex(), Identifier),
				(CharLiteral(), Character),
				(StringLiteral(), String),
				(BoolLiteral(), Boolean),
				(PoisonLiteral(), Poison),
				(SemicolonLiteral(), Semicolon),
				(ColonLiteral(), Colon),
				(EqualSignLiteral(), EqualSign),
				(ParenthesisLiteral(), Parenthesis),
				(CurlyBracketLiteral(), CurlyBracket),
				(CommaLiteral(), Comma),
				(DotLiteral(), Dot),
				(KeywordLiteral(), Keyword),
				(ModifierLiteral(), Modifier),
				])
				if ((m = regex.Match(testString)).Success)
					current.Add(new(type, m.Value, (leftIdx, leftIdx + m.Length + testString[m.Length..].TakeWhile(char.IsWhiteSpace).Count())));

			if (current.Count is 0)
			{
				++leftIdx;
				continue;
			}

			// Identifiers may not also be literals.
			if (current.Count > 1 && current.Any(static i => i.Type is Identifier) && current.Any(static i => i.Type is Integer or Decimal or Character or String or Boolean or Poison))
				current.RemoveWhere(static i => i.Type is Identifier);

			retval[leftIdx] = [.. current];

			leftIdx = current.Max(static t => t.Extents.End);
		}

		return retval.ToImmutableDictionary();
	}

	[GeneratedRegex(@"\A(`[^`]+`|[^\s=.,:;\p{Ps}\p{Pe}\p{Pi}\p{Pf}]+(?![^\s=.,:;\p{Ps}\p{Pe}\p{Pi}\p{Pf}]))", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
	private static partial Regex IdentifierRegex();

	[GeneratedRegex(@"\A-?\d+(?![^\s=,:;\p{Ps}\p{Pe}\p{Pi}\p{Pf}])", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
	private static partial Regex IntLiteral();

	[GeneratedRegex(@"\A-?(\d+\.\d*|\.\d+)(?![^\s=,:;\p{Ps}\p{Pe}\p{Pi}\p{Pf}])", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
	private static partial Regex DecimalLiteral();

	[GeneratedRegex(@"\A'([^'\\]|\\.)'", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
	private static partial Regex CharLiteral();

	[GeneratedRegex(@"\A""([^""\\]|\\.)*""", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
	private static partial Regex StringLiteral();

	[GeneratedRegex(@"\A(true|false)(?![^\s=.,:;\p{Ps}\p{Pe}\p{Pi}\p{Pf}])", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
	private static partial Regex BoolLiteral();

	[GeneratedRegex(@"\Apoison(?![^\s=.,:;\p{Ps}\p{Pe}\p{Pi}\p{Pf}])", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
	private static partial Regex PoisonLiteral();

	[GeneratedRegex(@"\A;", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
	private static partial Regex SemicolonLiteral();

	[GeneratedRegex(@"\A:", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
	private static partial Regex ColonLiteral();

	[GeneratedRegex(@"\A=", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
	private static partial Regex EqualSignLiteral();

	[GeneratedRegex(@"\A[)(]", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
	private static partial Regex ParenthesisLiteral();

	[GeneratedRegex(@"\A[}{]", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
	private static partial Regex CurlyBracketLiteral();

	[GeneratedRegex(@"\A,", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
	private static partial Regex CommaLiteral();

	[GeneratedRegex(@"\A\.", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
	private static partial Regex DotLiteral();

	[GeneratedRegex(@"\A(let|if|else|map|over|unreachable|return)(?![^\s=.,:;\p{Ps}\p{Pe}\p{Pi}\p{Pf}])", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
	private static partial Regex KeywordLiteral();

    [GeneratedRegex(@"\A(public|internal)(?![^\s=.,:;\p{Ps}\p{Pe}\p{Pi}\p{Pf}])", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
    private static partial Regex ModifierLiteral();
}

/// <summary>A token produced by the <see cref="Lexer"/>.</summary>
/// <param name="Type">The token discovered.</param>
/// <param name="Lexeme">The unmodified lexeme of the token.</param>
/// <param name="Extents">The extents at which the token was found, including any trailing whitespace.</param>
public record LexToken(LexToken.TokenType Type, string Lexeme, (int Start, int End) Extents)
{
	public enum TokenType
	{
		Keyword,
		Modifier,
		Semicolon,
		Colon,
		EqualSign,
		Parenthesis,
		CurlyBracket,
		Comma,
		Dot,
		Integer,
		Decimal,
		Character,
		String,
		Boolean,
		Poison,
		Identifier
	}
}
