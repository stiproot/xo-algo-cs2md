namespace Xo.Algo.Cs2Md;

internal class FileProvider : IProvider<string>
{
	public string Provide()
		=> "";
}

internal class MultiFileProvider : IProvider<IEnumerable<string>>
{
	public IEnumerable<string> Provide()
		=> new List<string>
		{
			""
		};
}
