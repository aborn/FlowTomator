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
        private Variable<int> start = new Variable<int>("Start", 0, "循环开始值（包含）");
        private Variable<int> end = new Variable<int>("End", 0, "循环结束值（不包含）");
        private Variable content = new Variable("Content", typeof(object), null, "The content of the clipboard");

        public override NodeResult Run()
        {
            Log.Debug("前一个节点的运行结果为： {0}", Context["result"]);
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

            // 保存当前节点的结果，到上下文
            Context["result"] = content.Value;            
            return NodeResult.Success;
        }
    }

    [Node("流程合并", "基本逻辑", "两个及以上流程合并成一个，需要等待执行完成后再执行")]
    public class FlowMerge: Task
    {
        public override NodeResult Run()
        {
            Log.Debug("前一个节点的运行结果为： {0}", Context["result"]);
            return NodeResult.Success;
        }
        
    }
}
