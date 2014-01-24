﻿using System.Reflection;
using System;
using Models;
using System.Diagnostics;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Models.Core
{
    [Serializable]
    public class Simulation : Zone
    {
        // Private links
        [Link] private ISummary Summary = null;

        /// <summary>
        /// To commence the simulation, this event will be invoked.
        /// </summary>
        public event EventHandler Commenced;

        /// <summary>
        /// Return the filename that this simulation sits in.
        /// </summary>
        [XmlIgnore]
        public string FileName { get; set; }

        /// <summary>
        /// Run the simulation. Returns true if no fatal errors or exceptions.
        /// </summary>
        public bool Run()
        {
            bool ok = false;
            try
            {
                Utility.ModelFunctions.CallOnCommencing(this);
                
                if (Commenced != null)
                {
                    Commenced.Invoke(this, new EventArgs());
                    ok = true;
                }
                else
                    Summary.WriteError(FullPath, "Cannot invoke Commenced!");
            }
            catch (Exception err)
            {
                string Msg = err.Message;
                if (err.InnerException != null)
                    Msg += "\r\n" + err.InnerException.Message + "\r\n" + err.InnerException.StackTrace;
                else
                    Msg += "\r\n" + err.StackTrace;
                Summary.WriteError(FullPath, Msg);

                ok = false;
            }

            Utility.ModelFunctions.CallOnCompleted(this);
            ok &= true;

            return ok;
        }

        public void ResolveLinks()
        {
            Utility.ModelFunctions.ResolveLinks(this);
        }
    }

}