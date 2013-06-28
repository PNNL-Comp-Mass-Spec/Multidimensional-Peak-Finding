using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using MultiDimensionalPeakFinding;
using MultiDimensionalPeakFinding.PeakDetection;
using Ookii.Dialogs;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Wpf;
using UIMFLibrary;
using Point = MultiDimensionalPeakFinding.PeakDetection.Point;
using Feature = InformedProteomics.Backend.IMS.Feature;
using LineSeries = OxyPlot.Series.LineSeries;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using Rectangle = System.Drawing.Rectangle;

namespace MultiDimensionalXicViewer.ViewModel
{
	public sealed class XicBrowserViewModel : ViewModelBase
	{
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

		public ProgressDialog FeatureFindingProgressDialog { get; set; }

		private AminoAcidSet m_aminoAcidSet;
		private IonTypeFactory m_ionTypeFactory;
		private BackgroundWorker m_backgroundWorker;

		public XicBrowserViewModel()
		{
			//this.XicPlot = new LinesVisual3D();
			this.XicPlotPoints = new List<Point3D>();
			this.FeatureList = new List<FeatureBlob>();
			this.FragmentFeaturesDictionary = new Dictionary<Tuple<IonType, int>, List<FeatureBlob>>();
			this.FragmentChargeStateList = new List<int>();
			this.FragmentNeutralLossList = new List<NeutralLoss> { NeutralLoss.NoLoss };
			this.FragmentIonList = new List<string>();
			this.IsotopeFeaturesDictionary = new Dictionary<string, List<FeatureBlob>>();

			m_aminoAcidSet = new AminoAcidSet(Modification.Carbamidomethylation);
			m_ionTypeFactory = new IonTypeFactory(
				new[] { BaseIonType.B, BaseIonType.Y, BaseIonType.A },
				new[] { NeutralLoss.NoLoss, NeutralLoss.H2O, NeutralLoss.NH3 },
				maxCharge: 3);
		}

		public void OpenUimfFile(string fileName)
		{
			FileInfo uimfFileInfo = new FileInfo(fileName);

			this.UimfUtil = new UimfUtil(fileName);

			// TODO: Make sure that the m/z based table exists
			if(!this.UimfUtil.DoesContainBinCentricData())
			{
				//IContainer components = new System.ComponentModel.Container();

				TaskDialog taskDialog = new TaskDialog();
				taskDialog.WindowTitle = "Bin Centric Data Creation";
				taskDialog.Content = "Content";
				taskDialog.MainInstruction = "Main Instruction";
				taskDialog.AllowDialogCancellation = true;

				TaskDialogButton yesButton = new TaskDialogButton();
				yesButton.ButtonType = ButtonType.Yes;

				TaskDialogButton noButton = new TaskDialogButton();
				noButton.ButtonType = ButtonType.No;

				taskDialog.Buttons.Add(noButton);
				taskDialog.Buttons.Add(yesButton);

				taskDialog.ShowDialog();
			}

			this.CurrentUimfFileName = uimfFileInfo.Name;
			OnPropertyChanged("CurrentUimfFileName");
		}

		public void OnFindFeatures()
		{
			this.FeatureFindingProgressDialog = new ProgressDialog();

			this.FeatureFindingProgressDialog.WindowTitle = "Feature Finding Progress";
			this.FeatureFindingProgressDialog.Description = "Feature Finding Progress";
			this.FeatureFindingProgressDialog.ShowTimeRemaining = true;
			this.FeatureFindingProgressDialog.ShowCancelButton = false;

			this.FeatureFindingProgressDialog.DoWork += ProgressDialogOnDoWork;

			this.FeatureFindingProgressDialog.ShowDialog();
		}

		public void CreateLcAndImsSlicePlots(FeatureBlob feature)
		{
			this.CurrentFeature = feature;
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
			if (this.CurrentFeature == null) return;

			Feature precursor = new Feature(this.CurrentFeature.Statistics);
			Rectangle precursorBoundary = precursor.GetBoundary();

			foreach (var isotopeEntry in this.IsotopeFeaturesDictionary)
			{
				string isotopeName = isotopeEntry.Key;

				foreach (var featureBlob in isotopeEntry.Value)
				{
					var feature = new Feature(featureBlob.Statistics);

					Rectangle boundary = feature.GetBoundary();
					Rectangle intersection = Rectangle.Intersect(precursorBoundary, boundary);

					// Ignore features that do not intersect at all
					if (intersection.IsEmpty) continue;

					AddToLcPlot(featureBlob, isotopeName, OxyColors.DeepSkyBlue);
					AddToImsPlot(featureBlob, isotopeName, OxyColors.DeepSkyBlue);
				}
			}
		}

		private void MatchPrecursorToFragments()
		{
			if (this.CurrentFeature == null) return;

			Feature precursor = new Feature(this.CurrentFeature.Statistics);
			Rectangle precursorBoundary = precursor.GetBoundary();

			foreach (var kvp in this.FragmentFeaturesDictionary)
			{
				Tuple<IonType, int> ionTypeTuple = kvp.Key;

				// Skip any fragments that do not meet the UI filter criteria
				if (!ShouldShowFragment(ionTypeTuple)) continue;

				int residueNumber = ionTypeTuple.Item2;
				string fragmentName = ionTypeTuple.Item1.GetName(residueNumber);
				List<FeatureBlob> fragmentFeatureList = kvp.Value;

				foreach (var fragmentFeature in fragmentFeatureList)
				{
					var feature = new Feature(fragmentFeature.Statistics);

					Rectangle fragmentBoundary = feature.GetBoundary();
					Rectangle intersection = Rectangle.Intersect(precursorBoundary, fragmentBoundary);

					// Ignore fragment features that do not intersect at all
					if (intersection.IsEmpty) continue;

					AddToLcPlot(fragmentFeature, fragmentName, OxyColors.Red);
					AddToImsPlot(fragmentFeature, fragmentName, OxyColors.Red);
				}
			}
		}

		private bool ShouldShowFragment(Tuple<IonType, int> ionTypeTuple)
		{
			IonType ionType = ionTypeTuple.Item1;

			// Check charge state
			int charge = ionType.Charge;
			if (!this.FragmentChargeStateList.Contains(charge)) return false;

			// Check specific ion (e.g. b3, a7, y1)
			int residueNumber = ionTypeTuple.Item2;
			string ionLetter = ionType.BaseIonType.Symbol.ToLower();
			string fragmentName = ionLetter + residueNumber;
			if (!this.FragmentIonList.Contains(fragmentName)) return false;

			// Check for neutral loss
			if(ionType.NeutralLoss != NeutralLoss.NoLoss)
			{
				if (!this.FragmentNeutralLossList.Contains(ionType.NeutralLoss)) return false;
			}

			// If all filters pass, return true
			return true;
		}

		private void AddToLcPlot(FeatureBlob feature, string title, OxyColor color)
		{
			// TODO: Use unique colors
			var newLcSeries = new LineSeries(color, 1, title);
			newLcSeries.MouseDown += SeriesOnSelected;

			foreach (var group in feature.PointList.GroupBy(x => x.ScanLc).OrderBy(x => x.Key))
			{
				int scanLc = group.Key;
				double intensity = group.Sum(x => x.Intensity);

				DataPoint dataPoint = new DataPoint(scanLc, intensity);
				newLcSeries.Points.Add(dataPoint);
			}

			this.LcSlicePlot.Series.Add(newLcSeries);
		}

		private void AddToImsPlot(FeatureBlob feature, string title, OxyColor color)
		{
			// TODO: Use unique colors
			var newImsSeries = new LineSeries(color, 1, title);
			newImsSeries.MouseDown += SeriesOnSelected;

			foreach (var group in feature.PointList.GroupBy(x => x.ScanIms).OrderBy(x => x.Key))
			{
				int scanLc = group.Key;
				double intensity = group.Sum(x => x.Intensity);

				DataPoint dataPoint = new DataPoint(scanLc, intensity);
				newImsSeries.Points.Add(dataPoint);
			}

			this.ImsSlicePlot.Series.Add(newImsSeries);
		}

		private void CreateLcSlicePlot(FeatureBlob feature)
		{
			PlotModel plotModel = new PlotModel("LC Slice");
			plotModel.TitleFontSize = 12;
			plotModel.Padding = new OxyThickness(0);
			plotModel.PlotMargins = new OxyThickness(0);
			plotModel.IsLegendVisible = false;

			var lcSeries = new LineSeries(OxyColors.Blue);
			lcSeries.MouseDown += SeriesOnSelected;

			int minScanLc = int.MaxValue;
			int maxScanLc = int.MinValue;
			double maxIntensity = double.MinValue;

			foreach (var group in feature.PointList.GroupBy(x => x.ScanLc).OrderBy(x => x.Key))
			{
				int scanLc = group.Key;
				double intensity = group.Sum(x => x.Intensity);

				if (scanLc < minScanLc) minScanLc = scanLc;
				if (scanLc > maxScanLc) maxScanLc = scanLc;
				if (intensity > maxIntensity) maxIntensity = intensity;

				DataPoint dataPoint = new DataPoint(scanLc, intensity);
				lcSeries.Points.Add(dataPoint);
			}

			plotModel.Series.Add(lcSeries);

			var yAxis = new LinearAxis(AxisPosition.Left, "Intensity");
			yAxis.Minimum = 0;
			yAxis.AbsoluteMinimum = 0;
			yAxis.Maximum = maxIntensity + (maxIntensity * .05);
			yAxis.AbsoluteMaximum = maxIntensity + (maxIntensity * .05);
			yAxis.IsPanEnabled = true;
			yAxis.IsZoomEnabled = true;
			yAxis.AxisChanged += OnYAxisChange;

			var xAxis = new LinearAxis(AxisPosition.Bottom, "LC Scan #");
			xAxis.Minimum = minScanLc - 5;
			xAxis.AbsoluteMinimum = minScanLc - 5;
			xAxis.Maximum = maxScanLc + 5;
			xAxis.AbsoluteMaximum = maxScanLc + 5;
			xAxis.IsPanEnabled = true;
			xAxis.IsZoomEnabled = true;

			plotModel.Axes.Add(xAxis);
			plotModel.Axes.Add(yAxis);

			this.LcSlicePlot = plotModel;
			OnPropertyChanged("LcSlicePlot");
		}

		private void CreateImsSlicePlot(FeatureBlob feature)
		{
			PlotModel plotModel = new PlotModel("IMS Slice");
			plotModel.TitleFontSize = 12;
			plotModel.Padding = new OxyThickness(0);
			plotModel.PlotMargins = new OxyThickness(0);
			plotModel.IsLegendVisible = false;

			var imsSeries = new LineSeries(OxyColors.Blue);
			imsSeries.MouseDown += SeriesOnSelected;

			int minScanIms = int.MaxValue;
			int maxScanIms = int.MinValue;
			double maxIntensity = double.MinValue;

			foreach (var group in feature.PointList.GroupBy(x => x.ScanIms).OrderBy(x => x.Key))
			{
				int scanIms = group.Key;
				double intensity = group.Sum(x => x.Intensity);

				if (scanIms < minScanIms) minScanIms = scanIms;
				if (scanIms > maxScanIms) maxScanIms = scanIms;
				if (intensity > maxIntensity) maxIntensity = intensity;

				DataPoint dataPoint = new DataPoint(scanIms, intensity);
				imsSeries.Points.Add(dataPoint);
			}

			plotModel.Series.Add(imsSeries);

			var yAxis = new LinearAxis(AxisPosition.Left, "Intensity");
			yAxis.Minimum = 0;
			yAxis.AbsoluteMinimum = 0;
			yAxis.Maximum = maxIntensity + (maxIntensity * .05);
			yAxis.AbsoluteMaximum = maxIntensity + (maxIntensity * .05);
			yAxis.IsPanEnabled = true;
			yAxis.IsZoomEnabled = true;
			yAxis.AxisChanged += OnYAxisChange;

			var xAxis = new LinearAxis(AxisPosition.Bottom, "IMS Scan #");
			xAxis.Minimum = minScanIms - 5;
			xAxis.AbsoluteMinimum = minScanIms - 5;
			xAxis.Maximum = maxScanIms + 5;
			xAxis.AbsoluteMaximum = maxScanIms + 5;
			xAxis.IsPanEnabled = true;
			xAxis.IsZoomEnabled = true;

			plotModel.Axes.Add(xAxis);
			plotModel.Axes.Add(yAxis);

			this.ImsSlicePlot = plotModel;
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
			this.CurrentFeature = featureBlob;
			OnPropertyChanged("CurrentFeature");

			CreateLcAndImsSlicePlots(featureBlob);
		}

		private void OnYAxisChange(object sender, AxisChangedEventArgs e)
		{
			LinearAxis yAxis = sender as LinearAxis;

			// No need to update anything if the minimum is already <= 0
			if (yAxis.ActualMinimum <= 0) return;

			// Set the minimum to 0 and refresh the plot
			yAxis.Zoom(0, yAxis.ActualMaximum);
			yAxis.PlotModel.RefreshPlot(true);
		}

		private void SeriesOnSelected(object sender, OxyMouseEventArgs eventArgs)
		{
			Plot plot = sender as Plot;
			switch (eventArgs.ChangedButton)
			{
				case OxyMouseButton.Left:
					var selectedSeries = plot.GetSeriesFromPoint(eventArgs.Position, 10);
					if (selectedSeries != null)
					{
						string title = selectedSeries.Title;

						foreach (LineSeries series in this.ImsSlicePlot.Series.Concat(this.LcSlicePlot.Series))
						{
							int testInt = 0;

							if (series.Title == null)
							{
								series.Color = OxyColors.Blue;

								// Make thick if precursor was selected
								series.StrokeThickness = title == null ? 5 : 1;
							}
							else if (int.TryParse(series.Title, out testInt))
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

						this.ImsSlicePlot.RefreshPlot(true);
						this.LcSlicePlot.RefreshPlot(true);
					}
					break;
			}
		}

		private void ProgressDialogOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
		{
			m_backgroundWorker = new BackgroundWorker();
			m_backgroundWorker.WorkerReportsProgress = true;
			m_backgroundWorker.WorkerSupportsCancellation = false;
			m_backgroundWorker.ProgressChanged += BackgroundWorkerOnProgressChanged;

			FindFeatures();
		}

		private void FindFeatures()
		{
			m_backgroundWorker.ReportProgress(0, "Finding 3-D Features for Precursor and Fragments");

			var seqGraph = new SequenceGraph(m_aminoAcidSet, this.CurrentPeptide);
			var scoringGraph = seqGraph.GetScoringGraph(0);
			var precursorIon = scoringGraph.GetPrecursorIon(this.CurrentChargeState);
			double monoMz = precursorIon.GetMz();

			List<IntensityPoint> uimfPointList = this.UimfUtil.GetXic(monoMz, this.CurrentTolerance, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);
			IEnumerable<Point> watershedPointList = WaterShedMapUtil.BuildWatershedMap(uimfPointList);

			SavitzkyGolaySmoother smoother = new SavitzkyGolaySmoother(11, 2);
			smoother.Smooth(ref watershedPointList);

			this.FeatureList = FeatureDetection.DoWatershedAlgorithm(watershedPointList).ToList();

			this.IsotopeFeaturesDictionary.Clear();
			List<string> precursorTargetList = this.CurrentChargeState == 2 ? new List<string> { "-1", "0.5", "1", "1.5", "2", "3" } : new List<string> { "-1", "1", "2", "3" };
			foreach (var precursorTarget in precursorTargetList)
			{
				double targetMz = precursorIon.GetIsotopeMz(double.Parse(precursorTarget));

				List<IntensityPoint> isotopeUimfPointList = this.UimfUtil.GetXic(targetMz, this.CurrentTolerance, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);
				IEnumerable<Point> isotopeWatershedPointList = WaterShedMapUtil.BuildWatershedMap(isotopeUimfPointList);

				List<FeatureBlob> isotopeFeatures = FeatureDetection.DoWatershedAlgorithm(isotopeWatershedPointList).ToList();
				this.IsotopeFeaturesDictionary.Add(precursorTarget, isotopeFeatures);
			}
			
			this.LcSlicePlot = new PlotModel();
			this.ImsSlicePlot = new PlotModel();

			this.FragmentFeaturesDictionary.Clear();
			var sequence = new Sequence(this.CurrentPeptide, m_aminoAcidSet);
			var ionTypeDictionary = sequence.GetProductIons(m_ionTypeFactory.GetAllKnownIonTypes());

			double fragmentCount = ionTypeDictionary.Count;
			int index = 0;
			foreach (var ionTypeKvp in ionTypeDictionary)
			{
				Tuple<IonType, int> ionTypeTuple = ionTypeKvp.Key;

				var ion = ionTypeKvp.Value;
				double fragmentMz = ion.GetMz();

				uimfPointList = this.UimfUtil.GetXic(fragmentMz, this.CurrentTolerance, DataReader.FrameType.MS2, DataReader.ToleranceType.PPM);
				watershedPointList = WaterShedMapUtil.BuildWatershedMap(uimfPointList);
				smoother.Smooth(ref watershedPointList);

				var fragmentFeatureBlobList = FeatureDetection.DoWatershedAlgorithm(watershedPointList).ToList();
				this.FragmentFeaturesDictionary.Add(ionTypeTuple, fragmentFeatureBlobList);

				index++;
				int progress = (int)((index / fragmentCount) * 100);
				m_backgroundWorker.ReportProgress(progress);
			}

			OnPropertyChanged("FeatureList");
			OnPropertyChanged("LcSlicePlot");
			OnPropertyChanged("ImsSlicePlot");
		}

		private void BackgroundWorkerOnProgressChanged(object sender, ProgressChangedEventArgs progressChangedEventArgs)
		{
			string displayString = progressChangedEventArgs.UserState != null ? progressChangedEventArgs.UserState.ToString() : "";
			if (!displayString.Equals(""))
			{
				this.FeatureFindingProgressDialog.ReportProgress(progressChangedEventArgs.ProgressPercentage, displayString, "Processing: " + progressChangedEventArgs.ProgressPercentage + "%");
			}
			else
			{
				this.FeatureFindingProgressDialog.ReportProgress(progressChangedEventArgs.ProgressPercentage, null, "Processing: " + progressChangedEventArgs.ProgressPercentage + "%");
			}
		}
	}
}
