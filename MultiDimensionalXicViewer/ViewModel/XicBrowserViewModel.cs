﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using MultiDimensionalPeakFinding;
using MultiDimensionalPeakFinding.PeakDetection;
using Ookii.Dialogs.Wpf;
using OxyPlot;
using OxyPlot.Axes;
using UIMFLibrary;
using LineSeries = OxyPlot.Series.LineSeries;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using Rectangle = System.Drawing.Rectangle;

namespace MultiDimensionalXicViewer.ViewModel
{
    public sealed class XicBrowserViewModel : ViewModelBase
    {
        private readonly double BIN_CENTRIC_PROGRESS_START = 0.0001;

        public string CurrentPeptide { get; set; }
        public int CurrentChargeState { get; set; }
        public double CurrentTolerance { get; set; }

        public String CurrentUimfFileName { get; set; }

        public UimfUtil UimfUtil { get; set; }
        //public LinesVisual3D XicPlot { get; set; }
        //public Mesh3D XicMeshPlot { get; set; }
        public IList<Point3D> XicPlotPoints { get; set; }
        public PlotModel XicContourPlot { get; set; }
        public PlotModel LcSlicePlot { get; set; }
        public PlotModel ImsSlicePlot { get; set; }
        public List<FeatureBlob> FeatureList { get; set; }
        public FeatureBlob CurrentFeature { get; set; }
        public Dictionary<Tuple<IonType, int>, List<FeatureBlob>> FragmentFeaturesDictionary { get; set; }
        public Dictionary<string, List<FeatureBlob>> IsotopeFeaturesDictionary { get; set; }

        public List<int> FragmentChargeStateList { get; set; }
        public List<NeutralLoss> FragmentNeutralLossList { get; set; }
        public List<string> FragmentIonList { get; set; }

        private double mBinCentricTableProgress;
        public string BinCentricTableProgress
        {
            get
            {
                if (mBinCentricTableProgress < BIN_CENTRIC_PROGRESS_START)
                    return string.Empty;

                if (Math.Abs(mBinCentricTableProgress - BIN_CENTRIC_PROGRESS_START) < float.Epsilon)
                    return "Adding bin-centric data: duplicating original .UIMF file";

                if (mBinCentricTableProgress > 99.999)
                {
                    return "Data loaded";
                }

                return "Adding bin-centric data: " + mBinCentricTableProgress.ToString("0.0") + "% complete";
            }
        }

        public ProgressDialog FeatureFindingProgressDialog { get; set; }

        private readonly AminoAcidSet m_aminoAcidSet;
        private readonly IonTypeFactory m_ionTypeFactory;

        private BackgroundWorker m_FeatureFinderBackgroundWorker;

        private double m_maxLcIntensity;
        private double m_maxImsIntensity;

        public XicBrowserViewModel()
        {
            //this.XicPlot = new LinesVisual3D();
            XicPlotPoints = new List<Point3D>();
            FeatureList = new List<FeatureBlob>();
            FragmentFeaturesDictionary = new Dictionary<Tuple<IonType, int>, List<FeatureBlob>>();
            FragmentChargeStateList = new List<int>();
            FragmentNeutralLossList = new List<NeutralLoss> { NeutralLoss.NoLoss };
            FragmentIonList = new List<string>();
            IsotopeFeaturesDictionary = new Dictionary<string, List<FeatureBlob>>();

            m_aminoAcidSet = new AminoAcidSet(Modification.Carbamidomethylation);
            m_ionTypeFactory = new IonTypeFactory(
                new[] { BaseIonType.B, BaseIonType.Y, BaseIonType.A },
                new[] { NeutralLoss.NoLoss, NeutralLoss.H2O, NeutralLoss.NH3 },
                maxCharge: 3);
        }

        public async void OpenUimfFile(string filePath)
        {
            var uimfFileInfo = new FileInfo(filePath);
            UpdateBinCentricTableProgress(0);

            UimfUtil = new UimfUtil(filePath);

            if(!UimfUtil.DoesContainBinCentricData())
            {
                //IContainer components = new System.ComponentModel.Container();

                var taskDialog = new TaskDialog
                {
                    WindowTitle = @"Bin Centric Data Creation",
                    Content = @"Add the bin-centric data table now?",
                    MainInstruction = @"Bin-centric data is required for this software to work",
                    AllowDialogCancellation = true
                };

                var yesButton = new TaskDialogButton
                {
                    ButtonType = ButtonType.Yes,
                    Default = true
                };

                var noButton = new TaskDialogButton
                {
                    ButtonType = ButtonType.No,
                    Default = true
                };

                taskDialog.Buttons.Add(noButton);
                taskDialog.Buttons.Add(yesButton);

                var eResult = taskDialog.ShowDialog();

                if (eResult != yesButton)
                    return;

                var result = await InsertBinCentricAsync(filePath);

                MessageBox.Show("Bin centric tables added", "Process Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            }

            CurrentUimfFileName = uimfFileInfo.Name;
            OnPropertyChanged("CurrentUimfFileName");
        }

        private async Task<int> InsertBinCentricAsync(string filePath)
        {
            try
            {
                UpdateBinCentricTableProgress(BIN_CENTRIC_PROGRESS_START);

                await Task.Run(() => AddBinCentricTables(filePath));
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in InsertBinCentricAsync: " + ex.Message);
                return 1;
            }
        }

        private void AddBinCentricTables(string filePath)
        {
            var sourceFile = new FileInfo(filePath);

            using (var uimfReader = new DataReader(sourceFile.FullName))
            {
                // Note: providing true for parseViaFramework as a workaround for reading SqLite files located on a remote UNC share or in readonly folders
                var connectionString = "Data Source = " + sourceFile.FullName;
                using (var dbConnection = new System.Data.SQLite.SQLiteConnection(connectionString, true))
                {
                    dbConnection.Open();

                    /*
                    // Start a transaction
                    using (var dbCommand = dbConnection.CreateCommand())
                    {
                        dbCommand.CommandText = "PRAGMA synchronous=0;BEGIN TRANSACTION;";
                        dbCommand.ExecuteNonQuery();
                    }
                    */

                    var binCentricTableCreator = new BinCentricTableCreation();

                    var workingDirectory = sourceFile.DirectoryName;

                    // Attach the events
                    binCentricTableCreator.Message += binCentricTableCreator_OnMessage;
                    binCentricTableCreator.OnProgress += binCentricTableCreator_OnProgress;

                    binCentricTableCreator.CreateBinCentricTable(dbConnection, uimfReader, workingDirectory);

                    /*
                    // Finalize the transaction
                    using (var dbCommand = dbConnection.CreateCommand())
                    {
                        dbCommand.CommandText = "END TRANSACTION;PRAGMA synchronous=1;";
                        dbCommand.ExecuteNonQuery();
                    }
                    */

                    dbConnection.Close();
                }
            }

            UpdateBinCentricTableProgress(100);
        }

        private void binCentricTableCreator_OnMessage(object sender, MessageEventArgs e)
        {
            Console.WriteLine(e.Message);
            //mBinCentricCreationMessage = e.Message;
            //OnPropertyChanged("BinCentricCreationMessage");
        }

        private void binCentricTableCreator_OnProgress(object sender, ProgressEventArgs e)
        {
            UpdateBinCentricTableProgress(e.PercentComplete);
        }

        /// <summary>
        /// Percent complete; value between 0 and 100
        /// </summary>
        /// <param name="newProgress"></param>
        private void UpdateBinCentricTableProgress(double newProgress)
        {
            mBinCentricTableProgress = newProgress;
            OnPropertyChanged("BinCentricTableProgress");
        }

        public void OnFindFeatures()
        {
            FeatureFindingProgressDialog = new ProgressDialog
            {
                WindowTitle = @"Feature Finding Progress",
                Description = @"Feature Finding Progress",
                ShowTimeRemaining = true,
                ShowCancelButton = false
            };


            FeatureFindingProgressDialog.DoWork += ProgressDialogOnDoWork;

            FeatureFindingProgressDialog.ShowDialog();
        }

        public void CreateLcAndImsSlicePlots(FeatureBlob feature)
        {
            CurrentFeature = feature;
            OnPropertyChanged("CurrentFeature");

            if(feature != null)
            {
                CreateLcSlicePlot(feature);
                CreateImsSlicePlot(feature);

                MatchPrecursorToFragments();

                AddIsotopesToPlots();

                OnPropertyChanged("LcSlicePlot");
                OnPropertyChanged("ImsSlicePlot");
            }
        }

        private void AddIsotopesToPlots()
        {
            if (CurrentFeature == null) return;

            var precursor = new Feature(CurrentFeature.Statistics);
            var precursorBoundary = precursor.GetBoundary();

            foreach (var isotopeEntry in IsotopeFeaturesDictionary)
            {
                var isotopeName = isotopeEntry.Key;

                foreach (var featureBlob in isotopeEntry.Value)
                {
                    var feature = new Feature(featureBlob.Statistics);

                    var boundary = feature.GetBoundary();
                    var intersection = Rectangle.Intersect(precursorBoundary, boundary);

                    // Ignore features that do not intersect at all
                    if (intersection.IsEmpty) continue;

                    AddToLcPlot(featureBlob, isotopeName, OxyColors.DeepSkyBlue);
                    AddToImsPlot(featureBlob, isotopeName, OxyColors.DeepSkyBlue);
                }
            }
        }

        private void MatchPrecursorToFragments()
        {
            if (CurrentFeature == null) return;

            var precursor = new Feature(CurrentFeature.Statistics);
            var precursorBoundary = precursor.GetBoundary();

            foreach (var kvp in FragmentFeaturesDictionary)
            {
                var ionTypeTuple = kvp.Key;

                // Skip any fragments that do not meet the UI filter criteria
                if (!ShouldShowFragment(ionTypeTuple)) continue;

                var residueNumber = ionTypeTuple.Item2;
                var fragmentName = ionTypeTuple.Item1.GetName(residueNumber);
                var fragmentFeatureList = kvp.Value;

                foreach (var fragmentFeature in fragmentFeatureList)
                {
                    var feature = new Feature(fragmentFeature.Statistics);

                    var fragmentBoundary = feature.GetBoundary();
                    var intersection = Rectangle.Intersect(precursorBoundary, fragmentBoundary);

                    // Ignore fragment features that do not intersect at all
                    if (intersection.IsEmpty) continue;

                    AddToLcPlot(fragmentFeature, fragmentName, OxyColors.Red);
                    AddToImsPlot(fragmentFeature, fragmentName, OxyColors.Red);
                }
            }
        }

        private bool ShouldShowFragment(Tuple<IonType, int> ionTypeTuple)
        {
            var ionType = ionTypeTuple.Item1;

            // Check charge state
            var charge = ionType.Charge;
            if (!FragmentChargeStateList.Contains(charge)) return false;

            // Check specific ion (e.g. b3, a7, y1)
            var residueNumber = ionTypeTuple.Item2;
            var ionLetter = ionType.BaseIonType.Symbol.ToLower();
            var fragmentName = ionLetter + residueNumber;
            if (!FragmentIonList.Contains(fragmentName)) return false;

            // Check for neutral loss
            if(ionType.NeutralLoss != NeutralLoss.NoLoss)
            {
                if (!FragmentNeutralLossList.Contains(ionType.NeutralLoss)) return false;
            }

            // If all filters pass, return true
            return true;
        }

        private void AddToLcPlot(FeatureBlob feature, string title, OxyColor color)
        {
            // TODO: Use unique colors
            var newLcSeries = new LineSeries()
            {
                Color = color,
                StrokeThickness = 1,
                Title = title
            };

            newLcSeries.MouseDown += SeriesOnSelected;

            foreach (var group in feature.PointList.GroupBy(x => x.ScanLc).OrderBy(x => x.Key))
            {
                var scanLc = group.Key;
                var intensity = group.Sum(x => x.Intensity);

                var dataPoint = new DataPoint(scanLc, intensity / m_maxLcIntensity);
                newLcSeries.Points.Add(dataPoint);
            }

            LcSlicePlot.Series.Add(newLcSeries);
        }

        private void AddToImsPlot(FeatureBlob feature, string title, OxyColor color)
        {
            // TODO: Use unique colors
            var newImsSeries = new LineSeries()
            {
                Color = color,
                StrokeThickness = 1,
                Title = title
            };
            newImsSeries.MouseDown += SeriesOnSelected;

            foreach (var group in feature.PointList.GroupBy(x => x.ScanIms).OrderBy(x => x.Key))
            {
                var scanLc = group.Key;
                var intensity = group.Sum(x => x.Intensity);

                var dataPoint = new DataPoint(scanLc, intensity / m_maxImsIntensity);
                newImsSeries.Points.Add(dataPoint);
            }

            ImsSlicePlot.Series.Add(newImsSeries);
        }

        private void CreateLcSlicePlot(FeatureBlob feature)
        {
            var plotModel = new PlotModel
            {
                Title = "LC Slice",
                TitleFontSize = 12,
                Padding = new OxyThickness(0),
                PlotMargins = new OxyThickness(0),
                IsLegendVisible = false
            };

            var lcSeries = new LineSeries {Color = OxyColors.Blue};
            lcSeries.MouseDown += SeriesOnSelected;

            var minScanLc = int.MaxValue;
            var maxScanLc = int.MinValue;
            var maxIntensity = double.MinValue;

            // Find the maximum intensity so that we can scale the Y values to be between 0 and 1
            foreach (var group in feature.PointList.GroupBy(x => x.ScanLc).OrderBy(x => x.Key))
            {
                var intensity = group.Sum(x => x.Intensity);
                if (intensity > maxIntensity) maxIntensity = intensity;
            }

            foreach (var group in feature.PointList.GroupBy(x => x.ScanLc).OrderBy(x => x.Key))
            {
                var scanLc = group.Key;
                var intensity = group.Sum(x => x.Intensity);

                if (scanLc < minScanLc) minScanLc = scanLc;
                if (scanLc > maxScanLc) maxScanLc = scanLc;

                var dataPoint = new DataPoint(scanLc, intensity / maxIntensity);
                lcSeries.Points.Add(dataPoint);
            }

            plotModel.Series.Add(lcSeries);

            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Relative Intensity",
                Minimum = 0,
                AbsoluteMinimum = 0,
                Maximum = 1.01,
                AbsoluteMaximum = 1.01,
                IsPanEnabled = true,
                IsZoomEnabled = true
            };
            yAxis.AxisChanged += OnYAxisChange;

            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "LC Scan #",
                Minimum = minScanLc - 5,
                AbsoluteMinimum = minScanLc - 5,
                Maximum = maxScanLc + 5,
                AbsoluteMaximum = maxScanLc + 5,
                IsPanEnabled = true,
                IsZoomEnabled = true
            };

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

            m_maxLcIntensity = maxIntensity;
            LcSlicePlot = plotModel;
            OnPropertyChanged("LcSlicePlot");
        }

        private void CreateImsSlicePlot(FeatureBlob feature)
        {
            var plotModel = new PlotModel
            {
                Title = "IMS Slice",
                TitleFontSize = 12,
                Padding = new OxyThickness(0),
                PlotMargins = new OxyThickness(0),
                IsLegendVisible = false
            };

            var imsSeries = new LineSeries {Color = OxyColors.Blue};
            imsSeries.MouseDown += SeriesOnSelected;

            var minScanIms = int.MaxValue;
            var maxScanIms = int.MinValue;
            var maxIntensity = double.MinValue;

            // Find the maximum intensity so that we can scale the Y values to be between 0 and 1
            foreach (var group in feature.PointList.GroupBy(x => x.ScanIms).OrderBy(x => x.Key))
            {
                var intensity = group.Sum(x => x.Intensity);
                if (intensity > maxIntensity) maxIntensity = intensity;
            }

            foreach (var group in feature.PointList.GroupBy(x => x.ScanIms).OrderBy(x => x.Key))
            {
                var scanIms = group.Key;
                var intensity = group.Sum(x => x.Intensity);

                if (scanIms < minScanIms) minScanIms = scanIms;
                if (scanIms > maxScanIms) maxScanIms = scanIms;

                var dataPoint = new DataPoint(scanIms, intensity / maxIntensity);
                imsSeries.Points.Add(dataPoint);
            }

            plotModel.Series.Add(imsSeries);

            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Relative Intensity",
                Minimum = 0,
                AbsoluteMinimum = 0,
                Maximum = 1.01,
                AbsoluteMaximum = 1.01,
                IsPanEnabled = true,
                IsZoomEnabled = true
            };
            yAxis.AxisChanged += OnYAxisChange;

            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "IMS Scan #",
                Minimum = minScanIms - 5,
                AbsoluteMinimum = minScanIms - 5,
                Maximum = maxScanIms + 5,
                AbsoluteMaximum = maxScanIms + 5,
                IsPanEnabled = true,
                IsZoomEnabled = true
            };

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

            m_maxImsIntensity = maxIntensity;
            ImsSlicePlot = plotModel;
            OnPropertyChanged("ImsSlicePlot");
        }

        //public void Create3dPlot(double mz, double tolerance)
        //{
        //    LinesVisual3D plot = new LinesVisual3D();

        //    plot.Thickness = 20;

        //    Console.WriteLine("mz = " + mz);
        //    Console.WriteLine("tolerance = " + tolerance);
        //    List<IntensityPoint> uimfPointList = this.UimfUtil.GetXic(mz, tolerance, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);

        //    Console.WriteLine("# points = " + uimfPointList.Count);

        //    foreach (var intensityPoint in uimfPointList)
        //    {
        //        Point3D point = new Point3D(intensityPoint.ScanLc, intensityPoint.ScanIms, intensityPoint.Intensity);
        //        plot.Points.Add(point);
        //    }

        //    this.XicPlotPoints = plot.Points;
        //    this.XicPlot = plot;

        //    //OnPropertyChanged("XicPlotPoints");
        //    //OnPropertyChanged("XicPlot");

        //    this.XicMeshPlot = new Mesh3D();
        //}

        //public void CreateContourPlot(double mz, double tolerance)
        //{
        //    PlotModel plotModel = new PlotModel("XIC Contour Plot");
        //    ContourSeries contourSeries = new ContourSeries();

        //    contourSeries.Data = this.UimfUtil.GetXicAsArray(mz, tolerance, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);
        //    contourSeries.ContourColors = new[] { OxyColors.Blue, OxyColors.Yellow, OxyColors.Red };
        //    contourSeries.RowCoordinates = ArrayHelper.CreateVector(0, 1, 2);
        //    contourSeries.ColumnCoordinates = ArrayHelper.CreateVector(0, 1, 2);

        //    plotModel.Series.Add(contourSeries);


        //    //Func<double, double, double> peaks = (x, y) =>
        //    //   3 * (1 - x) * (1 - x) * Math.Exp(-(x * x) - (y + 1) * (y + 1))
        //    //   - 10 * (x / 5 - x * x * x - y * y * y * y * y) * Math.Exp(-x * x - y * y)
        //    //   - 1.0 / 3 * Math.Exp(-(x + 1) * (x + 1) - y * y);

        //    //var cs = new ContourSeries
        //    //{
        //    //    ColumnCoordinates = ArrayHelper.CreateVector(-3, 3, 0.05),
        //    //    RowCoordinates = ArrayHelper.CreateVector(-3.1, 3.1, 0.05),
        //    //    ContourColors = new[] { OxyColors.SeaGreen, OxyColors.RoyalBlue, OxyColors.IndianRed }
        //    //};
        //    //cs.Data = ArrayHelper.Evaluate(peaks, cs.ColumnCoordinates, cs.RowCoordinates);
        //    //plotModel.Series.Add(cs);


        //    this.XicContourPlot = plotModel;
        //    OnPropertyChanged("XicContourPlot");
        //}

        public void FeatureSelectionChange(FeatureBlob featureBlob)
        {
            CurrentFeature = featureBlob;
            OnPropertyChanged("CurrentFeature");

            CreateLcAndImsSlicePlots(featureBlob);
        }

        private void OnYAxisChange(object sender, AxisChangedEventArgs e)
        {
            var yAxis = sender as LinearAxis;

            if (yAxis == null)
                return;

            // No need to update anything if the minimum is already <= 0
            if (yAxis.ActualMinimum <= 0) return;

            // Set the minimum to 0 and refresh the plot
            yAxis.Zoom(0, yAxis.ActualMaximum);
            yAxis.PlotModel.InvalidatePlot(true);
        }

        private void SeriesOnSelected(object sender, OxyMouseDownEventArgs eventArgs)
        {
            // Note: OxyMouseDownEventArgs replaces OxyMouseEventArgs

            var plot = sender as PlotModel;

            if (eventArgs.ChangedButton != OxyMouseButton.Left || plot == null)
            {
                return;
            }

            var selectedSeries = plot.GetSeriesFromPoint(eventArgs.Position, 10);
            if (selectedSeries != null)
            {
                var title = selectedSeries.Title;

                foreach (var seriesItem in ImsSlicePlot.Series.Concat(LcSlicePlot.Series))
                {
                    var series = (LineSeries)seriesItem;

                    if (series.Title == null)
                    {
                        series.Color = OxyColors.Blue;

                        // Make thick if precursor was selected
                        series.StrokeThickness = title == null ? 5 : 1;
                    }
                    else if (int.TryParse(series.Title, out _))
                    {
                        series.Color = OxyColors.DeepSkyBlue;
                        series.StrokeThickness = series.Title.Equals(title) ? 5 : 1;
                    }
                    else if (series.Title.Equals(title))
                    {
                        series.Color = OxyColors.Green;
                        series.StrokeThickness = 5;
                    }
                    else
                    {
                        series.Color = OxyColors.Red;
                        series.StrokeThickness = 1;
                    }
                }

                ImsSlicePlot.InvalidatePlot(true);
                LcSlicePlot.InvalidatePlot(true);
            }
        }

        private void ProgressDialogOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            m_FeatureFinderBackgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = false
            };
            m_FeatureFinderBackgroundWorker.ProgressChanged += BackgroundWorkerOnProgressChanged;

            FindFeatures();
        }

        private void FindFeatures()
        {
            m_FeatureFinderBackgroundWorker.ReportProgress(0, "Finding 3-D Features for Precursor and Fragments");

            var seqGraph = SequenceGraph.CreateGraph(m_aminoAcidSet, CurrentPeptide);
            // var scoringGraph = seqGraph.GetScoringGraph(0);
            // var precursorIon = scoringGraph.GetPrecursorIon(this.CurrentChargeState);
            // var monoMz = precursorIon.GetMz();

            var sequence = new Sequence(CurrentPeptide, m_aminoAcidSet);
            var precursorIon = sequence.GetPrecursorIon(CurrentChargeState);
            var monoMz = precursorIon.GetMonoIsotopicMz();

            var uimfPointList = UimfUtil.GetXic(monoMz, CurrentTolerance, UIMFData.FrameType.MS1, DataReader.ToleranceType.PPM);
            var watershedPointList = WaterShedMapUtil.BuildWatershedMap(uimfPointList);

            var smoother = new SavitzkyGolaySmoother(11, 2);
            smoother.Smooth(ref watershedPointList);

            FeatureList = FeatureDetection.DoWatershedAlgorithm(watershedPointList).ToList();

            IsotopeFeaturesDictionary.Clear();
            var precursorTargetList = CurrentChargeState == 2 ? new List<string> { "-1", "0.5", "1", "1.5", "2", "3" } : new List<string> { "-1", "1", "2", "3" };
            foreach (var precursorTarget in precursorTargetList)
            {
                var targetMz = precursorIon.GetIsotopeMz(double.Parse(precursorTarget));

                var isotopeUimfPointList = UimfUtil.GetXic(targetMz, CurrentTolerance, UIMFData.FrameType.MS1, DataReader.ToleranceType.PPM);
                var isotopeWatershedPointList = WaterShedMapUtil.BuildWatershedMap(isotopeUimfPointList);

                var isotopeFeatures = FeatureDetection.DoWatershedAlgorithm(isotopeWatershedPointList).ToList();
                IsotopeFeaturesDictionary.Add(precursorTarget, isotopeFeatures);
            }

            LcSlicePlot = new PlotModel();
            ImsSlicePlot = new PlotModel();

            FragmentFeaturesDictionary.Clear();
            // var sequence = new Sequence(this.CurrentPeptide, m_aminoAcidSet);
            var ionTypeDictionary = sequence.GetProductIons(m_ionTypeFactory.GetAllKnownIonTypes());

            double fragmentCount = ionTypeDictionary.Count;
            var index = 0;
            foreach (var ionTypeKvp in ionTypeDictionary)
            {
                var ionTypeTuple = ionTypeKvp.Key;

                var ion = ionTypeKvp.Value;
                var fragmentMz = ion.GetMonoIsotopicMz();

                uimfPointList = UimfUtil.GetXic(fragmentMz, CurrentTolerance, UIMFData.FrameType.MS2, DataReader.ToleranceType.PPM);
                watershedPointList = WaterShedMapUtil.BuildWatershedMap(uimfPointList);
                smoother.Smooth(ref watershedPointList);

                var fragmentFeatureBlobList = FeatureDetection.DoWatershedAlgorithm(watershedPointList).ToList();
                FragmentFeaturesDictionary.Add(ionTypeTuple, fragmentFeatureBlobList);

                index++;
                var progress = (int)((index / fragmentCount) * 100);
                m_FeatureFinderBackgroundWorker.ReportProgress(progress);
            }

            OnPropertyChanged("FeatureList");
            OnPropertyChanged("LcSlicePlot");
            OnPropertyChanged("ImsSlicePlot");
        }

        private void BackgroundWorkerOnProgressChanged(object sender, ProgressChangedEventArgs progressChangedEventArgs)
        {
            var displayString = progressChangedEventArgs.UserState != null ? progressChangedEventArgs.UserState.ToString() : "";
            if (!displayString.Equals(""))
            {
                FeatureFindingProgressDialog.ReportProgress(progressChangedEventArgs.ProgressPercentage, displayString, "Processing: " + progressChangedEventArgs.ProgressPercentage + "%");
            }
            else
            {
                FeatureFindingProgressDialog.ReportProgress(progressChangedEventArgs.ProgressPercentage, null, "Processing: " + progressChangedEventArgs.ProgressPercentage + "%");
            }
        }
    }
}
