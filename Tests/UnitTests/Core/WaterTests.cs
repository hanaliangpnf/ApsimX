﻿using System;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Soils;
using NUnit.Framework;

namespace UnitTests.Core
{
    [TestFixture]
    public class WaterTests
    {
        /// <summary>
        /// Ensures changing InitialValues property changes all other related properties and variables.
        /// </summary>
        [Test]
        public void TestChangingInitialValues_ChangesAllOtherValues()
        {
            Water waterModel = GetWaterModel();
            waterModel.InitialValues[0] = 0.300;
            Assert.True(MathUtilities.FloatsAreEqual(waterModel.InitialPAWmm, 327.9207228741369, 1.0));
            Assert.True(MathUtilities.FloatsAreEqual(waterModel.FractionFull, 0.91, 0.1));
            Assert.True(MathUtilities.FloatsAreEqual(waterModel.DepthWetSoil, 1672.0, 1.0));
        }

        /// <summary>
        /// Ensures changing InitialPAWmm changes all other values.
        /// </summary>
        [Test]
        public void TestChangingInitialPAWmm_ChangesAllOtherValues()
        {
            Water waterModel = GetWaterModel();
            waterModel.InitialPAWmm = 160.0;
            double[] expectedInitialValues = new double[] {
                    0.376,
                    0.358,
                    0.372,
                    0.369,
                    0.365,
                    0.358,
                    0.356 };
            for (int i = 0; i < expectedInitialValues.Length; i++)
                Assert.True(MathUtilities.FloatsAreEqual(waterModel.InitialValues[i], expectedInitialValues[i], 0.001));
            Assert.True(MathUtilities.FloatsAreEqual(waterModel.FractionFull, 0.44, 0.01));
            Assert.True(MathUtilities.FloatsAreEqual(waterModel.DepthWetSoil, 797.24, 0.01));
        }

        /// <summary>
        /// Ensures chaning FractionFull changes all other values.
        /// </summary>
        [Test]
        public void TestChangingFractionFull_ChangesAllOtherValues()
        {
            Water waterModel = GetWaterModel();
            waterModel.FractionFull = 0.78;
            double[] expectedInitialValues = new double[] {
                0.464,
                0.442,
                0.442,
                0.436,
                0.43,
                0.418,
                0.414};
            for (int i = 0; i < expectedInitialValues.Length; i++)
                Assert.True(MathUtilities.FloatsAreEqual(waterModel.InitialValues[i], expectedInitialValues[i], 0.001));
            Assert.True(MathUtilities.FloatsAreEqual(waterModel.InitialPAWmm, 281.77143408393613));
            Assert.True(MathUtilities.FloatsAreEqual(waterModel.DepthWetSoil, 1404));
        }


        /// <summary>
        /// Ensures changing DepthWetSoil changes all other values.
        /// </summary>
        [Test]
        public void TestChangingDepthWetSoil_ChangesAllOtherValues()
        {
            Water waterModel = GetWaterModel();
            waterModel.DepthWetSoil = 300;
            double[] expectedInitialValues = new double[]{
                0.521,
                0.497,
                0.28,
                0.28,
                0.28,
                0.28,
                0.28
            };
            for (int i = 0; i < expectedInitialValues.Length; i++)
                Assert.True(MathUtilities.FloatsAreEqual(waterModel.InitialValues[i], expectedInitialValues[i], 0.001));
            Console.WriteLine(waterModel.InitialPAWmm);
            Assert.True(MathUtilities.FloatsAreEqual(waterModel.InitialPAWmm, 76.32927712586311));
            Console.WriteLine(waterModel.FractionFull);
            Assert.True(MathUtilities.FloatsAreEqual(waterModel.FractionFull, 0.21129479058703288));
        }

        /// <summary>
        /// Ensures changing FilledFromTop changes all other effected values.
        /// </summary>
        [Test]
        public void TestChangingFilledFromTop_ChangesAllEffectedValues()
        {
            Water waterModel = GetWaterModel();
            waterModel.FractionFull = 0.66;
            waterModel.FilledFromTop = true;
            double[] expectedInitialValues = new double[] {
                0.521,
                0.497,
                0.488,
                0.48,
                0.412,
                0.28,
                0.28
            };
            for (int i = 0; i < expectedInitialValues.Length; i++)
                Assert.True(MathUtilities.FloatsAreEqual(waterModel.InitialValues[i], expectedInitialValues[i], 0.001));
            Console.WriteLine(waterModel.DepthWetSoil);
            Assert.True(MathUtilities.FloatsAreEqual(waterModel.DepthWetSoil, 1106.0319002668557));
        }


        [Test]
        public void TestChangingRelativeTo_ChangesAllEffectedValues()
        {
            Water waterModel = GetWaterModel();
            waterModel.FractionFull = 0.66;
            waterModel.FilledFromTop = true;
            waterModel.RelativeTo = "Wheat";
            double[] expectedInitialValues = new double[] {
                0.521,
                0.497,
                0.488,
                0.384,
                0.36,
                0.392,
                0.446
            };
            for (int i = 0; i < expectedInitialValues.Length; i++)
            {
                Console.WriteLine(waterModel.InitialValues[i]);
                Assert.True(MathUtilities.FloatsAreEqual(waterModel.InitialValues[i], expectedInitialValues[i], 0.001));
            }
            Console.WriteLine(waterModel.InitialPAWmm);
            Console.WriteLine(waterModel.DepthWetSoil);
            Assert.True(MathUtilities.FloatsAreEqual(waterModel.InitialPAWmm, 162.0));
            Assert.True(MathUtilities.FloatsAreEqual(waterModel.DepthWetSoil, 734.0));
        }

        /// <summary>
        /// Creates a Water model for use in test cases. Based of a Wheat example file version 171.
        /// </summary>
        /// <returns>a <see cref="Water"/> model.</returns>
        public Water GetWaterModel()
        {
            // FractionFull is 1.0 
            Water waterModel = new Water()
            {

                Thickness = new double[7]{
                    150.0,
                    150.0,
                    300.0,
                    300.0,
                    300.0,
                    300.0,
                    300.0
                },
                InitialValues = new double[7] {
                    0.52100021807301,
                    0.496723476938497,
                    0.488437607673005,
                    0.480296969355493,
                    0.471583596524955,
                    0.457070570557793,
                    0.452331759845006
                },
                InitialPAWmm = 361.2454283127387,
                RelativeTo = "LL15",
                FilledFromTop = false,
                Name = "Water",
                ResourceName = null,
                Enabled = true,
                ReadOnly = false,
                Children = new List<IModel>()
                {
                    new Physical()
                    {
                        Name = "Physical",
                        Thickness = new double[7]{
                            150.0,
                            150.0,
                            300.0,
                            300.0,
                            300.0,
                            300.0,
                            300.0
                        },
                        BD = new double[]{
                            1.01056473311131,
                            1.07145631083388,
                            1.09393858528057,
                            1.15861335018721,
                            1.17301160318016,
                            1.16287303586874,
                            1.18749547755906
                        },
                        AirDry = new double[]{
                            0.130250054518252,
                            0.198689390775399,
                            0.28,
                            0.28,
                            0.28,
                            0.28,
                            0.28
                        },
                        LL15 = new double[]{
                            0.260500109036505,
                            0.248361738469248,
                            0.28,
                            0.28,
                            0.28,
                            0.28,
                            0.28
                        },
                        DUL = new double[]{
                            0.52100021807301,
                            0.496723476938497,
                            0.488437607673005,
                            0.480296969355493,
                            0.471583596524955,
                            0.457070570557793,
                            0.452331759845006
                        },
                        SAT = new double[]{
                            0.588654817693846,
                            0.565676863836273,
                            0.557192986686577,
                            0.532787415023694,
                            0.527354112007486,
                            0.531179986464627,
                            0.521888499034317
                        },
                        KS = new double[]{
                            20.0,
                            20.0,
                            20.0,
                            20.0,
                            20.0,
                            20.0,
                            20.0
                        },
                        Enabled = true,
                        ReadOnly = false,
                        Children = new List<IModel>()
                        {
                            new SoilCrop()
                            {
                                Name = "WheatSoil",
                                LL = new double[]{
                                    0.261,
                                    0.248,
                                    0.28,
                                    0.306,
                                    0.36,
                                    0.392,
                                    0.446
                                },
                                KL = new double[]{
                                    0.06,
                                    0.06,
                                    0.06,
                                    0.04,
                                    0.04,
                                    0.02,
                                    0.01
                                },
                                XF = new double[]{
                                    1.0,
                                    1.0,
                                    1.0,
                                    1.0,
                                    1.0,
                                    1.0,
                                    1.0
                                },
                                Enabled=true,
                                ReadOnly=false,
                            }
                        }
                    }
                }
            };
            return waterModel;
        }
    }
}
