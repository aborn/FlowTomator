using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    public enum NStatus
    {
        Running,  // 表示正在执行
        Finished, // 表示已执行完成
        Init,    
        Ready, // 表示已经加入到执行列表，准备执行
    }

    public enum NodeResult
    {
        /// <summary>
        /// Return value if the node evaluation has succeeded
        /// </summary>
        Success,

        /// <summary>
        /// Return value if the runtime should skip further thread evaluation
        /// </summary>
        Skip,

        /// <summary>
        /// Return value if the flow should stop its evaluation
        /// </summary>
        Stop,

        /// <summary>
        /// Return value if the node evaluation has failed
        /// </summary>
        Fail
    }
    public class NodeStep
    {
        public NodeResult Result { get; private set; }
        public Slot Slot { get; private set; }

        public NodeStep(NodeResult result, Slot slot)
        {
            Result = result;
            Slot = slot;
        }
        public NodeStep(NodeResult result)
        {
            Result = result;
        }
    }

    public abstract class Node
    {
        public virtual IEnumerable<Variable> Inputs { get; } = Enumerable.Empty<Variable>();
        public virtual IEnumerable<Variable> Outputs { get; } = Enumerable.Empty<Variable>();
        public virtual IEnumerable<Slot> Slots { get; } = Enumerable.Empty<Slot>();

        public virtual Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        // 执行上下文保存的数据，提供给整体执行
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();

        // 保存节点ID，以备打点使用
        public int Id { get; set; } = -1;

        // 获取前一个节点的运行结果
        public object GetPreNodeResult()
        {
            return Context.TryGetValue("result", out var result) ? result : null;
        }

        public virtual void Reset() { }
        public abstract NodeStep Evaluate();
    }
}