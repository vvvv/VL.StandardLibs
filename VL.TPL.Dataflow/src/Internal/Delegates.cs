namespace VL.TPL.Dataflow.Internal;

public delegate void CreateHandler(int workerId, out object stateOutput);
public delegate void UpdateHandler<TInput>(object stateInput, TInput input, out object stateOutput);
public delegate void UpdateHandler<TInput, TOutput>(object stateInput, TInput input, out object stateOutput, out TOutput output);
public delegate void AsyncUpdateHandler<TInput>(object stateInput, TInput input, out object stateOutput, out Task asyncAction);
public delegate void AsyncUpdateHandler<TInput, TOutput>(object stateInput, TInput input, out object stateOutput, out Task<TOutput> asyncResult);