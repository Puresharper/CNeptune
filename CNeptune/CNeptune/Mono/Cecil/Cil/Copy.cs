using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mono.Cecil.Cil
{
    public class Copy
    {
        private MethodDefinition m_Method;
        private Dictionary<GenericParameter, GenericParameter> m_Genericity = new Dictionary<GenericParameter, GenericParameter>();
        private Dictionary<ParameterDefinition, ParameterDefinition> m_Signature;
        private Dictionary<VariableDefinition, VariableDefinition> m_Variation;
        private Dictionary<Instruction, Instruction> m_Dictionary;
        private bool m_HasTypeGenericty;
        private bool m_HasMethodGenericty;

        public Copy(MethodDefinition method)
        {
            this.m_Method = method;
            this.m_Dictionary = new Dictionary<Instruction, Instruction>();
        }

        public GenericParameter[] TypeGenericity
        {
            set
            {
                if (m_HasTypeGenericty) { throw new NotSupportedException(); }
                if (m_HasMethodGenericty) { throw new NotSupportedException(); }
                m_HasTypeGenericty = true;
                for (var _index = 0; _index < this.m_Method.DeclaringType.GenericParameters.Count; _index++) { this.m_Genericity.Add(this.m_Method.DeclaringType.GenericParameters[_index], value[_index]); }
            }
        }

        public GenericParameter[] MethodGenericity
        {
            set
            {
                if (m_HasMethodGenericty) { throw new NotSupportedException(); }
                m_HasMethodGenericty = true;
                for (var _index = 0; _index < this.m_Method.GenericParameters.Count; _index++) { this.m_Genericity.Add(this.m_Method.GenericParameters[_index], value[_index]); }
            }
        }

        public ParameterDefinition[] Signature
        {
            set
            {
                if (this.m_Signature != null) { throw new NotSupportedException(); }
                this.m_Signature = new Dictionary<ParameterDefinition, ParameterDefinition>();
                var _offset = m_Method.IsStatic ? 0 : 1;
                for (var _index = 0; _index < this.m_Method.Parameters.Count; _index++) { this.m_Signature.Add(this.m_Method.Parameters[_index], value[_offset + _index]); }
            }
        }

        public VariableDefinition[] Variation
        {
            set
            {
                if (this.m_Variation != null) { throw new NotSupportedException(); }
                this.m_Variation = new Dictionary<VariableDefinition, VariableDefinition>();
                for (var _index = 0; _index < this.m_Method.Body.Variables.Count; _index++) { this.m_Variation.Add(this.m_Method.Body.Variables[_index], value[_index]); }
            }
        }

        public TypeReference this[TypeReference type]
        {
            get
            {
                var _newType = ReplaceGenericTypes(type);
                if (_newType != null)
                {
                    return _newType;
                }
                return type;
            }
        }

        private TypeReference ReplaceGenericTypes(TypeReference type)
        {
            if (type is GenericParameter) { return this.m_Genericity.TryGetValue(type as GenericParameter) ?? type; }
            if (type is GenericInstanceType _genericType)
            {
                List<TypeReference> _newArgumentTypes = null;
                for (var _index = 0; _index < _genericType.GenericArguments.Count; _index++)
                {
                    var _newArgumentType = ReplaceGenericTypes(_genericType.GenericArguments[_index]);
                    if (_newArgumentType != null)
                    {
                        if (_newArgumentTypes == null) _newArgumentTypes = new List<TypeReference>();
                        for (int i = _newArgumentTypes.Count; i < _index; i++)
                        {
                            _newArgumentTypes.Add(_genericType.GenericArguments[i]);
                        }
                        _newArgumentTypes.Add(_newArgumentType);
                    }
                }

                if (_newArgumentTypes != null)
                {
                    for (int i = _newArgumentTypes.Count; i < _genericType.GenericArguments.Count; i++)
                    {
                        _newArgumentTypes.Add(_genericType.GenericArguments[i]);
                    }

                    return _genericType.ElementType.MakeGenericType(_newArgumentTypes);
                }
            }

            return null;
        }

        public FieldReference this[FieldReference field]
        {
            get { return field; }
        }

        public MethodReference this[MethodReference method]
        {
            get { return method; }
        }

        public Instruction this[Instruction instruction]
        {
            get
            {
                if (instruction == null) { return null; }
                var _instruction = this.m_Dictionary.TryGetValue(instruction);
                if (_instruction == null)
                {
                    var _operand = instruction.Operand;
                    if (_operand == null) { _instruction = Instruction.Create(instruction.OpCode); }
                    else if (_operand is ParameterDefinition) { _instruction = Instruction.Create(instruction.OpCode, this.m_Signature[_operand as ParameterDefinition]); }
                    else if (_operand is VariableDefinition) { _instruction = Instruction.Create(instruction.OpCode, this.m_Variation[_operand as VariableDefinition]); }
                    else if (_operand is FieldReference) { _instruction = Instruction.Create(instruction.OpCode, this[_operand as FieldReference]); }
                    else if (_operand is MethodReference) { _instruction = Instruction.Create(instruction.OpCode, this[_operand as MethodReference]); }
                    else if (_operand is TypeReference) { _instruction = Instruction.Create(instruction.OpCode, this[_operand as TypeReference]); }
                    else if (_operand is string) { _instruction = Instruction.Create(instruction.OpCode, _operand as string); }
                    else if (_operand is sbyte) { _instruction = Instruction.Create(instruction.OpCode, (sbyte)_operand); }
                    else if (_operand is long) { _instruction = Instruction.Create(instruction.OpCode, (long)_operand); }
                    else if (_operand is int) { _instruction = Instruction.Create(instruction.OpCode, (int)_operand); }
                    else if (_operand is float) { _instruction = Instruction.Create(instruction.OpCode, (float)_operand); }
                    else if (_operand is double) { _instruction = Instruction.Create(instruction.OpCode, (double)_operand); }
                    else if (_operand is Instruction) { _instruction = Instruction.Create(instruction.OpCode, this[_operand as Instruction]); }
                    else if (_operand is Instruction[]) { _instruction = Instruction.Create(instruction.OpCode, (_operand as Instruction[]).Select(_Instruction => this[_Instruction]).ToArray()); }
                    else { throw new NotSupportedException(); }
                    var _sequence = instruction.SequencePoint;
                    if (_sequence != null)
                    {
                        _instruction.SequencePoint = new SequencePoint(_sequence.Document)
                        {
                            StartLine = _sequence.StartLine,
                            StartColumn = _sequence.StartColumn,
                            EndLine = _sequence.EndLine,
                            EndColumn = _sequence.EndColumn
                        };
                    }
                    this.m_Dictionary.Add(instruction, _instruction);
                }
                return _instruction;
            }
        }

        public GenericParameter this[GenericParameter key]
        {
            get { return this.m_Genericity[key]; }
        }

        public ParameterDefinition this[ParameterDefinition key]
        {
            get { return this.m_Signature[key]; }
        }

        public VariableDefinition this[VariableDefinition key]
        {
            get { return this.m_Variation[key]; }
        }
    }
}
