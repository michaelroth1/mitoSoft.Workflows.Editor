using mitoSoft.Workflows.Editor.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mitoSoft.Workflows.Editor.Helpers.CodeGenerator
{
    static class GoToCode
    {
        public static int FindInCode(BaseNodeViewModel node, string FilePath)
        {
            var lineNr = -1;
            var lines = LoadFile(FilePath);
            if (lines == null)
            {
                return lineNr;
            }               

            if (node is NodeViewModel)
            {
                var x = WFBody.GetSingleNodeLine(node.Name, lines);
                lineNr = lines.IndexOf(x);
            }
            else if(node is ParallelNodeViewModel)
            {
                var x = WFBody.GetParallelNodeLine(node.Name, lines);
                lineNr = lines.IndexOf(x);
            }
            else if (node is SequenceNodeViewModel)
            {
                var x = WFBody.GetSequenceNodeLine(node.Name, lines);
                lineNr = lines.IndexOf(x);
            }
            else if (node is SubWorkflowNodeViewModel)
            {
                var x = WFBody.GetSubWorkflowNodeLine(node.Name, lines);
                lineNr = lines.IndexOf(x);
            }
            return lineNr;
        }

        public static int FindInCode(ConnectorViewModel transition, string FilePath)
        {
            var lineNr = -1;
            var lines = LoadFile(FilePath);
            if (lines == null)
            {
                return lineNr;
            }

            var x = WFBody.GetSingleConditionLine(transition.Name, lines);
            lineNr = lines.IndexOf(x);                
            
            return lineNr;
        }

            private static List<string> LoadFile(string FilePath)
        {
            if (File.Exists(FilePath))
            {
                return File.ReadAllLines(FilePath).ToList();                
            }
            return null;
        }
    }
}
