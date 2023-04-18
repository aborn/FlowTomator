using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlowTomator.Common;

namespace FlowTomator.Desktop
{
    using System.Threading;
    using Task = System.Threading.Tasks.Task;

    public enum DebuggerState
    {
        Idle,
        Break,
        Running,
    }

    public class FlowDebugger : DependencyModel
    {
        public static Log.Category DebuggerCategory { get; } = new Log.Category("Debugger");

        public FlowInfo FlowInfo { get; private set; }
        public DebuggerState State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
                NotifyPropertyChanged();
            }
        }

        private DebuggerState state = DebuggerState.Idle;
        private List<NodeInfo> nodes = new List<NodeInfo>();
        private List<Task> tasks = new List<Task>();

        public FlowDebugger(FlowInfo flowInfo)
        {
            FlowInfo = flowInfo;
        }

        public void Run()
        {
            if (State == DebuggerState.Idle)
                Reset();

            if (State == DebuggerState.Break)
            {
                State = DebuggerState.Running;
                Task.Run(Evaluate);
            }
        }
        public void Step()
        {
            if (State == DebuggerState.Idle)
                Reset();
            else if (State == DebuggerState.Break)
                Task.Run(Evaluate);
        }
        public void Break()
        {
            if (State == DebuggerState.Running)
                State = DebuggerState.Break;
        }
        public void Stop()
        {
            if (State == DebuggerState.Idle)
                return;

            State = DebuggerState.Idle;

            nodes.Clear();
            tasks.Clear();

            foreach (NodeInfo nodeInfo in FlowInfo.Flow.GetAllNodes().Select(n => NodeInfo.From(FlowInfo, n)))
                nodeInfo.Status = NodeStatus.Idle;
        }

        private void Reset()
        {
            FlowInfo.Flow.Reset();
            FlowInfo.Init();

            nodes.Clear();
            nodes = FlowInfo.Flow.Origins
                                 .Select(n => NodeInfo.From(FlowInfo, n))
                                 .ToList();

            foreach (NodeInfo nodeInfo in nodes)
                nodeInfo.Status = NodeStatus.Paused;

            State = DebuggerState.Break;
        }
        private async Task Evaluate()
        {
            NodeInfo[] stepNodes;

            lock (nodes)
            {
                if (nodes.Count == 0)
                    return;

                stepNodes = nodes.ToArray();
                nodes.Clear();
            }

            foreach (NodeInfo nodeInfo in stepNodes)
            {
                //TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();
                //CancellationToken token = new CancellationToken(false);
                //Task task = await Task.Factory.StartNew(() => Evaluate(nodeInfo), token, TaskCreationOptions.LongRunning, scheduler);

                Task task = Task.Run(() => Evaluate(nodeInfo));

                lock (tasks)
                    tasks.Add(task);

                task.ContinueWith(t =>
                {
                    lock (tasks)
                    {
                        tasks.Remove(t);

                        if (tasks.Count == 0 && nodes.Count == 0)
                            State = DebuggerState.Idle;
                    }

                    if (State == DebuggerState.Running)
                        Task.Run(Evaluate);
                });
            }
        }

        private void LogThreadInfo()
        {
            String taskInfo = Task.CurrentId == null ? "no task" : "task_" + Task.CurrentId;
            Log.Info(" thread {0} {1}", Thread.CurrentThread.ManagedThreadId, taskInfo);
        }

        private async Task Evaluate(NodeInfo nodeInfo)
        {
            NodeStep nodeStep;
            nodeInfo.Status = NodeStatus.Running;
            Dictionary<string, object> Context = new Dictionary<string, object>();
            try
            {
                Log.Trace(DebuggerCategory, "Entering node {0}", nodeInfo.Type.Name);
                nodeStep = nodeInfo.Node.Evaluate();
                Log.Trace(DebuggerCategory, "Exiting node {0} with result {1}", nodeInfo.Type.Name, nodeStep.Result);
            }
            catch (Exception e)
            {
                Log.Error(DebuggerCategory, "Error while executing node {0}. {1}", nodeInfo.Type.Name, e.Message);
                nodeStep = new NodeStep(NodeResult.Fail, null);
            }

            if (State == DebuggerState.Idle)
                return;

            nodeInfo.Status = NodeStatus.Idle;
            nodeInfo.Result = nodeStep.Result;

            switch (nodeStep.Result)
            {
                case NodeResult.Skip: return;
                case NodeResult.Fail: Break(); return;
                case NodeResult.Stop: Stop(); return;
            }

            NodeInfo[] nodeInfos = nodeStep.Slot.Nodes.Select(n => NodeInfo.From(FlowInfo, n)).ToArray();
            List<NodeInfo> nextCanRunNodes = new List<NodeInfo>();

            foreach (NodeInfo node in nodeInfos)
            {
                if (node.Type.FullName == "FlowTomator.Common.FlowMerge")
                {
                    FlowMerge flowMerge = node.Node as FlowMerge;
                    if (!flowMerge.TryRun(nodeInfo.Node))
                    {
                        LogThreadInfo();
                        Log.Info("当前{0}为汇聚节点！且前置节点未执行完成，不能开始执行当前节点", node.Node.Id);
                    }
                    else
                    {
                        LogThreadInfo();
                        Log.Info("当前{0}为汇聚节点！前置节点已经执行完成，可以开始执行", node.Node.Id);
                        lock (flowMerge)
                        {
                            // 对汇聚节点加锁，目的：是为了防止汇聚节点的因并发导致FlowMerge被多次加入到可执行的task列表里
                            if (flowMerge.Status == NStatus.Init)
                            {
                                flowMerge.Status = NStatus.Ready;
                                nextCanRunNodes.Add(node);
                                Log.Info("当前{0}为汇聚节点！前置节点已经执行完成，可以开始执行@@@@", node.Node.Id);
                                Log.Info("nodeType {0}", node.Type);
                                node.Status = NodeStatus.Paused;
                                node.Node.Context = nodeInfo.Node.Context;  // 将当前运行节点的context传递到下步执行的节点里
                            }
                        }
                    }
                }
                else
                {
                    nextCanRunNodes.Add(node);
                    Log.Info("nodeType {0}", node.Type);
                    node.Status = NodeStatus.Paused;
                    node.Node.Context = nodeInfo.Node.Context;  // 将当前运行节点的context传递到下步执行的节点里
                }

            }


            lock (nodes)
                nodes.AddRange(nextCanRunNodes);
        }
    }
}