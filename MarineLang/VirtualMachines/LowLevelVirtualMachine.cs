using System;
using MarineLang.VirtualMachines.MarineILs;

namespace MarineLang.VirtualMachines
{
    public class LowLevelVirtualMachine
    {
        readonly ILStack iLStack = new ILStack();
        ILGeneratedData iLGeneratedData;

        public int nextILIndex;
        public int callNestCount;
        public int stackBaseCount;
        public bool endFlag;
        public bool yieldFlag;

        public EventHandler<VirtualMachineStepEventArgs> onStepILCallback;

        public object Pop() => iLStack.Pop();
        public void Push(object v) => iLStack.Push(v);
        public int GetStackCurrent() => iLStack.currentIndex;
        public void SetStackCurrent(int count) => iLStack.currentIndex = count;
        public void Store(object v, int index) => iLStack.Store(v, index);
        public object Load(int index) => iLStack.Load(index);

        public void Run(ILGeneratedData iLGeneratedData)
        {
            this.iLGeneratedData = iLGeneratedData;
            while (endFlag == false && yieldFlag == false)
            {
                var il = iLGeneratedData.marineILs[nextILIndex];
                il.Run(this);
                OnStepEvent(il);
                nextILIndex++;
            }

            OnStopEvent();
        }

        public void Resume()
        {
            yieldFlag = false;
            while (endFlag == false && yieldFlag == false)
            {
                var il = iLGeneratedData.marineILs[nextILIndex];
                il.Run(this);
                OnStepEvent(il);
                nextILIndex++;
            }

            OnStopEvent();
        }

        public void Init()
        {
            iLStack.Init();
            nextILIndex = 0;
            callNestCount = 0;
            stackBaseCount = -1;
            endFlag = false;
        }

        void OnStepEvent(IMarineIL il)
        {
            onStepILCallback?.Invoke(this, new VirtualMachineStepEventArgs(nextILIndex, il));
        }

        void OnStopEvent()
        {
            var state = yieldFlag ? VirtualMachineStepState.Yield : VirtualMachineStepState.End;
            var args = new VirtualMachineStepEventArgs(nextILIndex, state);
            onStepILCallback?.Invoke(this, args);
        }

        class ILStack
        {
            readonly object[] stack = new object[10000];
            public int currentIndex;

            public void Init()
            {
                currentIndex = -1;
            }

            public object Pop()
            {
                currentIndex--;
                return stack[currentIndex + 1];
            }

            public void Push(object v)
            {
                currentIndex++;
                stack[currentIndex] = v;
            }

            public void Store(object v, int index) => stack[index] = v;
            public object Load(int index) => stack[index];
        }
    }
}