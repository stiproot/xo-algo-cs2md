var provider = new MultiFileProvider();

foreach (var file in provider.Provide())
{
	var proc = new SyntaxTreeProc();

	await proc.Init(new SyntaxTreeCmd { FilePath = file });

	var res = proc.Process() as SyntaxTreeRes;

	var path = Path.Combine(Directory.GetCurrentDirectory(), "src", "Output", $"{Path.GetFileName(file)}.txt");

	await File.WriteAllTextAsync(path, res.Data);
}