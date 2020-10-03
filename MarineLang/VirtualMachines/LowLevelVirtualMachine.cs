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

        public object Pop() => iLStack.Pop();
        public void Push(object v) => iLStack.Push(v);
        public int GetStackCurrent() => iLStack.currentIndex;
        public void SetStackCurrent(int count) => iLStack.currentIndex = count;
        public void Store(object v, int index) => iLStack.Store(v, index);
        public object Load(int index) => iLStack.Load(index);

        public int MarineFuncIndex(string funcName) => iLGeneratedData.funcILIndexDict[funcName];
        public void Run(ILGeneratedData iLGeneratedData)
        {
            this.iLGeneratedData = iLGeneratedData;
            while (endFlag == false && yieldFlag == false)
            {
                iLGeneratedData.marineILs[nextILIndex].Run(this);
                nextILIndex++;
            }
        }

        public void Resume()
        {
            yieldFlag = false;
            while (endFlag == false && yieldFlag == false)
            {
                iLGeneratedData.marineILs[nextILIndex].Run(this);
                nextILIndex++;
            }
        }

        public void Init()
        {
            iLStack.Init();
            nextILIndex = 0;
            callNestCount = 0;
            stackBaseCount = -1;
            endFlag = false;
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
