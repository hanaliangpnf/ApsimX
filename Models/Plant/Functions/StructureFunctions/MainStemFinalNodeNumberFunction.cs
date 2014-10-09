using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Models.Core;

namespace Models.PMF.Functions.StructureFunctions
{
    /// <summary>
    /// Main stem final node number function
    /// </summary>
    [Serializable]
    [Description("This Function determines final leaf number for a crop.  If no childern are present final leaf number will be the same as primordia number, increasing at the same rate and reaching a fixed value when primordia initiation stops or when maximum leaf number is reached.  However, if a child function called 'FinalLeafNumber' is present that function will determine the increase and fixing of final leaf number")]
    public class MainStemFinalNodeNumberFunction : Function
    {
        /// <summary>The structure</summary>
        [Link]
        Structure Structure = null;

        /// <summary>The final leaf number</summary>
        [Link(IsOptional=true)]
        Function FinalLeafNumber = null;

        /// <summary>The _ final node number</summary>
        double _FinalNodeNumber = 0;
        /// <summary>The maximum main stem node number</summary>
        public double MaximumMainStemNodeNumber = 0;

        /// <summary>Updates the variables.</summary>
        /// <param name="initial">The initial.</param>
        public override void UpdateVariables(string initial)
        {
            if (initial == "yes")
                _FinalNodeNumber = MaximumMainStemNodeNumber;
            else
            {
                if (FinalLeafNumber == null)
                {
                    if (Structure.MainStemPrimordiaNo != 0)
                        _FinalNodeNumber = Math.Min(MaximumMainStemNodeNumber, Structure.MainStemPrimordiaNo);
                    else _FinalNodeNumber = MaximumMainStemNodeNumber;
                }
                else
                    _FinalNodeNumber = Math.Min(FinalLeafNumber.Value, MaximumMainStemNodeNumber);
            }
        }

        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            _FinalNodeNumber = 0;
        }


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public override double Value
        {
            get
            {
                return _FinalNodeNumber;
            }
        }
    }
}
