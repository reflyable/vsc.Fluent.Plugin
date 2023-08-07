using Blast.Core.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vsc.Fluent.Plugin
{


    public class VSCodeWorkspacesSearchOperation : SearchOperationBase
    {

        public VSCodeWorkspacesSearchOperation() : base($"Open the workspace",
            $"Open", "\uE943")
        {
        }
    }
}
