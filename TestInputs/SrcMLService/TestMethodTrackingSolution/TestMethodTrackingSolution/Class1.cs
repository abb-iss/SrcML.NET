using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestMethodTrackingSolution
{
    public class Class1
    {
		int a {set; get;}
		
		public void member1(int x, string y)
		{
			return;//a position inside a method
		}
		//a position not in a method
		
		public void member2()
		{
			return;
		}
    }
}
