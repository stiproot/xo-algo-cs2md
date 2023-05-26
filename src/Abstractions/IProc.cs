namespace Algo.Cs2Uml.Abstractions;

internal interface IProc<TIn, TOut>
	where TIn : IProcCmd
	where TOut : IProcRes
{
	Task Init(TIn cmd);
	TOut Process();
}