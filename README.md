[![NuGet](https://img.shields.io/nuget/v/cneptune.svg)](https://www.nuget.org/packages/CNeptune)
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
- Rewrite is automatically done after build and before link by adding CNeptune nuget package : https://www.nuget.org/packages/CNeptune
```
PM> Install-Package CNeptune
```

## Example of usage
- Override method at runtime

Business
```
public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}
```

Obtain the delegate to manage a method of 'Calculator'
```
var _update = typeof(Calculator).GetNestedType("<Neptune>", BindingFlags.NonPublic).GetField("<Update>").GetValue(null) as Action<MethodBase, Func<MethodInfo, MethodInfo>>;
```

Define 'Add' method to inject a console 'Hello World' before call.
```
_update
(
    typeof(Calculator).GetMethod("Add"),
    _Method =>
    {
        var _method = new DynamicMethod(string.Empty, typeof(int), new Type[] { typeof(Calculator), typeof(int), typeof(int) }, typeof(Calculator), true);
        var _body = _method.GetILGenerator();
        _body.EmitWriteLine("Hello World");
        _body.Emit(OpCodes.Ldarg_0); //this
        _body.Emit(OpCodes.Ldarg_1); //a
        _body.Emit(OpCodes.Ldarg_2); //b
        _body.Emit(OpCodes.Call, _Method);
        _body.Emit(OpCodes.Ret);
        return _method;
    }
);
```

