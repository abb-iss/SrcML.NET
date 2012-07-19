/******************************************************************************
 * Copyright (c) 2010 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Xml.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;

namespace ABB.SrcML.Tools.Src2SrcMLPreview
{
    class SrcMLConverter : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (null != value)
            {
                XElement element = value as XElement;
                var tb = new TextBlock();

                tb.Inlines.Add(new Run { Text = element.Name.LocalName + " ", FontWeight = FontWeights.Bold });
                tb.Inlines.Add(new Run { Text = element.Value });
                return tb;
                //return String.Format("{0} : {1}", element.Name.LocalName, element.Value);
            }
            return String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
