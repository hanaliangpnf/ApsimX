using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Utilities;
using Newtonsoft.Json;

namespace Models.Functions
{
    /// <summary>
    /// This function is calculated from an XY matrix which returns a value for Y 
    /// interpolated from the Xvalue provided.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.XYPairsView")]
    [PresenterName("UserInterface.Presenters.XYPairsPresenter")]
    [Description("Returns the corresponding Y value for a given X value, based on the line shape defined by the specified XY matrix.")]
    public class XYPairs : Model, IFunction, IIndexedFunction, IGridModel
    {
        /// <summary>Gets or sets the x.</summary>
        [Description("X")]
        public double[] X { get; set; }

        /// <summary>Gets or sets the y.</summary>
        [Description("Y")]
        public double[] Y { get; set; }

        /// <summary>Tabular data. Called by GUI.</summary>
        [JsonIgnore]
        public List<GridTable> Tables
        {
            get
            {
                var columns = new List<GridTableColumn>();
                columns.Add(new GridTableColumn("X", new VariableProperty(this, GetType().GetProperty("X"))));
                columns.Add(new GridTableColumn("Y", new VariableProperty(this, GetType().GetProperty("Y"))));
                List<GridTable> tables = new List<GridTable>();
                tables.Add(new GridTable(Name, columns, this));
                return tables;
            }
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">Cannot call Value on XYPairs function. Must be indexed.</exception>
        public double Value(int arrayIndex = -1)
        {
            throw new Exception("Cannot call Value on XYPairs function. Must be indexed.");
        }

        /// <summary>Values the indexed.</summary>
        /// <param name="dX">The d x.</param>
        /// <returns></returns>
        public double ValueIndexed(double dX)
        {
            bool DidInterpolate = false;
            return MathUtilities.LinearInterpReal(dX, X, Y, out DidInterpolate);
        }
    }
}
