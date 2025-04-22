namespace Forge.Test;

using static LexToken.TokenType;
public class LexerTests
{
	[Theory(DisplayName = "Single Lexeme (non-identifier)")]
	[InlineData(";", Semicolon)]
	[InlineData(":", Colon)]
	[InlineData("=", EqualSign)]
	[InlineData("(", Parenthesis)]
	[InlineData(")", Parenthesis)]
	[InlineData("{", CurlyBracket)]
	[InlineData("}", CurlyBracket)]
	[InlineData(",", Comma)]
	[InlineData("5", Integer)]
	[InlineData("-10000", Integer)]
	[InlineData(".0", Decimal)]
	[InlineData("0.", Decimal)]
	[InlineData("-.0", Decimal)]
	[InlineData("-0.", Decimal)]
	[InlineData("'a'", Character)]
	[InlineData("'b'", Character)]
	[InlineData(@"'\a'", Character)]
	[InlineData(@"'\b'", Character)]
	[InlineData(@"'\''", Character)]
	[InlineData("\"This is a test\"", String)]
	[InlineData("\"This is \\a test\"", String)]
	[InlineData("\"This is an\\b test\"", String)]
	[InlineData("\"This has \\\" a quote\"", String)]
	[InlineData("true", Boolean)]
	[InlineData("false", Boolean)]
	[InlineData("poison", Poison)]
	public void TestSingle(string program, LexToken.TokenType expectedToken)
	{
		var result = Lexer.Lex(program);
		var kvp = Assert.Single(result);
		Assert.Equal(0, kvp.Key);
		int maxIdx = kvp.Value.Max(static v => v.Extents.End);
		var tokenType = Assert.Single(kvp.Value, v => v.Extents.End == maxIdx && v.Type is not Identifier).Type;
		Assert.Equal(expectedToken, tokenType);
	}

	[Theory(DisplayName = "Single Identifier")]
	[InlineData("hello")]
	[InlineData("`this is a longer identifier with spaces`")]
	[InlineData("`[](){}'\"\\.,;:=」`")]
	[InlineData("publicly")]
	[InlineData("minternal")]
	[InlineData("`public`")]
	[InlineData("`let`")]
	public void TestSingleIdentifier(string program)
    {
        var result = Lexer.Lex(program);
        var kvp = Assert.Single(result);
        Assert.Equal(0, kvp.Key);
        var tokenType = Assert.Single(kvp.Value).Type;
        Assert.Equal(Identifier, tokenType);
    }

	[Theory(DisplayName = "String of Unambiguous Tokens")]
	[InlineData("int x = 5;", Identifier, Identifier, EqualSign, Integer, Semicolon)]
	[InlineData("{ string variable = \"This is a test\" : string; }", CurlyBracket, Identifier, Identifier, EqualSign, String, Colon, Identifier, Semicolon, CurlyBracket)]
	public void TestMultiToken(string program, params LexToken.TokenType[] expectedTokens)
    {
        var result = Lexer.Lex(program);
		Assert.Equal(expectedTokens.Length, result.Count);
		LexToken.TokenType[][] tokenSets = [..result.OrderBy(static kvp => kvp.Key).Select(static kvp => kvp.Value.Select(static v => v.Type).ToArray())];
		
		for (int idx = 0; idx < tokenSets.Length; ++idx)
			Assert.Equal(expectedTokens[idx], Assert.Single(tokenSets[idx]));
    }

	[Theory(DisplayName = "Single Token with Ambiguous Lexicalisation")]
	[InlineData("let", Keyword, Identifier)]
	[InlineData("if", Keyword, Identifier)]
	[InlineData("else", Keyword, Identifier)]
	[InlineData("map", Keyword, Identifier)]
	[InlineData("over", Keyword, Identifier)]
	[InlineData("unreachable", Keyword, Identifier)]
	[InlineData("return", Keyword, Identifier)]
	[InlineData("public", Modifier, Identifier)]
	[InlineData("internal", Modifier, Identifier)]
	public void TestAmbiguousToken(string program, params LexToken.TokenType[] options)
	{
		var result = Lexer.Lex(program);
		Assert.Equal(0, Assert.Single(result.Keys));
		var tokenSet = Assert.Single(result.Values);
		Assert.Equal(options.OrderBy(static o => o.ToString()), tokenSet.Select(static t => t.Type).OrderBy(static o => o.ToString()));
	}
}
