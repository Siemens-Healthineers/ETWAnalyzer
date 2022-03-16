//// SPDX - FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETWAnalyzer.Configuration
{
    /// <summary>
    /// List of (MiniFilter) drivers which are commonly used by Antivirus and other solutions which can slow down 
    /// process start or file access.
    /// </summary>
    public class Drivers
    {
        /// <summary>
        /// Deserialized list
        /// </summary>
        public List<Driver> WellKnownDrivers
        {
            get;
            set;
        } = new List<Driver>();

        static Drivers myInstance;

        /// <summary>
        /// Get deserialized instance from Configuration folder
        /// </summary>
        internal static Drivers Default
        {
            get
            {
                if (myInstance == null)
                {
                    using StreamReader stream = File.OpenText(ConfigFiles.WellKnownDriverFiles);
                    JsonReader reader = new JsonTextReader(stream);
                    myInstance = JsonSerializer.CreateDefault().Deserialize<Drivers>(reader);
                }
                return myInstance;
            }
        }

        Dictionary<string, Driver> myDrivers;

        internal Driver TryGetDriverForModule(string module)
        {
            if( myDrivers == null)
            {
                myDrivers = new Dictionary<string, Driver>(StringComparer.OrdinalIgnoreCase);
                foreach(Driver known in WellKnownDrivers)
                {
                    myDrivers.Add(known.ModuleName, known);
                }
            }

            myDrivers.TryGetValue(module, out Driver driver);
            return driver;
        }
    }


    /// <summary>
    /// Well known Driver
    /// </summary>
    public class Driver
    {
        /// <summary>
        /// Dll/Driver Name
        /// </summary>
        public string ModuleName
        {
            get;set;
        }

        /// <summary>
        /// Company providing this filter driver
        /// </summary>
        public string Company
        {
            get;set;
        }

        /// <summary>
        /// Microsoft assigned driver category which is based on the drivers altitude. See https://docs.microsoft.com/en-us/windows-hardware/drivers/ifs/allocated-altitudes#400000---409999-fsfilter-top
        /// </summary>
        public string Category
        {
            get;set;
        }
    }

}
