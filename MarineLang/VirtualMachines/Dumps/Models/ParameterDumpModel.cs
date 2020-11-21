﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MarineLang.VirtualMachines.Dumps.Models
{
    public class ParameterDumpModel
    {
        public string Name { get; }

        public bool IsIn { get; }
        public bool IsOut { get; }
        public bool IsRef { get; }

        public bool IsOptional { get; }

        public object Value { get; }

        public TypeDumpModel Type { get; }

        public ParameterDumpModel(string name, TypeDumpModel type, bool isIn, bool isOut, bool isRef)
        {
            Name = name;

            IsIn = isIn;
            IsOut = isOut;
            IsRef = isRef;

            Type = type;
        }

        public ParameterDumpModel(string name, TypeDumpModel type, bool isIn, bool isOut, bool isRef, object defaultValue) : this(name, type, isIn, isOut, isRef)
        {
            IsOptional = true;

            Value = defaultValue;
        }
    }
}
