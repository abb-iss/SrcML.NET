/******************************************************************************
 * Copyright (c) 2011 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace ABB.SrcML.Data.Test
{
    /// <summary>
    /// Summary description for DbCreationTests
    /// </summary>
    [TestClass]
    public class DbCreationTests
    {
        void HandleProgressEvent(object sender, ProgressEventArgs e)
        {
            Console.WriteLine("{0} Received Event for {1}: {2}", DateTime.Now, e.FileName, e.Message);
        }

        [TestMethod]
        public void CreateDbTest()
        {
            var testFileName = NppTests.NppXmlPath;
            
            using (var db = SrcMLDataContext.CreateDatabaseConnection(SrcMLDataContext.MakeDBName(testFileName), true))
            {
                Console.WriteLine("Connected to DB named {0}", db.Connection.Database);
                db.RaiseProgressEvent += HandleProgressEvent;
                db.Load(testFileName);
            }
            SrcMLDataContext.DropDatabase(SrcMLDataContext.MakeDBName(testFileName));
        }
    }
}
