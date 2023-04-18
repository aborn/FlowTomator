using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("文件存在？", "IO", "判断文件是否存在")]
    public class FileExists : BinaryChoice
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return file;
            }
        }

        private Variable<FileInfo> file = new Variable<FileInfo>("File", null, "The file to be created");

        public override NodeStep Evaluate()
        {
            if (file.Value == null)
                return new NodeStep(NodeResult.Fail);

            return new NodeStep(NodeResult.Success, file.Value?.Exists == true ? TrueSlot : FalseSlot);
        }
    }
}