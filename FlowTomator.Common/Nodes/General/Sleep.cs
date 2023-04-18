using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("延时", "系统", "等待多长时间后执行（单位ms）")]
    public class Sleep : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                return new Variable[] { duration };
            }
        }

        private Variable<int> duration = new Variable<int>("Duration", 1000, "The duration to sleep in milliseconds");

        public override NodeResult Run()
        {
            Thread.Sleep(duration.Value);

            return NodeResult.Success;
        }
    }
}