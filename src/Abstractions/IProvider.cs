namespace Algo.Cs2Uml.Abstractions;

internal interface IProvider<T>
{
	T Provide();
}