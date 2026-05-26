# Application Common

Application use cases are expressed as `ICommand<TResult>` or `IQuery<TResult>` records and handled by one `IHandler<TRequest, TResult>` class.

`IHandlerDispatcher` resolves the handler through DI and wraps it in registered pipeline behaviors. The standard order is validation first, then logging, then the handler.

Handlers must not log request payloads. Log operational fields only, such as request type, user id, HCP id, latency, and success.
