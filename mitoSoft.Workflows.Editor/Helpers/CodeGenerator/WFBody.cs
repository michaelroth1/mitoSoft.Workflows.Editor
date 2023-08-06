using mitoSoft.Workflows.Editor.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
using DynamicData.Aggregation;
using static System.Windows.Forms.LinkLabel;
using mitoSoft.Workflows.Editor.Helpers.SchemeUpdater;

namespace mitoSoft.Workflows.Editor.Helpers.CodeGenerator
{
    public class WFBody
    {
        StringBuilder sbBody = new ();

        const string ADD_SINGLE_NODE = "AddSingleNode";
        const string ADD_SEQ_NODE = "AddSequenceNode";
        const string SEQ_STATE = "SequenceState";
        const string ADD_SEQ_SUBSTATE = "AddToSequence";
        const string ADD_PARA_NODE = "AddParallelNode";
        const string ADD_PARA_SUBSTATE = "AddToParallel";
        const string PARA_STATE = "ParallelState";
        const string ADD_SUBWF_NODE = "AddSubWorkflow";
        const string ADD_CONDS = "AddConditions";
        const string ADD_COND = "AddCondition";
        const string TRIGGER_ACTION = "Action";
        const string BUILD = "Build";

        const string T1 = "\t";
        const string T2 = "\t\t";
        const string T3 = "\t\t\t";
        const string T4 = "\t\t\t\t";
        const string DEFAULT_FUNC = "() => { }";
        const string DEFAULT_COND = "() => true";

        public List<string> ExistingWorkflowLines { get; set; }
        
        public WFBody()
        {
            sbBody.AppendLine($"{T3}this");            
        }

        public void AddSingleNode(string NodeName, string NextNodeName)
        {
            string line = GetSingleNodeLine(NodeName, ExistingWorkflowLines);

            string func;
            if (line == null)
            {
                line = GetSequenceSubstateLine(NodeName, ExistingWorkflowLines);
                func = GetFuncCodeFromSequenceSubState(line);
            }
            else
            {
                func = GetFuncCodeFromSingleNode(line);
            }
            InsertCodeForSingleNode(NodeName, NextNodeName, func);
        }

        public void AddSequenceNode(string NodeName, string NextNodeName, List<BaseNodeViewModel> SeqStates)
        {
            UpdateCodeForSequenceNode(NodeName, NextNodeName, SeqStates);         
        }

        public void AddSubWorkflowNode(string NodeName, string NextNodeName, string SubStateMachine)
        {
            var SubWf = PathHelper.GetFileNameWithoutExtension(SubStateMachine);
            if (string.IsNullOrEmpty(NextNodeName))
            {
                sbBody.AppendLine($"{T3}.{ADD_SUBWF_NODE}(\"{NodeName}\", new {SubWf}() )");
            }
            else
            {
                sbBody.AppendLine($"{T3}.{ADD_SUBWF_NODE}(\"{NodeName}\", new {SubWf}(), \"{NextNodeName}\" )");
            }
        }

        public void AddParallelNode(string NodeName, string NextNodeName, List<string> ParaStates)
        {
            StringBuilder sbPara = new StringBuilder();

            if (ParaStates.Any())
            {
                sbPara.AppendLine($"{T3}.{ADD_PARA_NODE}(new {PARA_STATE}(\"{NodeName}\")");

                for (int i = 0; i < ParaStates.Count - 1; i++)
                {
                    var ParaState = PathHelper.GetFileNameWithoutExtension(ParaStates[i]);

                    sbPara.AppendLine($"{T4}.{ADD_PARA_SUBSTATE}(new {ParaState}())");
                }
                var LastParaState = PathHelper.GetFileNameWithoutExtension(ParaStates.Last());
                
                if (string.IsNullOrEmpty(NextNodeName))
                {
                    sbPara.AppendLine($"{T4}.{ADD_PARA_SUBSTATE}(new {LastParaState}()))");
                }
                else
                {
                    sbPara.AppendLine($"{T4}.{ADD_PARA_SUBSTATE}(new {LastParaState}()),\"{NextNodeName}\")");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(NextNodeName))
                {
                    sbPara.AppendLine($"{T3}.{ADD_PARA_NODE}(new {PARA_STATE}(\"{NodeName}\"))");
                }
                else
                {
                    sbPara.AppendLine($"{T3}.{ADD_PARA_NODE}(new {PARA_STATE}(\"{NodeName}\"), \"{NextNodeName}\")");
                }
            }
            sbBody.Append(sbPara.ToString());
        }

        public void AddOrUpdateCondis(string NodeName, List<ConnectorViewModel> transitions)
        {
            string ExistingLine = GetConditionsLine(NodeName, ExistingWorkflowLines);

            if (ExistingLine == null)
            {
                InsertCodeForConditions(NodeName, transitions);
            }
            else
            {
                UpdateCodeForConditions(NodeName, transitions);
            }
        }


        private void InsertCodeForSingleNode(string NodeName, string NextNodeName, string NodeAction)
        {
            if (string.IsNullOrEmpty(NextNodeName))
            {
                sbBody.AppendLine($"{T3}.{ADD_SINGLE_NODE}(\"{NodeName}\", {NodeAction})");
            }
            else
            {
                sbBody.AppendLine($"{T3}.{ADD_SINGLE_NODE}(\"{NodeName}\", \"{NextNodeName}\", {NodeAction})");
            }
        }

        private void UpdateCodeForSequenceNode(string NodeName, string NextNodeName, List<BaseNodeViewModel> SeqStates)
        {
            StringBuilder sbSeq = new StringBuilder();

            if (SeqStates.Any())
            {
                sbSeq.AppendLine($"{T3}.{ADD_SEQ_NODE}(new {SEQ_STATE}(\"{NodeName}\")");

                foreach (var item in SeqStates)
                {
                    var last = item.Name == SeqStates.Last().Name;
                    var func = DEFAULT_FUNC;
                    string SubLine = GetSequenceSubstateLine(item.Name, ExistingWorkflowLines);
                    
                    if (SubLine == null)
                    {
                        SubLine = GetSingleNodeLine(item.Name, ExistingWorkflowLines);
                        func = GetFuncCodeFromSingleNode(SubLine);
                    }
                    else
                    {
                        func = GetFuncCodeFromSequenceSubState(SubLine);
                    }                     

                    if (last)
                    {
                        if (string.IsNullOrEmpty(NextNodeName))
                        {
                            sbSeq.AppendLine($"{T4}.{ADD_SEQ_SUBSTATE}(\"{item.Name}\", {func}))");
                        }
                        else
                        {
                            sbSeq.AppendLine($"{T4}.{ADD_SEQ_SUBSTATE}(\"{item.Name}\", {func}),\"{NextNodeName}\")");
                        }
                    }
                    else
                    {
                        sbSeq.AppendLine($"{T4}.{ADD_SEQ_SUBSTATE}(\"{item.Name}\", {func})");
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(NextNodeName))
                {
                    sbSeq.AppendLine($"{T3}.{ADD_SEQ_NODE}(new {SEQ_STATE}(\"{NodeName}\"))");
                }
                else
                {
                    sbSeq.AppendLine($"{T3}.{ADD_SEQ_NODE}(new {SEQ_STATE}(\"{NodeName}\"), \"{NextNodeName}\")");
                }
            }
            sbBody.Append(sbSeq.ToString());
        }

        private void UpdateCodeForConditions(string NodeName, List<ConnectorViewModel> transitions)
        {
            StringBuilder sbCond = new StringBuilder();

            sbCond.AppendLine($"{T3}.{ADD_CONDS}(\"{NodeName}\", (s, t) =>");

            sbCond.AppendLine($"{T3}{{");

            string action = GetTriggerActionLine(NodeName, ExistingWorkflowLines);

            if (action != null)
            {
                sbCond.AppendLine($"{T4}t.{TRIGGER_ACTION} = {action};");
            }
            else
            {
                sbCond.AppendLine($"{T4}t.{TRIGGER_ACTION} = {DEFAULT_FUNC};");
            }

            foreach (var t in transitions)
            {
                string conditionLine = GetSingleConditionLine(t.Name, ExistingWorkflowLines);
                
                if (conditionLine != null)
                {
                    var cond = GetConditionCode(conditionLine);
                    
                    sbCond.AppendLine($"{T4}t.{ADD_COND}(\"{t.Name}\", {cond}, \"{t.GetTargetNode().Name}\");");
                }
                else
                {
                    sbCond.AppendLine($"{T4}t.{ADD_COND}(\"{t.Name}\", {DEFAULT_COND}, \"{t.GetTargetNode().Name}\");");
                }
            }
            sbCond.AppendLine($"{T3}}})");

            sbBody.Append(sbCond.ToString());
        }

        private void InsertCodeForConditions(string NodeName, List<ConnectorViewModel> transitions)
        {
            StringBuilder sbCond = new StringBuilder();

            sbCond.AppendLine($"{T3}.{ADD_CONDS}(\"{NodeName}\", (s, t) =>");
            sbCond.AppendLine($"{T3}{{");
            sbCond.AppendLine($"{T4}t.{TRIGGER_ACTION} = {DEFAULT_FUNC};");

            foreach (var t in transitions)
            {
                sbCond.AppendLine($"{T4}t.{ADD_COND}(\"{t.Name}\", {DEFAULT_COND}, \"{t.GetTargetNode().Name}\");");
            }
            sbCond.AppendLine($"{T3}}})");

            sbBody.Append(sbCond.ToString());
        }

        public string Compile()
        {
            sbBody.AppendLine($"{T3}.{BUILD}();");
            return sbBody.ToString();
        }

        public static string GetConditionCode(string Line)
        {
            if (Line == null)
            {
                return DEFAULT_FUNC;
            }
            var left = Line.IndexOf(',');
            var right = Line.LastIndexOf(',');

            if (left != -1 && right != -1)
            {
                return Line.Substring(left + 1, (right - left) - 1).Trim();
            }
            return DEFAULT_FUNC;
        }

        public static string GetFuncCodeFromSingleNode(string Line)
        {
            if (Line == null)
            {
                return DEFAULT_FUNC;
            }
            var left = Line.LastIndexOf(',');
            var right = Line.LastIndexOf(')');

            if (left != -1 && right != -1)
            {
                return Line.Substring(left + 1, (right - left) - 1).Trim();
            }
            return DEFAULT_FUNC;
        }

        public static string GetFuncCodeFromSequenceSubState(string Line)
        {
            var func = DEFAULT_FUNC;

            if (Line == null)
            {
                return func;
            }

            var left = Line.IndexOf('(');

            var right = Line.LastIndexOf(')');            

            if (left != -1 && right != -1)
            {
                var subline = Line.Substring(left + 1, right - left - 1);
                var count = subline.Count(x => x == ',');

                if (count == 1)
                {
                    var x = subline.Split(',')[1].Trim();
                    var open = x.Count(x => x == '(');
                    var close = x.Count(x => x == ')');
                    if (close > open) { x = x.Remove(x.Length - 1); }
                    func = x;
                }
                else if (count == 2)
                {
                    var x = subline.IndexOf(',');
                    var y = subline.LastIndexOf(')');

                    if (x != -1 && y != -1)
                    {
                        func = subline.Substring(x + 1, y - x - 1).Trim();
                    }
                }
            }
            return func;
        }

        public static string GetSingleConditionLine(string TransitionName, List<string> WorkflowLines)
        {
            var reg = $"\\.{ADD_COND}\\s*\\(\\s*[\"\"|\\']{TransitionName}[\"\"|\\']\\s*,";

            return SearchLine(reg, WorkflowLines);
        }

        public static string GetSingleNodeLine(string NodeName, List<string> WorkflowLines)
        {
            var reg = $"\\.{ADD_SINGLE_NODE}\\s*\\(\\s*[\"\"|\\']{NodeName}[\"\"|\\']\\s*\\,";

            return SearchLine(reg, WorkflowLines);
        }

        public static string GetSequenceNodeLine(string NodeName, List<string> WorkflowLines)
        {
            var reg = $"\\.{ADD_SEQ_NODE}\\s*\\(\\s*new\\s*{SEQ_STATE}\\s*\\([\"\"|\\']{NodeName}[\"\"|\\']\\s*\\)";

            return SearchLine(reg, WorkflowLines);
        }

        public static string GetSequenceSubstateLine(string NodeName, List<string> WorkflowLines)
        {
            var reg = $"\\.{ADD_SEQ_SUBSTATE}\\s*\\(\\s*[\"\"|\\']{NodeName}[\"\"|\\']\\s*.*\\)";

            return SearchLine(reg, WorkflowLines);
        }

        public static string GetConditionsLine(string NodeName, List<string>WorkflowLines)
        {
            var reg = $"\\.{ADD_CONDS}\\s*\\(\\s*[\"\"|\\']{NodeName}[\"\"|\\']\\s*";

            return SearchLine(reg, WorkflowLines);
        }

        public static string GetParallelNodeLine(string NodeName, List<string> WorkflowLines)
        {
            var reg = $"\\.{ADD_PARA_NODE}\\s*\\(\\s*new\\s*{PARA_STATE}\\s*\\([\"\"|\\']{NodeName}[\"\"|\\']\\s*\\)";

            return SearchLine(reg, WorkflowLines);
        }

        public static string GetSubWorkflowNodeLine(string NodeName, List<string> WorkflowLines)
        {
            var reg = $"\\.{ADD_SUBWF_NODE}\\s*\\(\\s*[\"\"|\\']{NodeName}[\"\"|\\']\\s*\\,";

            return SearchLine(reg, WorkflowLines);
        }

        public static string GetTriggerActionLine(string NodeName, List<string> WorkflowLines)
        {
            var s = GetConditionsLine(NodeName, WorkflowLines);

            var idx = WorkflowLines.IndexOf(s);
            if (idx != -1)
            {
                Regex ActionRegex = new Regex($"\\.{TRIGGER_ACTION}\\s*\\=\\s*.*;");

                Regex EndRegex = new Regex($"\\s*}}\\s*\\)");

                for (int i = idx; i < WorkflowLines.Count; i++)
                {
                    var line = WorkflowLines[i];

                    var match = ActionRegex.Match(line);
                    if (match.Success)
                    {
                        var left = line.IndexOf('=');
                            
                        var right = line.LastIndexOf(';');

                        if (left != -1 && right != -1)
                        {
                            return line.Substring(left + 1, (right - left) - 1).Trim();
                        }                           
                    }                    
                    var condEnd = EndRegex.Match(line);
                    if (condEnd.Success)
                    {
                        //Keine Action gefunden --> Abbruch um nicht andere Action zu bekommen
                        return null;
                    }
                }
            }
            return null;
        }

        private static string SearchLine(string Regex,List<string> WorkflowLines)
        {
            Regex regex = new Regex(Regex);
            foreach (var line in WorkflowLines)
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    return line;
                }
            }
            return null;
        }    
    }
}
