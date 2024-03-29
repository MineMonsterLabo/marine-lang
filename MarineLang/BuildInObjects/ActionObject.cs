﻿using MarineLang.VirtualMachines;
using System;
using System.Linq;

namespace MarineLang.BuildInObjects
{
    public class ActionObjectGenerator
    {
        readonly HighLevelVirtualMachine vm;

        public ActionObjectGenerator(HighLevelVirtualMachine vm)
        {
            this.vm = vm;
        }

        public ActionObject Generate(string marineFuncName, object[] captures)
        {
            return new ActionObject(marineFuncName, captures, vm);
        }
    }

    public class ActionObject
    {
        readonly object[] captures;
        readonly string marineFuncName;
        readonly HighLevelVirtualMachine vm;

        public ActionObject(string marineFuncName, object[] captures, HighLevelVirtualMachine vm)
        {
            this.marineFuncName = marineFuncName;
            this.captures = captures;
            this.vm = vm;
        }

        public object Get(int i) => captures[i];
        public void Set(int i, object obj) => captures[i] = obj;

        public MarineValue<T> InvokeGeneric<T>(params object[] args)
        {
            return vm.Run<T>(marineFuncName, new object[] { this }.Concat(args));
        }

        public MarineValue Invoke(params object[] args)
        {
            return vm.Run(marineFuncName, new object[] { this }.Concat(args));
        }

        public static Func<MarineValue> ToDelegate(ActionObject actionObject)
        {
            return () => actionObject.Invoke();
        }

        public static Func<object[], MarineValue> ToDelegateArg(ActionObject actionObject)
        {
            return args => actionObject.Invoke(args);
        }
    }
}
