using System;
using System.Collections.Generic;
using System.Text;

namespace mitoSoft.Workflows.Editor.Helpers.Enums
{
    public enum NodeCanvasClickMode
    {
        noCorrect = 0,
        Default,
        AddNode,
        Delete,
        Select,
        AddParallelNode,
        AddSubWorkflowNode,
        Cut
    }
}
