using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace mitoSoft.Workflows.Editor.Helpers.SchemeUpdater
{
    public static class PathHelper
    {
        public static string GetWorkflowProjectDirectory(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                int levels = 3;

                var projectFiles = GetFilesFromDirectory(path, "*.csproj");

                while (projectFiles?.Count == 0 && levels > 0)
                {
                    path = GetDirectoryParent(path);
                    projectFiles = GetFilesFromDirectory(path, "*.csproj");
                    levels--;
                }

                return GetDirectoryName(projectFiles?.First());
            }
            else
            {
                return null;
            }

        }

        public static string GetFilePathFromProjectDirectory(string SchemePath, string subWorkflowName)
        {
            string RootDir = GetWorkflowProjectDirectory(Path.GetDirectoryName(SchemePath));

            var projectFiles = GetFilesFromDirectory(RootDir, $"{subWorkflowName}*.xml", SearchOption.AllDirectories);

            return projectFiles.FirstOrDefault();
        }

        public static string GetDirectoryName(string FilePath)
        {
            try
            {
                return Path.GetDirectoryName(FilePath);
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        public static List<string> GetFilesFromDirectory(string directory, string SearchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            try
            {
                return Directory.GetFiles(directory, SearchPattern, searchOption).ToList(); ;
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        public static string GetDirectoryParent(string Path)
        {
            try
            {
                return Directory.GetParent(Path).FullName;
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        public static string GetFileNameWithoutExtension(string path)
        {
            try
            {
                return Path.GetFileNameWithoutExtension(path);
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }
    }
}
