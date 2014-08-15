﻿using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using Models.Core;
using Models;
using Models.PMF;
using Models.PMF.Slurp;
using System.Xml.Serialization;

namespace Models
{
        [Serializable]
    public class CanopyEnergyBalanceInterceptionlayerType
    {
        public double thickness;
        public double amount;
    }
    public class KeyValueArraypair_listType
    {
        public string key = "";
        public double value;
    }

    public class ChangeGSMaxType
    {
        public string component = "";
        public double dlt;
    }
    public class KeyValueArrayType
    {
        public KeyValueArraypair_listType[] pair_list;
    }
    
    [Serializable]
    public class NewCanopyType
    {
        public string sender = "";
        public double height;
        public double depth;
        public double lai;
        public double lai_tot;
        public double cover;
        public double cover_tot;
    }

    public delegate void KeyValueArraypair_listDelegate(KeyValueArraypair_listType Data);


    /// <remarks>
    /// <para>
    /// I have generally followed the original division of interface code and "science" code
    /// into different units (formerly MicroMet.for and MicroScience.for)
    /// </para>
    ///
    /// <para>
    /// Function routines were changed slightly as part of the conversion process. The "micromet_"
    /// prefixes were dropped, as the functions are now members of a MicroMet class, and that class
    /// membership keeps them distinguishable from other functions. A prefix of "Calc" was added to
    /// some function names (e.g., CalcAverageT) if, after dropping the old "micromet_" prefix, there
    /// was potential confusion between the function name and a variable name.
    /// </para>
    ///
    /// <para> The following Fortran routines, originally in MicroScience.for, were NOT converted, 
    /// because they were not actively being used:</para>
    /// <para>    micromet_PenmanMonteith (the converted routine below was originally micromet_Penman_Monteith from MicroMet.for)</para>
    /// <para>    micromet_ActualCanopyCond (the routine below was originally micromet_CanopyConductance)</para>
    /// <para>    micromet_FrictionVelocity</para>
    /// <para>    micromet_ZeroPlaneDispl</para>
    /// <para>    micromet_RoughnessLength</para>
    /// <para>    micromet_Radn2SolRad</para>
    /// <para>    micromet_FreeEvapRate</para>
    /// <para>    micromet_AerodynamicConductance (the routine below was originally micromet_AerodynamicConductanceFAO)</para>
    /// </remarks>
    /// <summary>
    /// A more-or-less direct port of the Fortran MicroMet model
    /// Ported by Eric Zurcher Jun 2011, first to C#, then automatically
    /// to VB via the converter in SharpDevelop.
    /// Ported back to C# by Dean Holzworth
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public partial class MicroClimate : Model
    {
        [Link]
        Clock Clock = null;

        [Link]
        WeatherFile Weather = null;

        private double _albedo = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public MicroClimate()
        {
            a_interception = 0.0;
            b_interception = 1.0;
            c_interception = 0.0;
            d_interception = 0.0;
            soil_albedo = 0.23;
        }


        #region "Parameters used to initialise the model"
        #region "Parameters set in the GUI by the user"
        [Description("a_interception")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm/mm")]
        public double a_interception { get; set; }

        [Description("b_interception")]
        [Bounds(Lower = 0.0, Upper = 5.0)]
        public double b_interception {get; set;}

        [Description("c_interception")]
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("mm")]
        public double c_interception { get; set; }

        [Description("d_interception")]
        [Bounds(Lower = 0.0, Upper = 20.0)]
        [Units("mm")]
        public double d_interception { get; set; }

        [Description("soil albedo")]
        [Bounds(Lower = 0.0, Upper = 1.0)]
        public double soil_albedo { get; set; }

        #endregion

        #region "Parameters not normally settable from the GUI by the user"
        [Bounds(Lower = 900.0, Upper = 1100.0)]
        [Units("hPa")]
        [Description("")]

        public double air_pressure = 1010;
        [Bounds(Lower = 0.9, Upper = 1.0)]
        [Units("")]
        [Description("")]

        public double soil_emissivity = 0.96;

        [Bounds(Lower = -20.0, Upper = 20.0)]
        [Units("deg")]
        [Description("")]

        public double sun_angle = 15.0;
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [Description("")]

        public double soil_heat_flux_fraction = 0.4;
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("")]
        [Description("")]

        public double night_interception_fraction = 0.5;
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("m/s")]
        [Description("")]

        public double windspeed_default = 3.0;
        [Bounds(Lower = 0.0, Upper = 10.0)]
        [Units("m")]
        [Description("")]

        public double refheight = 2.0;
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("0-1")]
        [Description("")]

        public double albedo = 0.15;
        [Bounds(Lower = 0.9, Upper = 1.0)]
        [Units("0-1")]
        [Description("")]

        public double emissivity = 0.96;
        [Bounds(Lower = 0.0, Upper = 1.0)]
        [Units("m/s")]
        [Description("")]

        public double gsmax = 0.01;
        [Bounds(Lower = 0.0, Upper = 1000.0)]
        [Units("W/m^2")]
        [Description("")]

        public double r50 = 200;
        #endregion

        #endregion

        #region "Outputs we make available"

        [Units("mm")]
        public double interception
        {
            get
            {
                double totalInterception = 0.0;
                for (int i = 0; i <= numLayers - 1; i++)
                {
                    for (int j = 0; j <= ComponentData.Count - 1; j++)
                    {
                        totalInterception += ComponentData[j].interception[i];
                    }
                }
                return totalInterception;
            }
        }

        public double gc
        {
            // Should this be returning a sum or an array instead of just the first value???
            get { return ((ComponentData.Count > 0) && (numLayers > 0)) ? ComponentData[0].Gc[0] : 0.0; }
        }

        public double ga
        {
            // Should this be returning a sum or an array instead of just the first value???
            get { return ((ComponentData.Count > 0) && (numLayers > 0)) ? ComponentData[0].Ga[0] : 0.0; }
        }

        public double petr
        {
            get
            {
                double totalPetr = 0.0;
                for (int i = 0; i <= numLayers - 1; i++)
                {
                    for (int j = 0; j <= ComponentData.Count - 1; j++)
                    {
                        totalPetr += ComponentData[j].PETr[i];
                    }
                }
                return totalPetr;
            }
        }

        public double peta
        {
            get
            {
                double totalPeta = 0.0;
                for (int i = 0; i <= numLayers - 1; i++)
                {
                    for (int j = 0; j <= ComponentData.Count - 1; j++)
                    {
                        totalPeta += ComponentData[j].PETa[i];
                    }
                }
                return totalPeta;
            }
        }

        public double net_radn
        {
            get { return radn * (1.0 - _albedo) + netLongWave; }
        }

        public double net_rs
        {
            get { return radn * (1.0 - _albedo); }
        }

        public double net_rl
        {
            get { return netLongWave; }
        }

        [XmlIgnore]
        public double soil_heat = 0.0;

        [XmlIgnore]
        public double dryleaffraction = 0.0;

        public KeyValueArrayType gsmax_array
        {
            get
            {
                KeyValueArrayType _gsmax_array = new KeyValueArrayType();
                Array.Resize<KeyValueArraypair_listType>(ref _gsmax_array.pair_list, ComponentData.Count);
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    _gsmax_array.pair_list[j] = new KeyValueArraypair_listType();
                    _gsmax_array.pair_list[j].key = ComponentData[j].Name;
                    _gsmax_array.pair_list[j].value = ComponentData[j].Gsmax;
                }
                return _gsmax_array;
            }
        }
        #endregion

        #region "Events to which we subscribe, and their handlers"

        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            day = Clock.Today.DayOfYear;
            year = Clock.Today.Year;
            //DateUtility.JulianDayNumberToDayOfYear(time.startday, day, year)
        }

        [EventSubscribe("ChangeGSMax")]
        private void OnChangeGSMax(ChangeGSMaxType ChangeGSMax)
        {
            int senderIdx = FindComponentIndex(ChangeGSMax.component);
            if (senderIdx < 0)
            {
                throw new Exception("Unknown Canopy Component: " + Convert.ToString(ChangeGSMax.component));
            }
            ComponentData[senderIdx].Gsmax += ChangeGSMax.dlt;
        }



        /// <summary>
        /// Obtain all relevant met data
        /// </summary>
        [EventSubscribe("NewWeatherDataAvailable")]
        private void OnNewWeatherDataAvailable(object sender, EventArgs e)
        {
            radn = Weather.MetData.Radn;
            maxt = Weather.MetData.Maxt;
            mint = Weather.MetData.Mint;
            rain = Weather.MetData.Rain;
            vp = Weather.MetData.VP;
            wind = Weather.MetData.Wind;
        }

        [EventSubscribe("DoCanopy")]
        private void OnDoCanopy(object sender, EventArgs e)
        {
            CalculateGc();
            CalculateGa();
            CalculateInterception();
            CalculatePM();
            CalculateOmega();

            SendEnergyBalanceEvent();
        }

        [EventSubscribe("DoCanopyEnergyBalance")]
        private void OnDoCanopyEnergyBalance(object sender, EventArgs e)
        {
            MetVariables();
            CanopyCompartments();
            BalanceCanopyEnergy();

            // Loop through all crops and get their potential growth for today.
            foreach (ICrop crop in FindAll(typeof(ICrop)))
            {
                int senderIdx = FindComponentIndex(crop.CropType);
                if (senderIdx < 0)
                {
                    throw new Exception("Unknown Canopy Component: " + crop.CropType);
                }
                ComponentData[senderIdx].Crop = crop;
                ComponentData[senderIdx].Frgr = crop.FRGR;
            }
        }

        /// <summary>
        /// Register presence of a new crop
        /// </summary>
        [EventSubscribe("Sowing")]
        private void OnSowing(object sender, EventArgs e)
        {
            Model newCrop = sender as Model;

            int senderIdx = FindComponentIndex(newCrop.Name);

            // If sender is unknown, add it to the list
            if (senderIdx == -1)
                throw new ApsimXException(FullPath, "Cannot find MicroClimate definition for crop '" + newCrop.Name + "'");
            ComponentData[senderIdx].Name = newCrop.Name;
            if (newCrop is Plant)
                ComponentData[senderIdx].Type = (newCrop as Plant).CropType;
            else
                ComponentData[senderIdx].Type = newCrop.Name;
            Clear(ComponentData[senderIdx]);
        }

        /// <summary>
        /// Register presence of slurp
        /// </summary>
        [EventSubscribe("StartSlurp")]
        private void OnStartSlurp(object sender, EventArgs e)
        {
            Slurp newSlurp = sender as Slurp;

            int senderIdx = FindComponentIndex(newSlurp.Name);

            // If sender is unknown, add it to the list
            if (senderIdx == -1)
                throw new ApsimXException(FullPath, "Cannot find MicroClimate definition for Slurp");
            ComponentData[senderIdx].Name = newSlurp.Name;
            ComponentData[senderIdx].Type = newSlurp.CropType;
            Clear(ComponentData[senderIdx]);
        }

        private void Clear(ComponentDataStruct c)
        {
            c.CoverGreen = 0;
            c.CoverTot = 0;
            c.Depth = 0;
            c.Frgr = 0;
            c.Height = 0;
            c.K = 0;
            c.Ktot = 0;
            c.LAI = 0;
            c.LAItot = 0;
            Util.ZeroArray(c.layerLAI);
            Util.ZeroArray(c.layerLAItot);
            Util.ZeroArray(c.Ftot);
            Util.ZeroArray(c.Fgreen);
            Util.ZeroArray(c.Rs);
            Util.ZeroArray(c.Rl);
            Util.ZeroArray(c.Rsoil);
            Util.ZeroArray(c.Gc);
            Util.ZeroArray(c.Ga);
            Util.ZeroArray(c.PET);
            Util.ZeroArray(c.PETr);
            Util.ZeroArray(c.PETa);
            Util.ZeroArray(c.Omega);
            Util.ZeroArray(c.interception);
        }

        [EventSubscribe("NewCanopy")]
        private void OnNewCanopy(NewCanopyType newCanopy)
        {
            int senderIdx = FindComponentIndex(newCanopy.sender);
            if (senderIdx < 0)
            {
                throw new Exception("Unknown Canopy Component: " + Convert.ToString(newCanopy.sender));
            }
            ComponentData[senderIdx].LAI = newCanopy.lai;
            ComponentData[senderIdx].LAItot = newCanopy.lai_tot;
            ComponentData[senderIdx].CoverGreen = newCanopy.cover;
            ComponentData[senderIdx].CoverTot = newCanopy.cover_tot;
            ComponentData[senderIdx].Height = Math.Round(newCanopy.height, 5) / 1000.0;
            // Round off a bit and convert mm to m
            ComponentData[senderIdx].Depth = Math.Round(newCanopy.depth, 5) / 1000.0;
            // Round off a bit and convert mm to m
        }

        public override void OnLoaded()
        {
            ComponentData = new List<ComponentDataStruct>();
            foreach (ComponentDataStruct c in ComponentData)
                Clear(c);
            AddCropTypes();
        }

        public override void OnSimulationCommencing()
        {
            _albedo = albedo;
            windspeed_checked = false;
            netLongWave = 0;
            sumRs = 0;
            averageT = 0;
            sunshineHours = 0;
            fractionClearSky = 0;
            dayLength = 0;
            dayLengthLight = 0;
            numLayers = 0;
            DeltaZ = new double[-1 + 1];
            layerKtot = new double[-1 + 1];
            layerLAIsum = new double[-1 + 1];
           
        }


        #endregion

        #region "Useful constants"
        // Teten coefficients
        private const double svp_A = 6.106;
        // Teten coefficients
        private const double svp_B = 17.27;
        // Teten coefficients
        private const double svp_C = 237.3;
        // 0 C in Kelvin (g_k)
        private const double abs_temp = 273.16;
        // universal gas constant (J/mol/K)
        private const double r_gas = 8.3143;
        // molecular weight water (kg/mol)
        private const double mwh2o = 0.018016;
        // molecular weight air (kg/mol)
        private const double mwair = 0.02897;
        // molecular fraction of water to air ()
        private const double molef = mwh2o / mwair;
        // Specific heat of air at constant pressure (J/kg/K)
        private const double Cp = 1010.0;
        // Stefan-Boltzman constant
        private const double stef_boltz = 5.67E-08;
        // constant for cloud effect on longwave radiation
        private const double c_cloud = 0.1;
        // convert degrees to radians
        private const double Deg2Rad = Math.PI / 180.0;
        // kg/m3
        private const double RhoW = 998.0;
        // weights vpd towards vpd at maximum temperature
        private const double svp_fract = 0.66;
        private const double SunSetAngle = 0.0;
        // hours to seconds
        private const double hr2s = 60.0 * 60.0;
        #endregion
        #region "Various class variables"

        [Serializable]
        public class ComponentDataStruct
        {
            public string Name;
            public string Type;
            public ICrop Crop;

            [XmlIgnore]
            public double LAI;
            [XmlIgnore]
            public double LAItot;
            [XmlIgnore]
            public double CoverGreen;
            [XmlIgnore]
            public double CoverTot;
            [XmlIgnore]
            public double Ktot;
            [XmlIgnore]
            public double K;
            [XmlIgnore]
            public double Height;
            [XmlIgnore]
            public double Depth;
            [XmlIgnore]
            public double Albedo {get; set;}
            [XmlIgnore]
            public double Emissivity {get; set;}
            [XmlIgnore]
            public double Gsmax {get; set;}
            [XmlIgnore]
            public double R50 {get; set;}
            [XmlIgnore]
            public double Frgr;
            [XmlIgnore]
            public double[] layerLAI;
            [XmlIgnore]
            public double[] layerLAItot;
            [XmlIgnore]
            public double[] Ftot;
            [XmlIgnore]
            public double[] Fgreen;
            [XmlIgnore]
            public double[] Rs;
            [XmlIgnore]
            public double[] Rl;
            [XmlIgnore]
            public double[] Rsoil;
            [XmlIgnore]
            public double[] Gc;
            [XmlIgnore]
            public double[] Ga;
            [XmlIgnore]
            public double[] PET;
            [XmlIgnore]
            public double[] PETr;
            [XmlIgnore]
            public double[] PETa;
            [XmlIgnore]
            public double[] Omega;
            [XmlIgnore]
            public double[] interception;
        }

        private void AddCropTypes()
        {
            SetupCropTypes("crop", "Crop");
            SetupCropTypes("broccoli", "Crop");
            SetupCropTypes("tree", "Tree");
            SetupCropTypes("eucalyptus", "Tree");
            SetupCropTypes("oilpalm", "Tree");
            SetupCropTypes("oilmallee", "Tree");
            SetupCropTypes("globulus", "Tree");
            SetupCropTypes("camaldulensis", "Tree");
            SetupCropTypes("grass", "Grass");
            SetupCropTypes("wheat", "Crop");
            SetupCropTypes("barley", "Crop");
            SetupCropTypes("canola", "Crop");
            SetupCropTypes("raphanus_raphanistrum", "Crop");
            SetupCropTypes("lolium_rigidum", "Crop");
            SetupCropTypes("chickpea", "Crop");
            SetupCropTypes("weed", "Crop");
            SetupCropTypes("oats", "Crop");
            SetupCropTypes("chickpea", "Crop");
            SetupCropTypes("fieldpea", "Crop");
            SetupCropTypes("sugar", "Crop");
            SetupCropTypes("potato", "Potato");
            SetupCropTypes("frenchbean", "Crop");
            SetupCropTypes("bambatsi", "C4grass");
            SetupCropTypes("lucerne", "Crop");
            SetupCropTypes("maize", "Crop");
            SetupCropTypes("banksia", "Tree");
            SetupCropTypes("understorey", "Crop");
            SetupCropTypes("ryegrass", "Grass");
            SetupCropTypes("vine", "Crop");
            SetupCropTypes("saltbush", "Tree");
            SetupCropTypes("sorghum", "Crop");
            SetupCropTypes("danthonia", "Grass");
            SetupCropTypes("nativepasture", "C4Grass");
            SetupCropTypes("raphanus_raphanistrum", "Crop");
            SetupCropTypes("canola", "Crop");
            SetupCropTypes("kale2", "Crop");
            SetupCropTypes("Carrots4", "Crop");
            SetupCropTypes("Slurp", "Crop");
            SetupCropTypes("AgPasture", "Crop");
        }

        private void SetupCropTypes(string Name, string Type)
        {
            ComponentDataStruct CropType = new ComponentDataStruct();
            CropType.Name = Name;

            //Set defalst
            CropType.Albedo = 0.15;
            CropType.Gsmax = 0.01;
            CropType.Emissivity = 0.96;
            CropType.R50 = 200;

            //Override type specific values
            if (Type.Equals("Crop"))
            {
                CropType.Albedo = 0.26;
                CropType.Gsmax=0.011;
            }
            if (Type.Equals("Potato"))
            {
                CropType.Albedo = 0.26;
                CropType.Gsmax = 0.03;
            }
            else if (Type.Equals("Grass"))
            {
                CropType.Albedo = 0.23;
            }
            else if (Type.Equals("C4grass"))
            {
                CropType.Albedo = 0.23;
                CropType.Gsmax = 0.015;
                CropType.R50 = 150;
            }
            else if (Type.Equals("Tree"))
            {
                CropType.Albedo = 0.15;
                CropType.Gsmax = 0.005;
            }
            else if (Type.Equals("Tree2"))
            {
                CropType.Albedo = 0.15;
                CropType.R50 = 100;
            }

            ComponentData.Add(CropType);
        }

        private double maxt;
        private double mint;
        private double radn;
        private double rain;
        private double vp;
        private double wind;
        private bool use_external_windspeed;

        private bool windspeed_checked = false;
        private int day;

        private int year;
        private double netLongWave;
        private double sumRs;
        private double averageT;
        private double sunshineHours;
        private double fractionClearSky;
        private double dayLength;
        private double dayLengthLight;
        private double[] DeltaZ = new double[-1 + 1];
        private double[] layerKtot = new double[-1 + 1];
        private double[] layerLAIsum = new double[-1 + 1];
        private int numLayers;

        [XmlElement("ComponentData")]
        [XmlIgnore]
        public List<ComponentDataStruct> ComponentData { get; set; }

        #endregion

        private double FetchTableValue(string field, int compNo, int layerNo)
        {
            if (field == "LAI")
            {
                return ComponentData[compNo].layerLAI[layerNo];
            }
            else if (field == "Ftot")
            {
                return ComponentData[compNo].Ftot[layerNo];
            }
            else if (field == "Fgreen")
            {
                return ComponentData[compNo].Fgreen[layerNo];
            }
            else if (field == "Rs")
            {
                return ComponentData[compNo].Rs[layerNo];
            }
            else if (field == "Rl")
            {
                return ComponentData[compNo].Rl[layerNo];
            }
            else if (field == "Gc")
            {
                return ComponentData[compNo].Gc[layerNo];
            }
            else if (field == "Ga")
            {
                return ComponentData[compNo].Ga[layerNo];
            }
            else if (field == "PET")
            {
                return ComponentData[compNo].PET[layerNo];
            }
            else if (field == "PETr")
            {
                return ComponentData[compNo].PETr[layerNo];
            }
            else if (field == "PETa")
            {
                return ComponentData[compNo].PETa[layerNo];
            }
            else if (field == "Omega")
            {
                return ComponentData[compNo].Omega[layerNo];
            }
            else
            {
                throw new Exception("Unknown table element: " + field);
            }
        }

        private int FindComponentIndex(string name)
        {
            for (int i = 0; i <= ComponentData.Count - 1; i++)
            {
                if (ComponentData[i].Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    return i;
                }
            }
            // Couldn't find

            if (name.Equals("wheat", StringComparison.CurrentCultureIgnoreCase))
            {
                // crop type
                ComponentData.Add(new ComponentDataStruct());
                return ComponentData.Count - 1;
            }
            return -1;
        }


        private void CanopyCompartments()
        {
            DefineLayers();
            DivideComponents();
            LightExtinction();
        }

        private void MetVariables()
        {
            // averageT = (maxt + mint) / 2.0;
            averageT = CalcAverageT(mint, maxt);

            // This is the length of time within the day during which
            //  Evaporation will take place
            dayLength = CalcDayLength(Weather.Latitude, day, sun_angle);

            // This is the length of time within the day during which
            // the sun is above the horizon
            dayLengthLight = CalcDayLength(Weather.Latitude, day, SunSetAngle);

            sunshineHours = CalcSunshineHours(radn, dayLengthLight, Weather.Latitude, day);

            fractionClearSky = Utility.Math.Divide(sunshineHours, dayLengthLight, 0.0);
        }

        /// <summary>
        /// Break the combined Canopy into layers
        /// </summary>
        private void DefineLayers()
        {
            double[] nodes = new double[2 * ComponentData.Count];
            int numNodes = 1;
            for (int compNo = 0; compNo <= ComponentData.Count - 1; compNo++)
            {
                double height = ComponentData[compNo].Height;
                double canopyBase = height - ComponentData[compNo].Depth;
                if (Array.IndexOf(nodes, height) == -1)
                {
                    nodes[numNodes] = height;
                    numNodes = numNodes + 1;
                }
                if (Array.IndexOf(nodes, canopyBase) == -1)
                {
                    nodes[numNodes] = canopyBase;
                    numNodes = numNodes + 1;
                }
            }
            Array.Resize<double>(ref nodes, numNodes);
            Array.Sort(nodes);
            numLayers = numNodes - 1;
            if (DeltaZ.Length != numLayers)
            {
                // Number of layers has changed; adjust array lengths
                Array.Resize<double>(ref DeltaZ, numLayers);
                Array.Resize<double>(ref layerKtot, numLayers);
                Array.Resize<double>(ref layerLAIsum, numLayers);

                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    Array.Resize<double>(ref ComponentData[j].Ftot, numLayers);
                    Array.Resize<double>(ref ComponentData[j].Fgreen, numLayers);
                    Array.Resize<double>(ref ComponentData[j].Rs, numLayers);
                    Array.Resize<double>(ref ComponentData[j].Rl, numLayers);
                    Array.Resize<double>(ref ComponentData[j].Rsoil, numLayers);
                    Array.Resize<double>(ref ComponentData[j].Gc, numLayers);
                    Array.Resize<double>(ref ComponentData[j].Ga, numLayers);
                    Array.Resize<double>(ref ComponentData[j].PET, numLayers);
                    Array.Resize<double>(ref ComponentData[j].PETr, numLayers);
                    Array.Resize<double>(ref ComponentData[j].PETa, numLayers);
                    Array.Resize<double>(ref ComponentData[j].Omega, numLayers);
                    Array.Resize<double>(ref ComponentData[j].interception, numLayers);
                }
            }
            for (int i = 0; i <= numNodes - 2; i++)
            {
                DeltaZ[i] = nodes[i + 1] - nodes[i];
            }
        }

        /// <summary>
        /// Break the components into layers
        /// </summary>
        private void DivideComponents()
        {
            double[] Ld = new double[ComponentData.Count];
            for (int j = 0; j <= ComponentData.Count - 1; j++)
            {
                ComponentDataStruct componentData = ComponentData[j];

                componentData.layerLAI = new double[numLayers];
                componentData.layerLAItot = new double[numLayers];
                Ld[j] = Utility.Math.Divide(componentData.LAItot, componentData.Depth, 0.0);
            }
            double top = 0.0;
            double bottom = 0.0;

            for (int i = 0; i <= numLayers - 1; i++)
            {
                bottom = top;
                top = top + DeltaZ[i];
                layerLAIsum[i] = 0.0;

                // Calculate LAI for layer i and component j
                // ===========================================
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentDataStruct componentData = ComponentData[j];

                    if ((componentData.Height > bottom) && (componentData.Height - componentData.Depth < top))
                    {
                        componentData.layerLAItot[i] = Ld[j] * DeltaZ[i];
                        componentData.layerLAI[i] = componentData.layerLAItot[i] * Utility.Math.Divide(componentData.LAI, componentData.LAItot, 0.0);
                        layerLAIsum[i] += componentData.layerLAItot[i];
                    }
                }

                // Calculate fractional contribution for layer i and component j
                // ====================================================================
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentDataStruct componentData = ComponentData[j];

                    componentData.Ftot[i] = Utility.Math.Divide(componentData.layerLAItot[i], layerLAIsum[i], 0.0);
                    // Note: Sum of Fgreen will be < 1 as it is green over total
                    componentData.Fgreen[i] = Utility.Math.Divide(componentData.layerLAI[i], layerLAIsum[i], 0.0);
                }
            }
        }

        /// <summary>
        /// Calculate light extinction parameters
        /// </summary>
        private void LightExtinction()
        {
            // Calculate effective K from LAI and cover
            // =========================================
            for (int j = 0; j <= ComponentData.Count - 1; j++)
            {
                ComponentDataStruct componentData = ComponentData[j];

                if (Utility.Math.FloatsAreEqual(ComponentData[j].CoverGreen, 1.0, 1E-05))
                {
                    throw new Exception("Unrealistically high cover value in MicroMet i.e. > -.9999");
                }

                componentData.K = Utility.Math.Divide(-Math.Log(1.0 - componentData.CoverGreen), componentData.LAI, 0.0);
                componentData.Ktot = Utility.Math.Divide(-Math.Log(1.0 - componentData.CoverTot), componentData.LAItot, 0.0);
            }

            // Calculate extinction for individual layers
            // ============================================
            for (int i = 0; i <= numLayers - 1; i++)
            {
                layerKtot[i] = 0.0;
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentDataStruct componentData = ComponentData[j];

                    layerKtot[i] += componentData.Ftot[i] * componentData.Ktot;

                }
            }
        }

        /// <summary>
        /// Perform the overall Canopy Energy Balance
        /// </summary>
        private void BalanceCanopyEnergy()
        {
            ShortWaveRadiation();
            EnergyTerms();
            LongWaveRadiation();
            SoilHeatRadiation();
        }

        /// <summary>
        /// Calculate the canopy conductance for system compartments
        /// </summary>
        private void CalculateGc()
        {
            double Rin = radn;

            for (int i = numLayers - 1; i >= 0; i += -1)
            {
                double Rflux = Rin * 1000000.0 / (dayLength * hr2s) * (1.0 - _albedo);
                double Rint = 0.0;

                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentDataStruct componentData = ComponentData[j];

                    componentData.Gc[i] = CanopyConductance(componentData.Gsmax, componentData.R50, componentData.Frgr, componentData.Fgreen[i], layerKtot[i], layerLAIsum[i], Rflux);

                    Rint += componentData.Rs[i];
                }
                // Calculate Rin for the next layer down
                Rin -= Rint;
            }
        }
        /// <summary>
        /// Calculate the aerodynamic conductance for system compartments
        /// </summary>
        private void CalculateGa()
        {
            double windspeed = windspeed_default;
            if (!windspeed_checked)
            {
                object val = this.Get("windspeed");
                use_external_windspeed = val != null;
                if (use_external_windspeed)
                    windspeed = (double)val;
                windspeed_checked = true;
            }

            double sumDeltaZ = 0.0;
            double sumLAI = 0.0;
            for (int i = 0; i <= numLayers - 1; i++)
            {
                sumDeltaZ += DeltaZ[i];
                // top height
                // total lai
                sumLAI += layerLAIsum[i];
            }

            double totalGa = AerodynamicConductanceFAO(windspeed, refheight, sumDeltaZ, sumLAI);

            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentData[j].Ga[i] = totalGa * Utility.Math.Divide(ComponentData[j].Rs[i], sumRs, 0.0);
                }
            }
        }

        /// <summary>
        /// Calculate the interception loss of water from the canopy
        /// </summary>
        private void CalculateInterception()
        {
            double sumLAI = 0.0;
            double sumLAItot = 0.0;
            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    sumLAI += ComponentData[j].layerLAI[i];
                    sumLAItot += ComponentData[j].layerLAItot[i];
                }
            }

            double totalInterception = a_interception * Math.Pow(rain, b_interception) + c_interception * sumLAItot + d_interception;

            totalInterception = Math.Max(0.0, Math.Min(0.99 * rain, totalInterception));

            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentData[j].interception[i] = Utility.Math.Divide(ComponentData[j].layerLAI[i], sumLAI, 0.0) * totalInterception;
                }
            }
        }

        /// <summary>
        /// Calculate the Penman-Monteith water demand
        /// </summary>
        private void CalculatePM()
        {
            // zero a few things, and sum a few others
            double sumRl = 0.0;
            double sumRsoil = 0.0;
            double sumInterception = 0.0;
            double freeEvapGa = 0.0;
            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentDataStruct componentData = ComponentData[j];
                    componentData.PET[i] = 0.0;
                    componentData.PETr[i] = 0.0;
                    componentData.PETa[i] = 0.0;
                    sumRl += componentData.Rl[i];
                    sumRsoil += componentData.Rsoil[i];
                    sumInterception += componentData.interception[i];
                    freeEvapGa += componentData.Ga[i];
                }
            }

            double netRadiation = ((1.0 - _albedo) * sumRs + sumRl + sumRsoil) * 1000000.0;
            // MJ/J
            netRadiation = Math.Max(0.0, netRadiation);
            double freeEvapGc = freeEvapGa * 1000000.0;
            // =infinite surface conductance
            double freeEvap = CalcPenmanMonteith(netRadiation, mint, maxt, vp, air_pressure, dayLength, freeEvapGa, freeEvapGc);

            dryleaffraction = 1.0 - Utility.Math.Divide(sumInterception * (1.0 - night_interception_fraction), freeEvap, 0.0);
            dryleaffraction = Math.Max(0.0, dryleaffraction);

            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentDataStruct componentData = ComponentData[j];

                    netRadiation = 1000000.0 * ((1.0 - _albedo) * componentData.Rs[i] + componentData.Rl[i] + componentData.Rsoil[i]);
                    // MJ/J
                    netRadiation = Math.Max(0.0, netRadiation);

                    componentData.PETr[i] = CalcPETr(netRadiation * dryleaffraction, mint, maxt, air_pressure, componentData.Ga[i], componentData.Gc[i]);

                    componentData.PETa[i] = CalcPETa(mint, maxt, vp, air_pressure, dayLength * dryleaffraction, componentData.Ga[i], componentData.Gc[i]);

                    componentData.PET[i] = componentData.PETr[i] + componentData.PETa[i];
                }
            }
        }

        /// <summary>
        /// Calculate the aerodynamic decoupling for system compartments
        /// </summary>
        private void CalculateOmega()
        {
            for (int i = 0; i <= numLayers - 1; i++)
            {
                for (int j = 0; j <= ComponentData.Count - 1; j++)
                {
                    ComponentDataStruct componentData = ComponentData[j];

                    componentData.Omega[i] = CalcOmega(mint, maxt, air_pressure, componentData.Ga[i], componentData.Gc[i]);
                }
            }
        }

        /// <summary>
        /// Send an energy balance event
        /// </summary>
        private void SendEnergyBalanceEvent()
        {
            for (int j = 0; j <= ComponentData.Count - 1; j++)
            {
                ComponentDataStruct componentData = ComponentData[j];
                if (componentData.Crop != null)
                {
                    CanopyEnergyBalanceInterceptionlayerType[] lightProfile = new CanopyEnergyBalanceInterceptionlayerType[numLayers];
                    double totalPotentialEp = 0;
                    double totalInterception = 0.0;
                    for (int i = 0; i <= numLayers - 1; i++)
                    {
                        lightProfile[i] = new CanopyEnergyBalanceInterceptionlayerType();
                        lightProfile[i].thickness = Convert.ToSingle(DeltaZ[i]);
                        lightProfile[i].amount = Convert.ToSingle(componentData.Rs[i] * RadnGreenFraction(j));
                        totalPotentialEp += componentData.PET[i];
                        totalInterception += componentData.interception[i];
                    }

                    componentData.Crop.PotentialEP = totalPotentialEp;
                    componentData.Crop.LightProfile = lightProfile;
                }
            }
        }

    }


}