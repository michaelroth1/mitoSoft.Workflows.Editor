using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using mitoSoft.Workflows.Editor.Machines;

namespace mitoSoft.Workflows.Editor.Helpers.SchemeUpdater
{
    public static class WorkflowLoader
    {
        public static SequenceStateMachine GetWorkflowInstance(string WorkflowName, string dllPath)
        {
            var type = GetWorkflowType(WorkflowName, dllPath);

            return type != null ? (SequenceStateMachine)Activator.CreateInstance(type) : null;
        }

        private static Type GetWorkflowType(string WorkflowName, string dllPath)
        {
            var types = GetWorkflowAssemblys(dllPath);

            var type = types.Where(x => x.Name == WorkflowName);

            if (type.Count() == 1)
            {
                return type.First();
            }
            return null;
        }

        private static List<Type> GetWorkflowAssemblys(string dllPath)
        {
            var assembly = Assembly.Load(File.ReadAllBytes(dllPath));

            return assembly.GetExportedTypes().ToList().Where(x => x.IsSubclassOf(typeof(SequenceStateMachine)) && !x.IsAbstract).ToList();

        }
    }
}
