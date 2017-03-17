# CNeptune

CNeptune is an util to rewrite .net assembly to inject all the needs to control execution flow in order to help architect to build a productive and efficient architecture.

## Features
- Dynamic : Change method behavior at runtime
- Limitless : Support all type of methods (constructors included)
- Efficient : Injected mechanism is extremely efficient
- Transparent : Client side perception is not altered

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

## Example of usage
under specification... coming soon!
