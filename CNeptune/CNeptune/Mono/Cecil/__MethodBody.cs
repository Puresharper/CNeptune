using System;
using Mono;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections;
using Mono.Collections.Generic;

using ConstructorInfo = System.Reflection.ConstructorInfo;
using MethodInfo = System.Reflection.MethodInfo;

namespace Mono.Cecil
{
    static internal class __MethodBody
    {
        static private readonly MethodInfo GetTypeFromHandle = System.Metadata.Method(() => Type.GetTypeFromHandle(Argument<RuntimeTypeHandle>.Value));
        static private readonly MethodInfo GetMethodFromHandle = System.Metadata.Method(() => MethodInfo.GetMethodFromHandle(Argument<RuntimeMethodHandle>.Value, Argument<RuntimeTypeHandle>.Value));

        static public void Add(this MethodBody body, Instruction instruction)
        {
            body.Instructions.Add(instruction);
            var _branch = Branch.Query(body);
            if (_branch == null) { return; }
            _branch.Finialize(instruction);
        }

        static public IDisposable True(this MethodBody body)
        {
            return new Branch(body, OpCodes.Brfalse).Begin();
        }

        static public IDisposable False(this MethodBody body)
        {
            return new Branch(body, OpCodes.Brtrue).Begin();
        }

        static public void Emit(this MethodBody body, OpCode instruction)
        {
            body.Add(Instruction.Create(instruction));
        }

        static public void Emit(this MethodBody body, OpCode instruction, VariableDefinition variable)
        {
            body.Add(Instruction.Create(instruction, variable));
        }

        static public void Emit(this MethodBody body, OpCode instruction, MethodInfo method)
        {
            body.Add(Instruction.Create(instruction, body.Method.DeclaringType.Module.Import(method)));
        }

        static public void Emit(this MethodBody body, OpCode instruction, TypeReference type, Collection<ParameterDefinition> parameters)
        {
            if (instruction == OpCodes.Calli)
            {
                var _signature = new CallSite(type);
                foreach (var _parameter in parameters) { _signature.Parameters.Add(_parameter); }
                _signature.CallingConvention = MethodCallingConvention.Default;
                body.Add(Instruction.Create(instruction, _signature));
                return;
            }
            throw new InvalidOperationException();
        }

        static public void Emit(this MethodBody body, OpCode instruction, TypeReference type)
        {
            body.Add(Instruction.Create(instruction, type));
        }

        static public void Emit(this MethodBody body, OpCode instruction, Type type)
        {
            body.Add(Instruction.Create(instruction, body.Method.Module.Import(type)));
        }

        static public void Emit(this MethodBody body, OpCode instruction, MethodReference method)
        {
            body.Add(Instruction.Create(instruction, method));
        }

        static public void Emit(this MethodBody body, OpCode instruction, FieldReference field)
        {
            body.Add(Instruction.Create(instruction, field));
        }

        static public void Emit(this MethodBody body, OpCode instruction, ParameterDefinition parameter)
        {
            body.Add(Instruction.Create(instruction, parameter));
        }

        static public void Emit(this MethodBody body, OpCode instruction, int operand)
        {
            body.Add(Instruction.Create(instruction, operand));
        }

        static public void Emit(this MethodBody body, OpCode instruction, string operand)
        {
            body.Add(Instruction.Create(instruction, operand));
        }

        static public void Emit(this MethodBody body, OpCode instruction, ConstructorInfo constructor)
        {
            body.Add(Instruction.Create(instruction, body.Method.DeclaringType.Module.Import(constructor)));
        }

        static public void Emit(this MethodBody body, TypeReference type)
        {
            body.Emit(OpCodes.Ldtoken, type);
            body.Emit(OpCodes.Call, __MethodBody.GetTypeFromHandle);
        }

        static public void Emit(this MethodBody body, MethodReference method)
        {
            body.Emit(OpCodes.Ldtoken, method);
            body.Emit(OpCodes.Ldtoken, method.DeclaringType);
            body.Emit(OpCodes.Call, __MethodBody.GetMethodFromHandle);
        }

        static public VariableDefinition Variable<T>(this MethodBody body)
        {
            var _variable = new VariableDefinition(string.Concat("<", Metadata<T>.Type, ">"), body.Method.DeclaringType.Module.Import(Metadata<T>.Type));
            body.Variables.Add(_variable);
            return _variable;
        }

        static public VariableDefinition Variable<T>(this MethodBody body, string name)
        {
            var _variable = new VariableDefinition(name, body.Method.DeclaringType.Module.Import(Metadata<T>.Type));
            body.Variables.Add(_variable);
            return _variable;
        }

        static public VariableDefinition Add(this MethodBody body, VariableDefinition variable)
        {
            body.Variables.Add(variable);
            return variable;
        }
    }
}
