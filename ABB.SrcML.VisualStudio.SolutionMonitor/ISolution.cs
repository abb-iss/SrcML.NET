using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;

namespace ABB.SrcML.VisualStudio.SolutionMonitor
{
    public class SolutionWrapper
    {
        public virtual ProjectItem FindProjectItem(string name)
        {
            throw new NotImplementedException();
        }

        public virtual Projects getProjects()
        {
            throw new NotImplementedException();
        }

        public static SolutionWrapper Create(Solution openSolution)
        {
            return new StandardSolutionWrapper(openSolution);
        }
    }

    public class StandardSolutionWrapper : SolutionWrapper
    {
        private Solution _mySolution;
        public StandardSolutionWrapper(Solution s)
        {
            _mySolution = s;
        }

        public override ProjectItem FindProjectItem(string name)
        {
            return _mySolution.FindProjectItem(name);
        }

        public override Projects getProjects()
        {
            return _mySolution.Projects;
        }
    }

}
