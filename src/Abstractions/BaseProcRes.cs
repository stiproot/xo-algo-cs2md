namespace Algo.Cs2Uml.Abstractions;

internal abstract record BaseProcRes<TData> : IProcRes
{
	public TData? Data { get; set; }
}