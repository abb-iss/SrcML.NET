using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.VisualStudio.SrcMLService.IntegrationTests {
    class TestConstants {
        public static string SolutionDirectory { get; private set; }
        public static string InputFolderPath { get; private set; }
        public static string TemplatesFolder { get; private set; }

        static TestConstants() {
            SolutionDirectory = TestHelpers.GetSolutionDirectory();
            InputFolderPath = Path.Combine(SolutionDirectory, "TestInputs", "SrcMLService");
            TemplatesFolder = Path.Combine(InputFolderPath, "Template");
        }
        //public const string InputFolderPath = @"..\..\..\TestInputs\SrcMLService\";
        //public const string TemplatesFolder = @"..\..\..\TestInputs\SrcMLService\Template";
    }
}
