# CNeptune

CNeptune is an util based on mono.cecil to rewrite .net assembly to inject all the needs to control execution flow in order to help architects to build a productive and efficient architecture.

## Features
- Dynamic : change method behavior at runtime
- Efficient : injected mechanism is extremely efficient
- Limitless : support all type of methods (constructors included)
- Transparent : client side perception is not affected

## Coverage
- Aspect-Oriented Programming
- Code contract / Data validation
- Uncoupling technical concern
- Diagnostics & measures
- Mocking & tests
- Simplify existing design pattern

## Example of injection
- Rewrite .net assembly by specifying path
```
neptune.exe "C:\...\Assembly.dll"
```
- Rewrite .net assembly by specifying project and configuration
```
neptune.exe "C:\...\Project.csproj" "Debug"
```
- Rewrite is automatically done after compilation by adding CNeptune nuget package : https://www.nuget.org/packages/CNeptune
```
PM> Install-Package CNeptune
```

## Example of usage
under specification... coming soon!
