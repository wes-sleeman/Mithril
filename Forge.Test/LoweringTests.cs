namespace Forge.Test;

using Forge.Lowering;

public class LoweringTests
{
	[Theory(DisplayName = "Empty Program")]
	[InlineData(0)]
	[InlineData(2)]
	public void TestEmptyProgram(int emptyFileCount)
	{
		List<ParseTree> data = [];

		for (int file = 0; file < emptyFileCount; ++file)
			data.Add(new(ParseTree.ConstructType.File, [], (0, 0)));

		Assert.Empty(SyntaxTree.ParseProgram([.. data]));
	}

	[Fact(DisplayName = "Simple Expression Value Definition")]
	public void TestValueDefinition()
	{
		var definition = Assert.Single(SyntaxTree.ParseProgram(new ParseTree(ParseTree.ConstructType.File, [
			new ParseTree(ParseTree.ConstructType.ValueDefinition, [
				new ParseTree(ParseTree.ConstructType.Modifiers, [
					new ParseLeaf(new(LexToken.TokenType.Modifier, "public", (0, 7)))
				], (0, 7)),
				new ParseLeaf(new(LexToken.TokenType.Keyword, "let", (7, 11))),
				new ParseLeaf(new(LexToken.TokenType.Identifier, "x", (11, 13))),
				new ParseLeaf(new(LexToken.TokenType.Integer, "39", (15, 17)))
			], (0, 18))
		], (0, 18))));

		var valueDef = Assert.IsType<ValueDefinition>(definition);

		Assert.Equal("x", valueDef.DefinedIdentifier);
		Assert.Equal(Definition.Encapsulation.Public, valueDef.Visibility);
		Assert.Equal((0, 18), valueDef.Extents);
		Assert.Equal(new Integer(39, (15, 17)), Assert.IsType<Integer>(valueDef.Value));
	}

	[Fact(DisplayName = "Compound Expression Value Definition")]
	public void TestCompoundValueDefinition()
	{
		var definition = Assert.Single(SyntaxTree.ParseProgram(new ParseTree(ParseTree.ConstructType.File, [
			new ParseTree(ParseTree.ConstructType.ValueDefinition, [
				new ParseTree(ParseTree.ConstructType.Modifiers, [
					new ParseLeaf(new(LexToken.TokenType.Modifier, "public", (0, 7)))
				], (0, 7)),
				new ParseLeaf(new(LexToken.TokenType.Keyword, "let", (7, 11))),
				new ParseTree(ParseTree.ConstructType.RecordPattern, [
					new ParseLeaf(new(LexToken.TokenType.Identifier, "a", (12, 13))),
					new ParseLeaf(new(LexToken.TokenType.Identifier, "b", (15, 16)))
				], (11, 17)),
				new ParseTree(ParseTree.ConstructType.RecordExpression, [
					new ParseLeaf(new(LexToken.TokenType.Integer, "3", (21, 22))),
					new ParseLeaf(new(LexToken.TokenType.Integer, "9", (24, 25))),
				], (20, 26))
			], (0, 27))
		], (0, 27))));

		var valueDef = Assert.IsType<ValueDefinition>(definition);

		Assert.Null(valueDef.DefinedIdentifier);
		Assert.Equal(Definition.Encapsulation.Public, valueDef.Visibility);
		Assert.Equal((0, 27), valueDef.Extents);

		var bindingPattern = Assert.IsType<RecordPattern>(valueDef.BindingPattern);
		Assert.All(bindingPattern.Items, static kvp => Assert.IsType<EmptyRecordKey>(kvp.Key));
		Assert.Collection(bindingPattern.Items, static first => Assert.Equal(new PatternId("a", null, (12, 13)), first.Value), static second => Assert.Equal(new PatternId("b", null, (15, 16)), second.Value));
		Assert.Equal((11, 17), bindingPattern.Extents);

		var bindingExpr = Assert.IsType<RecordExpression>(valueDef.Value);
		Assert.All(bindingExpr.Items, static kvp => Assert.IsType<EmptyRecordKey>(kvp.Key));
		Assert.Collection(bindingExpr.Items, static first => Assert.Equal(new Integer(3, (21, 22)), first.Value), static second => Assert.Equal(new Integer(9, (24, 25)), second.Value));
		Assert.Equal((20, 26), bindingExpr.Extents);
	}

	[Fact(DisplayName = "Simple Expression Procedure Definition")]
	public void TestProcedureExprDefinition()
	{
		var definition = Assert.Single(SyntaxTree.ParseProgram(new ParseTree(ParseTree.ConstructType.File, [
			new ParseTree(ParseTree.ConstructType.ProcedureDefinition, [
				new ParseTree(ParseTree.ConstructType.Modifiers, [
					new ParseLeaf(new(LexToken.TokenType.Modifier, "public", (0, 7)))
				], (0, 7)),
				new ParseLeaf(new(LexToken.TokenType.Keyword, "let", (7, 11))),
				new ParseLeaf(new(LexToken.TokenType.Identifier, "id", (11, 13))),
				new ParseTree(ParseTree.ConstructType.RecordPattern, [ new ParseLeaf(new(LexToken.TokenType.Identifier, "x", (14, 15))) ], (13, 17)),
				new ParseLeaf(new(LexToken.TokenType.Identifier, "x", (19, 20)))
			], (0, 21))
		], (0, 21))));

		var procDef = Assert.IsType<ProcedureDefinition>(definition);

		Assert.Equal("id", procDef.DefinedIdentifier);
		Assert.Equal(Definition.Encapsulation.Public, procDef.Visibility);
		Assert.Equal((0, 21), procDef.Extents);

		var param = Assert.Single(procDef.Parameter.Items);
		Assert.IsType<EmptyRecordKey>(param.Key);
		Assert.Equal(new("x", null, (14, 15)), Assert.IsType<PatternId>(param.Value));

		var body = Assert.IsType<Block>(procDef.Body);
		Assert.Equal(new Access("x", (19, 20)), Assert.IsType<ReturnStatement>(Assert.Single(body.Statements)).Expression);
	}

	[Fact(DisplayName = "Simple Block Procedure Definition")]
	public void TestProcedureBlockDefinition()
	{
		var definition = Assert.Single(SyntaxTree.ParseProgram(new ParseTree(ParseTree.ConstructType.File, [
			new ParseTree(ParseTree.ConstructType.ProcedureDefinition, [
				new ParseTree(ParseTree.ConstructType.Modifiers, [
					new ParseLeaf(new(LexToken.TokenType.Modifier, "public", (0, 7)))
				], (0, 7)),
				new ParseLeaf(new(LexToken.TokenType.Keyword, "let", (7, 11))),
				new ParseLeaf(new(LexToken.TokenType.Identifier, "id", (11, 13))),
				new ParseTree(ParseTree.ConstructType.RecordPattern, [ new ParseLeaf(new(LexToken.TokenType.Identifier, "x", (14, 15))) ], (13, 17)),
				new ParseTree(ParseTree.ConstructType.Block, [
					new ParseTree(ParseTree.ConstructType.ReturnStatement, [
						new ParseLeaf(new(LexToken.TokenType.Identifier, "x", (26, 27)))
					], (19, 29))
				], (17, 30))
			], (0, 30))
		], (0, 30))));

		var procDef = Assert.IsType<ProcedureDefinition>(definition);

		Assert.Equal("id", procDef.DefinedIdentifier);
		Assert.Equal(Definition.Encapsulation.Public, procDef.Visibility);
		Assert.Equal((0, 30), procDef.Extents);

		var param = Assert.Single(procDef.Parameter.Items);
		Assert.IsType<EmptyRecordKey>(param.Key);
		Assert.Equal(new("x", null, (14, 15)), Assert.IsType<PatternId>(param.Value));

		var body = Assert.IsType<Block>(procDef.Body);
		Assert.Equal(new Access("x", (26, 27)), Assert.IsType<ReturnStatement>(Assert.Single(body.Statements)).Expression);
	}

	[Fact(DisplayName = "Unreachable Procedure Definition")]
	public void TestUnreachableProcedureDefinition()
	{
		var definition = Assert.Single(SyntaxTree.ParseProgram(new ParseTree(ParseTree.ConstructType.File, [
			new ParseTree(ParseTree.ConstructType.ProcedureDefinition, [
				new ParseTree(ParseTree.ConstructType.Modifiers, [
					new ParseLeaf(new(LexToken.TokenType.Modifier, "public", (0, 7)))
				], (0, 7)),
				new ParseLeaf(new(LexToken.TokenType.Keyword, "let", (7, 11))),
				new ParseLeaf(new(LexToken.TokenType.Identifier, "fail", (11, 15))),
				new ParseTree(ParseTree.ConstructType.RecordPattern, [], (15, 18)),
				new ParseTree(ParseTree.ConstructType.Block, [
					new ParseLeaf(new(LexToken.TokenType.Keyword, "unreachable", (20, 31)))
				], (18, 34))
			], (0, 34))
		], (0, 34))));

		var procDef = Assert.IsType<ProcedureDefinition>(definition);

		Assert.Equal("fail", procDef.DefinedIdentifier);
		Assert.Equal(Definition.Encapsulation.Public, procDef.Visibility);
		Assert.Equal((0, 34), procDef.Extents);
		Assert.Empty(procDef.Parameter.Items);

		var body = Assert.IsType<Block>(procDef.Body);
		Assert.Equal(new UnreachableStatement((20, 31)), Assert.Single(body.Statements));
	}

	[Fact(DisplayName = "Complex Block Procedure Definition")]
	public void TestProcedureComplexBlockDefinition()
	{
		var definition = Assert.Single(SyntaxTree.ParseProgram(new ParseTree(ParseTree.ConstructType.File, [
			new ParseTree(ParseTree.ConstructType.ProcedureDefinition, [
				new ParseTree(ParseTree.ConstructType.Modifiers, [
					new ParseLeaf(new(LexToken.TokenType.Modifier, "public", (0, 7)))
				], (0, 7)),
				new ParseLeaf(new(LexToken.TokenType.Keyword, "let", (7, 11))),
				new ParseLeaf(new(LexToken.TokenType.Identifier, "fst", (11, 14))),
				new ParseTree(ParseTree.ConstructType.RecordPattern, [ new ParseLeaf(new(LexToken.TokenType.Identifier, "l", (15, 16))) ], (14, 18)),
				new ParseTree(ParseTree.ConstructType.Block, [
					new ParseTree(ParseTree.ConstructType.ValueDefinition, [
						new ParseTree(ParseTree.ConstructType.Modifiers, [], (19, 19)),
						new ParseLeaf(new(LexToken.TokenType.Keyword, "let", (19, 23))),
						new ParseTree(ParseTree.ConstructType.RecordPattern, [
							new ParseLeaf(new(LexToken.TokenType.Identifier, "h", (24, 25))),
							new ParseLeaf(new(LexToken.TokenType.Identifier, "t", (27, 28)))
						], (23, 30)),
						new ParseLeaf(new(LexToken.TokenType.Identifier, "l", (32, 33)))
					], (19, 36)),
					new ParseTree(ParseTree.ConstructType.ReturnStatement, [
						new ParseLeaf(new(LexToken.TokenType.Identifier, "h", (43, 44)))
					], (36, 45))
				], (18, 47))
			], (0, 47))
		], (0, 47))));

		var procDef = Assert.IsType<ProcedureDefinition>(definition);

		Assert.Equal("fst", procDef.DefinedIdentifier);
		Assert.Equal(Definition.Encapsulation.Public, procDef.Visibility);
		Assert.Equal((0, 47), procDef.Extents);

		var param = Assert.Single(procDef.Parameter.Items);
		Assert.IsType<EmptyRecordKey>(param.Key);
		Assert.Equal(new("l", null, (15, 16)), Assert.IsType<PatternId>(param.Value));

		var body = Assert.IsType<Block>(procDef.Body);
		Assert.Collection(body.Statements,
			static spreader => {
				var def = Assert.IsType<ValueDefinition>(Assert.IsType<BindingStatement>(spreader).Binding);
				Assert.Equal(Definition.Encapsulation.Private, def.Visibility);

				var bind = Assert.IsType<RecordPattern>(def.BindingPattern);
				Assert.All(bind.Items, static kvp => Assert.IsType<EmptyRecordKey>(kvp.Key));
				Assert.Collection(bind.Items,
					static fst => Assert.Equal(new PatternId("h", null, (24, 25)), fst.Value),
					static snd => Assert.Equal(new PatternId("t", null, (27, 28)), snd.Value)
				);
			},
			static returner => Assert.Equal(new Access("h", (43, 44)), Assert.IsType<ReturnStatement>(returner).Expression)
		);
	}

	[Fact(DisplayName = "Type Alias")]
	public void TestTypeAlias()
	{
		var definition = Assert.Single(SyntaxTree.ParseProgram(new ParseTree(ParseTree.ConstructType.File, [
			new ParseTree(ParseTree.ConstructType.TypeDefinition, [
				new ParseTree(ParseTree.ConstructType.Modifiers, [new ParseLeaf(new(LexToken.TokenType.Modifier, "public", (0, 7)))], (0, 7)),
				new ParseLeaf(new(LexToken.TokenType.Identifier, "myType", (12, 19))),
				new ParseTree(ParseTree.ConstructType.PointerType, [
				new ParseLeaf(new(LexToken.TokenType.Identifier, "int", (21, 25)))
				], (21, 28))
			], (0, 29))
		], (0, 29))));

		var typeDef = Assert.IsType<TypeDefinition>(definition);
		Assert.Equal(Definition.Encapsulation.Public, typeDef.Visibility);
		Assert.Equal("myType", typeDef.DefinedIdentifier);
		Assert.Equal((0, 29), typeDef.Extents);

		var type = Assert.IsType<PointerType>(typeDef.Definition);
		Assert.Equal("int", Assert.IsType<TypeId>(type.PointeeType).Identifier);
		Assert.Equal((21, 28), type.Extents);
	}

	[Fact(DisplayName = "Record Type Definition")]
	public void TestRecordTypeDefinition()
	{
		var definition = Assert.Single(SyntaxTree.ParseProgram(new ParseTree(ParseTree.ConstructType.File, [
			new ParseTree(ParseTree.ConstructType.TypeDefinition, [
				new ParseTree(ParseTree.ConstructType.Modifiers, [new ParseLeaf(new(LexToken.TokenType.Modifier, "public", (0, 7)))], (0, 7)),
				new ParseLeaf(new(LexToken.TokenType.Identifier, "myType", (12, 19))),
				new ParseTree(ParseTree.ConstructType.TypeRecord, [
					new ParseLeaf(new(LexToken.TokenType.Identifier, "int", (22, 25)))
				], (21, 26))
			], (0, 27))
		], (0, 27))));

		var typeDef = Assert.IsType<TypeDefinition>(definition);
		Assert.Equal(Definition.Encapsulation.Public, typeDef.Visibility);
		Assert.Equal("myType", typeDef.DefinedIdentifier);
		Assert.Equal((0, 27), typeDef.Extents);

		var type = Assert.IsType<RecordType>(typeDef.Definition);
		var kvp = Assert.Single(type.Items);
		Assert.IsType<EmptyRecordKey>(kvp.Key);
		Assert.Equal("int", Assert.IsType<TypeId>(kvp.Value).Identifier);
		Assert.Equal((21, 26), type.Extents);
	}
}
