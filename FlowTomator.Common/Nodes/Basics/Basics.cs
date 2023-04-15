using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
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

        public override IEnumerable<Variable> Outputs
        {
            get
            {
                yield return content;
            }
        }

        private Variable<int> i = new Variable<int>("i", 0, "The path of the application to launch");
        private Variable<int> start = new Variable<int>("起始值", 0, "初始值");
        private Variable<int> end = new Variable<int>("结束值", 0, "初始值");
        private Variable content = new Variable("Content", typeof(object), null, "The content of the clipboard");

        public override NodeResult Run()
        {
            if (start.Value >= end.Value)
            {
                return NodeResult.Skip;
            }

            for (int j=start.Value; j<end.Value;j++)
            {
                Log.Debug("循环进行中，当前值 {0} {1}", j, i.Value);
                i.Value = j * 2;
            }
            content.Value = i.Value;
            Log.Debug("结果为：{0}", content.Value);
            return NodeResult.Success;
        }
    }
}
