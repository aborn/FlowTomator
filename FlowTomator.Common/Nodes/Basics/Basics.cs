using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FlowTomator.Common
{
    [Node("循环开始", "基本逻辑", "循环节点处理，类似For循环，需要结合循环结束节点一起")]
    public class LoopStart : Task
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

            Log.Debug("当前：LoopStart，前一个节点的运行结果为： {0}", GetPreNodeResult() ?? "null");

            if (start.Value >= end.Value)
            {
                return NodeResult.Skip;
            }

            for (int j = start.Value; j < end.Value; j++)
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

    [Node("循环结束", "基本逻辑", "循环结束节点")]
    public class LoopEnd: Task
    {
        public override NodeResult Run()
        {
            return NodeResult.Success;
        }
    }

    [Node("流程合并", "基本逻辑", "多个流程汇聚成一个，需要等待执行完成后再执行")]
    public class FlowMerge : Task
    {
        public override IEnumerable<Variable> Outputs
        {
            get
            {
                yield return Progress;
            }
        }

        private Variable<string> Progress = new Variable<string>("Progress", "0%", "前置节点执行的进度");

        // 汇聚节点的前置节点列表
        public List<Node> PreNodes { get; set; } = new List<Node>();

        private Dictionary<int, bool> State = new Dictionary<int, bool>();

        public NStatus Status { get; set; } = NStatus.Init;

        public void Init()
        {
            foreach (Node n in PreNodes)
            {
                State[n.Id] = false;
            }
            Status = NStatus.Init;
            Log.Info(" node{0} 初始化成功", Id);
        }

        // 执行这步前要先确保Init()已经被执行，否则会有并发问题
        public bool TryRun(Node node)
        {
            Log.Info("汇聚节点{0} ，传入的前置节点 {1}", Id, node.Id);

            lock (State)
            {
                State[node.Id] = true;
            }
            bool resl = CanRun();

            int total = 0;
            int finished = 0;
            foreach (Node n in PreNodes)
            {
                if (State[n.Id])
                {
                    finished++;
                }
                total++;
                Log.Info("__ {0} @ {1}  -->  {2}", n.ToString(), n.Id, State[n.Id]);
            }
            Progress.Value = (((float)finished) / total) * 100 + "%";
            Log.Info($"Progress: {Progress.Value}");
            return resl;
        }

        private bool CanRun()
        {
            foreach (Node n in PreNodes)
            {
                if (!State.ContainsKey(n.Id) || !State[n.Id])
                {
                    return false;
                }
            }
            return true;
        }

        public override NodeResult Run()
        {
            Status = NStatus.Running;
            if (PreNodes.Count == 0)
            {
                return NodeResult.Skip;
            }

            Log.Debug("当前：FlowMerge，前一个节点的运行结果为： {0}", GetPreNodeResult() ?? "null");
            Status = NStatus.Finished;
            return NodeResult.Success;
        }

    }
}
