using mitoSoft.Workflows.Editor.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace mitoSoft.Workflows.Editor.Helpers.SchemeUpdater
{
    public delegate void DllChangedHandler(string FileChanged);

    public class DirectoryWatcher : IDisposable
    {

        public FileSystemWatcher watcher;

        public event DllChangedHandler OnFileChanged;

        double bufferDelay = 300;

        List<string> changedFilesList;

        public DirectoryWatcher(DllChangedHandler _onFileChanged)
        {
            watcher = new FileSystemWatcher();
            watcher.NotifyFilter = NotifyFilters.Size;
            watcher.Filter = "*.dll";
            OnFileChanged = _onFileChanged;
            watcher.IncludeSubdirectories = true;
            watcher.Changed += watcherChanegd;
            changedFilesList = new List<string>();
        }

        private void watcherChanegd(object sender, FileSystemEventArgs e)
        {
            string fullPath = e.FullPath;

            lock (changedFilesList)
            {
                if (changedFilesList.Contains(fullPath))
                    return;
                else
                    changedFilesList.Add(fullPath);
            }

            var timer = new Timer(bufferDelay) { AutoReset = false };

            timer.Elapsed += (object elapsedSender, ElapsedEventArgs elapsedArgs) =>
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    OnFileChanged.Invoke(e.FullPath);
                });

                lock (changedFilesList)
                {
                    changedFilesList.Remove(fullPath);
                }
            };
            timer.Start();
        }

        public void PathChanged(string newPath)
        {
            watcher.Path = newPath;
            watcher.EnableRaisingEvents = true;
            Debug.WriteLine("WatcherPath:" + newPath);
        }

        public void Dispose()
        {
            watcher.EnableRaisingEvents = false;
            watcher.Changed -= watcherChanegd;
            this.watcher.Dispose();
        }
    }
}
