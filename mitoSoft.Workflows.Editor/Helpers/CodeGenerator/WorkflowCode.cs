﻿using mitoSoft.StateMachine.AdvancedStateMachines;
using mitoSoft.Workflows.Editor.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


namespace mitoSoft.Workflows.Editor.Helpers.CodeGenerator
{
    public class WorkflowCode
    {        
        public string Namespace;

        public string WorkflowName;

        public string BaseStateMachine;

        public WFBody WFBody = new WFBody();
       
        StringBuilder sbUsings = new StringBuilder();

        public WorkflowCode(List<string> _Usings, string _Namespace, string _WorkflowName, string _BaseStateMachine, List<string> ExistingLines)
        {
            _Usings.ForEach(x=>sbUsings.AppendLine($"using {x};"));
            Namespace = _Namespace;
            WorkflowName = _WorkflowName;
            BaseStateMachine = _BaseStateMachine;
            WFBody.ExistingWorkflowLines = ExistingLines;
        }

        public string Compile()
        {
            return $@"{sbUsings}

namespace {Namespace}
{{
    public class {WorkflowName} : {BaseStateMachine}
    {{
        public {WorkflowName}()
        {{
            //dont remove region for code generation
            #region AutoGenerated StateMachine - {WorkflowName}
{WFBody.Compile()}
            #endregion;
        }}      
    }}    
}}";
        }
    }
}
