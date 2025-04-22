namespace Forge.Test;

using System.Collections.Immutable;
using static LexToken.TokenType;
using static ParseTree.ConstructType;

public class ParserTests
{
	[Fact(DisplayName = "Empty File")]
	public void TestEmpty()
	{
		ParseTree parseTree = Assert.IsType<ParseTree>(ParseTokenStream());
		Assert.Equal("File()", parseTree.ToTestComparisonString());
	}

	[Theory(DisplayName = "Value Definition")]
	[InlineData($"{nameof(File)}({nameof(ValueDefinition)}({nameof(Modifiers)}(), {nameof(Keyword)}(let), {nameof(Identifier)}(varname), {nameof(Block)}()))",
					$"{nameof(Keyword)}|let|0|4", $"{nameof(Identifier)}|varname|4|12", $"{nameof(CurlyBracket)}|{{|12|13", $"{nameof(CurlyBracket)}|}}|13|14")]
    [InlineData($"{nameof(File)}({nameof(ValueDefinition)}({nameof(Modifiers)}(), {nameof(Identifier)}(int), {nameof(Identifier)}(varname), {nameof(Integer)}(5)))",
                    $"{nameof(Identifier)}|int|0|4", $"{nameof(Identifier)}|varname|4|12", $"{nameof(EqualSign)}|=|12|14", $"{nameof(Integer)}|5|14|15", $"{nameof(Semicolon)}|;|15|16")]
    public void TestValueDefinitions(string expected, params string[] tokens) =>
		RunTest(expected, tokens);

    [Theory(DisplayName = "Procedure Definition")]
    [InlineData($"{nameof(File)}({nameof(ProcedureDefinition)}({nameof(Modifiers)}(), {nameof(Keyword)}(let), {nameof(Identifier)}(varname), {nameof(RecordPattern)}(), {nameof(Block)}({nameof(Keyword)}(unreachable))))",
                    $"{nameof(Keyword)}|let|0|4", $"{nameof(Identifier)}|varname|4|12", $"{nameof(Parenthesis)}|(|12|13", $"{nameof(Parenthesis)}|)|13|15", $"{nameof(CurlyBracket)}|{{|15|17", $"{nameof(Keyword)}|unreachable|17|28", $"{nameof(Semicolon)}|;|28|30", $"{nameof(CurlyBracket)}|}}|30|31")]
    [InlineData($"{nameof(File)}({nameof(ProcedureDefinition)}({nameof(Modifiers)}(), {nameof(Identifier)}(int), {nameof(Identifier)}(varname), {nameof(RecordPattern)}(), {nameof(Integer)}(5)))",
                    $"{nameof(Identifier)}|int|0|4", $"{nameof(Identifier)}|varname|4|12", $"{nameof(Parenthesis)}|(|12|13", $"{nameof(Parenthesis)}|)|13|15", $"{nameof(EqualSign)}|=|15|17", $"{nameof(Integer)}|5|17|18", $"{nameof(Semicolon)}|;|18|19")]
    public void TestProcedureDefinitions(string expected, params string[] tokens) =>
        RunTest(expected, tokens);

    [Theory(DisplayName = "Type Definition")]
	[InlineData($"{nameof(File)}({nameof(TypeDefinition)}({nameof(Modifiers)}(), {nameof(Identifier)}(typename), {nameof(Identifier)}(int)))",
					$"{nameof(Keyword)}|type|0|5", $"{nameof(Identifier)}|typename|5|14", $"{nameof(EqualSign)}|=|14|16", $"{nameof(Identifier)}|int|16|20", $"{nameof(Semicolon)}|;|20|21")]
    public void TestTypeDefinitions(string expected, params string[] tokens) =>
		RunTest(expected, tokens);

    private static void RunTest(string expected, IEnumerable<string> tokens)
	{
		ParseTree parseTree = Assert.IsType<ParseTree>(ParseTokenStream(tokens.Select(static d => {
			string[] segments = d.Split('|');
			return new LexToken(Enum.Parse<LexToken.TokenType>(segments[0]), segments[1], (int.Parse(segments[2]), int.Parse(segments[3])));
		})));
		Assert.Equal(expected, parseTree.ToTestComparisonString());
	}

	private static ParseTree ParseTokenStream(params IEnumerable<LexToken> tokens)
	{
		ImmutableDictionary<int, ImmutableHashSet<LexToken>> input =
			tokens.GroupBy(static t => t.Extents.Start)
			.ToImmutableDictionary(static g => g.Key, static g => g.ToImmutableHashSet());

		return Parser.Parse(input);
	}
}
