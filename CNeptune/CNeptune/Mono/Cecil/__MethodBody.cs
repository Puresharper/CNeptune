using System;
using Mono;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
    static internal class __MethodBody
    {
        static public void Emit(this MethodBody body, OpCode instruction)
        {
            body.Instructions.Add(Instruction.Create(instruction));
        }

        static public void Emit(this MethodBody body, OpCode instruction, VariableDefinition variable)
        {
            body.Instructions.Add(Instruction.Create(instruction, variable));
        }

        static public void Emit(this MethodBody body, OpCode instruction, System.Reflection.MethodInfo method)
        {
            body.Instructions.Add(Instruction.Create(instruction, body.Method.DeclaringType.Module.Import(method)));
        }

        static public void Emit(this MethodBody body, OpCode instruction, TypeReference type, Collection<ParameterDefinition> parameters)
        {
            if (instruction == OpCodes.Calli)
            {
                var _signature = new CallSite(type);
                foreach (var _parameter in parameters) { _signature.Parameters.Add(_parameter); }
                _signature.CallingConvention = MethodCallingConvention.Default;
                body.Instructions.Add(Instruction.Create(instruction, _signature));
                return;
            }
            throw new InvalidOperationException();
        }

        static public void Emit(this MethodBody body, OpCode instruction, TypeReference type)
        {
            body.Instructions.Add(Instruction.Create(instruction, type));
        }

        static public void Emit(this MethodBody body, OpCode instruction, MethodReference method)
        {
            body.Instructions.Add(Instruction.Create(instruction, method));
        }

        static public void Emit(this MethodBody body, OpCode instruction, FieldReference field)
        {
            body.Instructions.Add(Instruction.Create(instruction, field));
        }

        static public void Emit(this MethodBody body, OpCode instruction, ParameterDefinition parameter)
        {
            body.Instructions.Add(Instruction.Create(instruction, parameter));
        }

        static public void Emit(this MethodBody body, OpCode instruction, int operand)
        {
            body.Instructions.Add(Instruction.Create(instruction, operand));
        }

        static public void Emit(this MethodBody body, OpCode instruction, string operand)
        {
            body.Instructions.Add(Instruction.Create(instruction, operand));
        }

        static public void Emit(this MethodBody body, OpCode instruction, System.Reflection.ConstructorInfo constructor)
        {
            body.Instructions.Add(Instruction.Create(instruction, body.Method.DeclaringType.Module.Import(constructor)));
        }

        static public VariableDefinition Variable<T>(this MethodBody body, string name)
        {
            var _variable = new VariableDefinition(name, body.Method.DeclaringType.Module.Import(typeof(T)));
            body.Variables.Add(_variable);
            return _variable;
        }
    }
}
