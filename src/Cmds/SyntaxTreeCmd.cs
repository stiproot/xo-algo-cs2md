namespace Xo.Algo.Cs2Md;

internal record SyntaxTreeCmd : IProcCmd
{
	public string FilePath { get; init; } = string.Empty;
}