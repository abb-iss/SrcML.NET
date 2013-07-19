using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.SrcML.VisualStudio.SrcMLService.IntegrationTests {
    class TestHelpers {
        public static void CopyDirectory(string sourcePath, string destinationPath) {
            foreach(var fileTemplate in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)) {
                var fileName = fileTemplate.Replace(sourcePath, destinationPath);
                var directoryName = Path.GetDirectoryName(fileName);
                if(!Directory.Exists(directoryName)) {
                    Directory.CreateDirectory(directoryName);
                }
                File.Copy(fileTemplate, fileName);
            }
        }
        public static bool WaitForServiceToFinish(ISrcMLGlobalService service, int millisecondsTimeout) {
            if(!service.IsReady) {
                ManualResetEvent mre = new ManualResetEvent(false);
                EventHandler<IsReadyChangedEventArgs> action = (o, e) => { mre.Set(); };
                service.IsReadyChanged += action;
                mre.WaitOne(millisecondsTimeout);
                service.IsReadyChanged -= action;
            }
            return service.IsReady;
        }

        public static IEnumerable<Project> GetProjects(Solution solution) {
            var projects = solution.Projects;
            var enumerator = projects.GetEnumerator();
            while(enumerator.MoveNext()) {
                Project currentProject = enumerator.Current as Project;
                if(null != currentProject) {
                    yield return currentProject;
                }
            }
        }
    }
}
