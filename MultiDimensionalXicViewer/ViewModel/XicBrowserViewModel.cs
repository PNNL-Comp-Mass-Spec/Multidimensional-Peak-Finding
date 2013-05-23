using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using InformedProteomics.Backend.Data.Sequence;
using MultiDimensionalPeakFinding;
using MultiDimensionalPeakFinding.PeakDetection;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using UIMFLibrary;
using Point = MultiDimensionalPeakFinding.PeakDetection.Point;

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

		private AminoAcidSet m_aminoAcidSet;

		public XicBrowserViewModel()
		{
			//this.XicPlot = new LinesVisual3D();
			this.XicPlotPoints = new List<Point3D>();
			this.FeatureList = new List<FeatureBlob>();
			m_aminoAcidSet = new AminoAcidSet(Modification.Carbamidomethylation);
		}

		public void OpenUimfFile(string fileName)
		{
			FileInfo uimfFileInfo = new FileInfo(fileName);

			this.UimfUtil = new UimfUtil(fileName);

			this.CurrentUimfFileName = uimfFileInfo.Name;
			OnPropertyChanged("CurrentUimfFileName");

			// TODO: Make sure that the m/z based table exists
		}

		public void FindFeatures()
		{
			var seqGraph = new SequenceGraph(m_aminoAcidSet, this.CurrentPeptide);
			var scoringGraph = seqGraph.GetScoringGraph(0);
			double mz = scoringGraph.GetPrecursorIon(this.CurrentChargeState).GetMz();

			List<IntensityPoint> uimfPointList = this.UimfUtil.GetXic(mz, this.CurrentTolerance, DataReader.FrameType.MS1, DataReader.ToleranceType.PPM);
			IEnumerable<Point> watershedPointList = WaterShedMapUtil.BuildWatershedMap(uimfPointList);

			SavitzkyGolaySmoother smoother = new SavitzkyGolaySmoother(11, 2);
			smoother.Smooth(ref watershedPointList);

			this.FeatureList = FeatureDetection.DoWatershedAlgorithm(watershedPointList).ToList();
			OnPropertyChanged("FeatureList");

			this.LcSlicePlot = new PlotModel();
			OnPropertyChanged("LcSlicePlot");

			this.ImsSlicePlot = new PlotModel();
			OnPropertyChanged("ImsSlicePlot");
		}

		public void CreateLcAndImsSlicePlots(FeatureBlob feature)
		{
			CreateLcSlicePlot(feature);
			CreateImsSlicePlot(feature);
		}

		private void CreateLcSlicePlot(FeatureBlob feature)
		{
			PlotModel plotModel = new PlotModel("LC Slice");
			plotModel.TitleFontSize = 12;
			plotModel.Padding = new OxyThickness(0);
			plotModel.PlotMargins = new OxyThickness(0);

			var lcSeries = new LineSeries(OxyColors.Blue);

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
			yAxis.IsPanEnabled = false;
			yAxis.IsZoomEnabled = false;

			var xAxis = new LinearAxis(AxisPosition.Bottom, "LC Scan #");
			xAxis.Minimum = minScanLc - 5;
			xAxis.AbsoluteMinimum = minScanLc - 5;
			xAxis.Maximum = maxScanLc + 5;
			xAxis.AbsoluteMaximum = maxScanLc + 5;
			xAxis.IsPanEnabled = false;
			xAxis.IsZoomEnabled = false;

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

			var imsSeries = new LineSeries(OxyColors.Blue);

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
			yAxis.IsPanEnabled = false;
			yAxis.IsZoomEnabled = false;

			var xAxis = new LinearAxis(AxisPosition.Bottom, "IMS Scan #");
			xAxis.Minimum = minScanIms - 5;
			xAxis.AbsoluteMinimum = minScanIms - 5;
			xAxis.Maximum = maxScanIms + 5;
			xAxis.AbsoluteMaximum = maxScanIms + 5;
			xAxis.IsPanEnabled = false;
			xAxis.IsZoomEnabled = false;

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
	}
}
