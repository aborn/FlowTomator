using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("循环开始", "基本逻辑", "Run the specified application")]
    public class LoopStart: Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return i;
                yield return start;
                yield return end;
            }
        }

        private Variable<int> i = new Variable<int>("i", 0, "The path of the application to launch");
        private Variable<int> start = new Variable<int>("起始值", 0, "初始值");
        private Variable<int> end = new Variable<int>("结束值", 0, "初始值");
        
        public override NodeResult Run()
        {
            if (start.Value >= end.Value)
            {
                return NodeResult.Skip;
            }

            for (int i=start.Value; i<end.Value;i++)
            {
                Log.Debug("循环进行中，当前值 {0}", i);
            }
            return NodeResult.Success;
        }
    }
}
