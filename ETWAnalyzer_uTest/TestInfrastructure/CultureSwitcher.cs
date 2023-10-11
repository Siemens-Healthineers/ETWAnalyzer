using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETWAnalyzer_uTest.TestInfrastructure
{
    /// <summary>
    /// Set thread language to English and revert on dispose to allow stable unit tests with a specific culture dependant formatting.
    /// </summary>
    internal class CultureSwitcher : IDisposable
    {
        CultureInfo myUICulture;
        CultureInfo myThreadCulture;

        /// <summary>
        /// Set Thread language to English. This sets CurrentUICulture and CurrentCulture.
        /// </summary>
        public CultureSwitcher() 
        {
            myUICulture = Thread.CurrentThread.CurrentUICulture;
            myThreadCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");
        }

        /// <summary>
        /// Reset culture to previous values
        /// </summary>
        public void Dispose()
        {
            Thread.CurrentThread.CurrentUICulture = myUICulture;
            Thread.CurrentThread.CurrentCulture = myThreadCulture;
        }
    }
}
