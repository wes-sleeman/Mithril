namespace Forge.Test;

using Construct = ParseTree.ConstructType;

public class SemanticTests
{
	[Fact(DisplayName = "Empty File")]
	public void TestEmptyFile()
	{
		// Ensure a program of no files returns no definitions.
		Assert.Empty(Semantics.Elaborator.Elaborate());

		//// Ensure a single file with no content returns no definitions.
		//Assert.Empty(Semantics.Elaborator.Elaborate(new ParseTree(Construct.File, [], (0, 0))));

		//// Ensure that only files are accepted.
		//Assert.Throws<ArgumentException>(static () => Semantics.Elaborator.Elaborate(new ParseTree(Construct.ValueDefinition, [], (0, 0))));
	}
}
