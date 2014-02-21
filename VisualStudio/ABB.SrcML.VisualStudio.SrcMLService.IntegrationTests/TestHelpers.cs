using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.SrcML.VisualStudio.SrcMLService.IntegrationTests {
    [TestClass]
    public class TestHelpers {
        internal static Scaffold<ISrcMLGlobalService> TestScaffold;

        [AssemblyInitialize]
        public static void AssemblySetup(TestContext testContext) {
            TestScaffold = Scaffold<ISrcMLGlobalService>.Setup(new SrcMLServicePackage(), typeof(SSrcMLGlobalService));
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup() {
            TestScaffold.Cleanup();
        }

        
        internal static bool WaitForServiceToFinish(ISrcMLGlobalService service, int millisecondsTimeout) {
            AutoResetEvent monitoringStartedResetEvent = new AutoResetEvent(false),
                           updateArchivesStartedResetEvent = new AutoResetEvent(false),
                           updateArchivesCompletedResetEvent = new AutoResetEvent(false);

            EventHandler monitoringStartedEventHandler = GetEventHandler(monitoringStartedResetEvent),
                         updateStartedEventHandler = GetEventHandler(updateArchivesStartedResetEvent),
                         updateCompleteEventHandler = GetEventHandler(updateArchivesCompletedResetEvent);

            service.MonitoringStarted += monitoringStartedEventHandler;
            service.UpdateArchivesStarted += updateStartedEventHandler;
            service.UpdateArchivesCompleted += updateCompleteEventHandler;
            
            service.StartMonitoring();

            Assert.IsTrue(updateArchivesStartedResetEvent.WaitOne(millisecondsTimeout));
            Assert.IsTrue(monitoringStartedResetEvent.WaitOne(millisecondsTimeout));
            Assert.IsTrue(updateArchivesCompletedResetEvent.WaitOne(millisecondsTimeout));

            service.MonitoringStarted -= monitoringStartedEventHandler;
            service.UpdateArchivesStarted -= updateStartedEventHandler;
            service.UpdateArchivesCompleted -= updateCompleteEventHandler;

            return !service.IsUpdating;
        }

        internal static EventHandler GetEventHandler(AutoResetEvent resetEvent) {
            return (o, e) => resetEvent.Set();
        }
        internal static IEnumerable<Project> GetProjects(Solution solution) {
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
