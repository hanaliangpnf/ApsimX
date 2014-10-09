using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions
{
    /// <summary>
    /// This Function calculates a mean daily temperature from Max and Min weighted toward Max according to the specified MaximumTemperatureWeighting factor.  This is then passed into the XY matrix as the x property and the function returns the y value
    /// </summary>
    [Serializable]
    [Description("This Function calculates a mean daily temperature from Max and Min weighted toward Max according to the specified MaximumTemperatureWeighting factor.  This is then passed into the XY matrix as the x property and the function returns the y value")]
    public class WeightedTemperatureFunction : Function
    {
        #region Class Data Members
        /// <summary>Gets or sets the xy pairs.</summary>
        /// <value>The xy pairs.</value>
        public XYPairs XYPairs { get; set; }   // Temperature effect on Growth Interpolation Set

        /// <summary>The maximum temperature weighting</summary>
        public double MaximumTemperatureWeighting = 0.0;

        //[Input]
        //public NewMetType MetData;

        #endregion


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        [Units("0-1")]
        public override double Value
        {
            get
            {
                double Tav = MaximumTemperatureWeighting * MetData.MaxT + (1 - MaximumTemperatureWeighting) * MetData.MinT;
                return XYPairs.ValueIndexed(Tav);
            }
        }

    }
}
