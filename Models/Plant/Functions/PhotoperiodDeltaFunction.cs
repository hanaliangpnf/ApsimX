﻿using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Returns the difference between today's and yesterday's photoperiods in hours.
    /// </summary>
    [Serializable]
    [Description("Returns the difference between today's and yesterday's photoperiods in hours.")]
    public class PhotoperiodDeltaFunction : Function
    {
        //[Link]
        //Clock Clock = null;

        /// <summary>The twilight</summary>
        public double Twilight = 0;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public override double Value
        {
            get
            {
                double PhotoperiodToday = Utility.Math.DayLength(Clock.Today.DayOfYear, Twilight, MetData.Latitude);
                double PhotoperiodYesterday = Utility.Math.DayLength(Clock.Today.DayOfYear - 1, Twilight, MetData.Latitude);
                double PhotoperiodDelta = PhotoperiodToday - PhotoperiodYesterday;
                return PhotoperiodDelta;
            }
        }

    }
}
