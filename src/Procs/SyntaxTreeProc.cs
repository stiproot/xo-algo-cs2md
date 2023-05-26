namespace Xo.Algo.Cs2Md;

internal class SyntaxTreeProc : IProc<SyntaxTreeCmd, SyntaxTreeRes>
{
	private SyntaxTree _syntaxTree;
	private CompilationUnitSyntax _root;
	private readonly StringBuilder _builder = new StringBuilder("sequenceDiagram\n");
	private List<MethodDeclarationSyntax> _methods;
	private readonly IDictionary<string, MethodDeclarationSyntax> _methodHash = new Dictionary<string, MethodDeclarationSyntax>();
	const string PATTERN = @"(?:await\s+)?(?:this\.)?(\w+)\(";
	const string ARG_COUNT_PATTERN = @"(?:await\s+)?(?:this\.)?\w+\((.*?)\)";
	private static Regex _re = new Regex(PATTERN);
	private static Regex _argCountRe = new Regex(ARG_COUNT_PATTERN);
	private string Key(string m, int i) => $"{m}({i})";

	public async Task Init(SyntaxTreeCmd cmd)
	{
		string code = await File.ReadAllTextAsync(cmd.FilePath);
		this._syntaxTree = CSharpSyntaxTree.ParseText(code);
		this._root = this._syntaxTree.GetCompilationUnitRoot();
		this._methods =
			(
				from node in this._root.DescendantNodes()
				where node is MethodDeclarationSyntax
				select node as MethodDeclarationSyntax
			)
			.ToList();
	}

	internal static IEnumerable<SyntaxNode> GetBlocks(this SyntaxNode node)
	{
		if (node is not MethodDeclarationSyntax) throw new InvalidOperationException("node is not MethodDeclarationSyntax");

		var blocks = node.DescendantNodes().OfType<BlockSyntax>().ToList();

 // what is BlockSyntax?

	}



	public SyntaxTreeRes Process()
	{
		foreach (var m in this._methods)
		{
			string name = m.Identifier.Text;
			var paramCount = m.ParameterList.Parameters.Count();
			string key = Key(name, paramCount);

			if (!this._methodHash.TryGetValue(key, out var _))
			{
				// this._builder.AppendLine($"participant {key}");
				this._methodHash.Add(key, m);
			}
		}

		this._builder.AppendLine($"participant {this._methodHash.First().Key}");
		this._builder.AppendLine($"activate {this._methodHash.First().Key}");

		foreach (var m in this._methods)
		{
			ProcMethod(m);
		}

		this._builder.AppendLine($"deactivate {this._methodHash.First().Key}");

		return new SyntaxTreeRes { Data = this._builder.ToString() };
	}

	private void ProcMethod(MethodDeclarationSyntax node) => ProcessMethodSyntax(node);

	private (List<string>, List<string>) _TraverseAncestors(SyntaxNode n)
	{
		List<string> prepend = new List<string>();
		List<string> append = new List<string>();

		var x =
		(
			from a in n.Ancestors()
			where a is ForEachStatementSyntax || a is ElseClauseSyntax || a is IfStatementSyntax || a is WhileStatementSyntax
			select a
		).ToList();

		x.ForEach(_n =>
		{
			switch (_n)
			{
				case ForEachStatementSyntax w:
					{
						prepend.Add($"loop {w}");
						append.Add("end");
						break;
					}
				case ElseClauseSyntax w:
					{
						if (w != null && w.Parent is IfStatementSyntax _i)
						{
							prepend.Add($"alt (!) {_i.Condition}");
							append.Add("end");
						}
						break;
					}
				case IfStatementSyntax w:
					{
						prepend.Add($"alt {w.Condition}");
						append.Add("end");
						break;
					}
				case WhileStatementSyntax w:
					{
						prepend.Add($"loop {w.Condition}");
						append.Add("end");
						break;
					}
				default: break;
			}
		});

		return (prepend, append);
	}

	private (string, string)? TraverseAncestors(
		SyntaxNode n
	)
	{
		var e = n.Ancestors().OfType<ElseClauseSyntax>().FirstOrDefault();
		if (e != null && e.Parent is IfStatementSyntax _i) return ($"alt (!) {_i.Condition}", "end");

		var i = n.Ancestors().OfType<IfStatementSyntax>().FirstOrDefault();
		if (i != null) return ($"alt {i.Condition}", "end");

		return null;
	}

	public void ProcessMethodSyntax(MethodDeclarationSyntax root)
	{
		string rootName = root.Identifier.ValueText;
		int rootParamCount = root.ParameterList.Parameters.Count();
		string rootKey = Key(rootName, rootParamCount);
		var descendants = root.DescendantNodes();
		// this._builder.AppendLine($"activate {rootKey}");

		void AddToDiag(
			SyntaxNode n,
			MethodDeclarationSyntax x,
			string a1,
			string a2,
			string msg,
			string resp
		)
		{
			this._builder.AppendLine($"participant {a2}");

			var (prepend, append) = _TraverseAncestors(n);

			if (prepend.Any())
			{
				foreach (var v in prepend)
				{
					this._builder.AppendLine(v);
				}
			}

			this._builder.AppendLine($"{a1}->>+{a2}: {msg}");
			ProcessMethodSyntax(x);
			this._builder.AppendLine($"{a2}-->>-{a1}: {resp}");

			if (append.Any())
			{
				foreach (var v in append)
				{
					this._builder.AppendLine(v);
				}
			}

			//var ifDecorator = TraverseAncestors(n);
			//if (ifDecorator != null)
			//{
			//this._builder.AppendLine(ifDecorator.Value.Item1);
			//}

			//this._builder.AppendLine($"{a1}->>+{a2}: {msg}");
			//ProcessMethodSyntax(x);
			//this._builder.AppendLine($"{a2}-->>-{a1}: {resp}");

			//if (ifDecorator != null)
			//{
			//this._builder.AppendLine(ifDecorator.Value.Item2);
			//}
		}

		foreach (var n in descendants)
		{
			if (n is AssignmentExpressionSyntax a)
			{
				if (a.DescendantNodes().OfType<InvocationExpressionSyntax>().Any())
				{
					var r = ProcessSyntaxNode(n);
					var methodName = GetMethodName(r);
					int argCount = GetArgCount(r);
					string key = Key(methodName, argCount);

					if (this._methodHash.TryGetValue(key, out var _syntax))
					{
						AddToDiag(
							n,
							_syntax,
							rootKey,
							key,
							r,
							"result"
						);
					}
				}
			}
			else if (n is InvocationExpressionSyntax i)
			{
				var r = ProcessSyntaxNode(n);
				var methodName = GetMethodName(r);
				int argCount = GetArgCount(r);
				string key = Key(methodName, argCount);

				if (this._methodHash.TryGetValue(key, out var _syntax))
				{
					AddToDiag(
						n,
						_syntax,
						rootKey,
						key,
						r,
						"result"
					);
				}
			}
		}

		// this._builder.AppendLine($"deactivate {rootKey}");
	}

	private static string GetMethodName(string input)
	{
		var match = _re.Match(input);

		if (match.Success)
		{
			string methodName = match.Groups[1].Value;
			return methodName;
		}

		return ""; //todo: fix...
	}

	private static int GetArgCount(string input)
	{
		var match = _argCountRe.Match(input);

		if (match.Success)
		{
			string argumentsString = match.Groups[1].Value;
			if (argumentsString.Contains(","))
			{
				int numArguments = argumentsString.Split(',').Length;
				return numArguments;
			}
			return 0;
		}

		return -1; // todo: fix...
	}

	private static string ProcessSyntaxNode(SyntaxNode? node)
	{
		return node switch
		{
			ParameterListSyntax x => string.Join(", ", x.Parameters.Select(p => ProcessSyntaxNode(p))),
			ParameterSyntax x => ProcessSyntaxToken(x.Identifier),
			AssignmentExpressionSyntax x => $"{ProcessSyntaxNode(x.Left)} {ProcessSyntaxToken(x.OperatorToken)} {ProcessSyntaxNode(x.Right)};",
			EqualsValueClauseSyntax x => $"{ProcessSyntaxToken(x.EqualsToken)} {ProcessSyntaxNode(x.Value)}",
			VariableDeclaratorSyntax x => $"{ProcessSyntaxToken(x.Identifier)} ",
			VariableDeclarationSyntax x => $"{x.Variables.Select(v => ProcessSyntaxNode(v))}", // what about initializing many at one time?
			AwaitExpressionSyntax x => $"{ProcessSyntaxToken(x.AwaitKeyword)} {ProcessSyntaxNode(x.Expression)};",
			BinaryExpressionSyntax x => $"{ProcessSyntaxNode(x.Left)} {ProcessSyntaxToken(x.OperatorToken)} {ProcessSyntaxNode(x.Right)}",
			ObjectCreationExpressionSyntax x => $"{ProcessSyntaxToken(x.NewKeyword)} {ProcessSyntaxNode(x.Type)}({ProcessSyntaxNode(x.ArgumentList)})",
			InvocationExpressionSyntax x => $"{ProcessSyntaxNode(x.Expression)}({ProcessSyntaxNode(x.ArgumentList)})",
			MemberAccessExpressionSyntax x => $"{ProcessSyntaxNode(x.Expression)}.{ProcessSyntaxNode(x.Name)}",
			IdentifierNameSyntax x => x.Identifier.ValueText,
			ArgumentSyntax x => ProcessSyntaxNode(x.Expression),
			ThisExpressionSyntax x => "this",
			ArgumentListSyntax x => string.Join(", ", x.Arguments.Select(a => ProcessSyntaxNode(a)).ToList()),
			LiteralExpressionSyntax x => x.Token.Value is null ? "null" : $"\"{ProcessSyntaxToken(x.Token)}\"",
			ConditionalExpressionSyntax x => $"{ProcessSyntaxNode(x.Condition)} {ProcessSyntaxToken(x.QuestionToken)} {ProcessSyntaxNode(x.WhenTrue)} {ProcessSyntaxToken(x.ColonToken)} {ProcessSyntaxNode(x.WhenFalse)}",
			_ => ""
			// PredefinedTypeSyntax...
		};
	}

	public static string ProcessSyntaxToken(SyntaxToken token)
		=> token.ValueText;
}