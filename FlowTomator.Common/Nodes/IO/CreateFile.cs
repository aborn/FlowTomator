using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowTomator.Common
{
    [Node("创建文件", "IO", "Create a file at the specified location")]
    public class CreateFile : Task
    {
        public override IEnumerable<Variable> Inputs
        {
            get
            {
                yield return file;
            }
        }

        private Variable<FileInfo> file = new Variable<FileInfo>("File", null, "The file to be created");

        public override NodeResult Run()
        {
            if (file.Value == null || file.Value.Exists)
                return NodeResult.Skip;

            try
            {
                File.Create(file.Value.FullName).Close();
            }
            catch
            {
                return NodeResult.Fail;
            }

            return NodeResult.Success;
        }
    }
}