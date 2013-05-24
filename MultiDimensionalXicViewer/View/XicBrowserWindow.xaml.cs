using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MultiDimensionalPeakFinding;
using MultiDimensionalPeakFinding.PeakDetection;
using MultiDimensionalXicViewer.ViewModel;
using UIMFLibrary;

namespace MultiDimensionalXicViewer.View
{
	/// <summary>
	/// Interaction logic for XicBrowserWindow.xaml
	/// </summary>
	public partial class XicBrowserWindow : Window
	{
		public XicBrowserViewModel XicBrowserViewModel { get; set; }

		public XicBrowserWindow()
		{
			this.XicBrowserViewModel = new XicBrowserViewModel();
			
			InitializeComponent();

			this.DataContext = this.XicBrowserViewModel;
		}

		private void UimfOpenButtonClick(object sender, RoutedEventArgs e)
		{
			// Create OpenFileDialog and Set filter for file extension and default file extension
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog { DefaultExt = ".uimf", Filter = "UIMF Files (*.uimf)|*.uimf" };

			// Get the selected file name and display in a TextBox 
			if (dlg.ShowDialog() == true)
			{
				// Open file 
				string fileName = dlg.FileName;
				this.XicBrowserViewModel.OpenUimfFile(fileName);
			}
		}

		private void FindFeaturesButtonClick(object sender, RoutedEventArgs e)
		{
			string peptideSequence = peptideSelectedTextBox.Text;

			int chargeState = 2;
			int.TryParse(chargeSelectedTextBox.Text, out chargeState);

			int ppmTolerance = 50;
			int.TryParse(ppmToleranceSelectedTextBox.Text, out ppmTolerance);

			this.XicBrowserViewModel.CurrentPeptide = peptideSequence;
			this.XicBrowserViewModel.CurrentTolerance = ppmTolerance;
			this.XicBrowserViewModel.CurrentChargeState = chargeState;

			this.FeatureDataGrid.SelectedItem = null;

			this.XicBrowserViewModel.FindFeatures();
		}

		private void FeatureSelectionChange(object sender, SelectionChangedEventArgs e)
		{
			var selectedItem = (sender as DataGrid).SelectedItem;

			if (selectedItem != null && ReferenceEquals(selectedItem.GetType(), typeof(FeatureBlob)))
			{
				FeatureBlob featureBlob = (FeatureBlob) selectedItem;
				this.XicBrowserViewModel.CreateLcAndImsSlicePlots(featureBlob);
			}
		}

		//private void MsLevelChange(object sender, SelectionChangedEventArgs e)
		//{
		//    ComboBox comboBox = (ComboBox) sender;
		//    ComboBoxItem selectedItem = (ComboBoxItem)comboBox.SelectedItem;

		//    string frameType = (string)selectedItem.Content;

		//    this.XicBrowserViewModel.CurrentFrameType = (DataReader.FrameType)Enum.Parse(typeof(DataReader.FrameType), frameType);
		//}
		private void PeptideTextChanged(object sender, TextChangedEventArgs e)
		{
			string peptideString = (sender as TextBox).Text;
			int peptideLength = peptideString.Length;

			ionNumbersContainer.Children.Clear();

			for(int i = 1; i <= peptideLength; i++)
			{
				StackPanel stackPanel = new StackPanel
				                        	{
				                        		Name = "ionsNumber" + i + "Panel",
				                        		FlowDirection = FlowDirection.LeftToRight,
				                        		Orientation = Orientation.Horizontal
				                        	};

				CheckBox checkBox = new CheckBox();
				checkBox.Content = i.ToString();
				checkBox.Checked += CheckBoxOnChecked;
				checkBox.Unchecked += CheckBoxOnUnchecked;
				checkBox.Margin = i < 10 ? new Thickness(10, 2, 7, 2) : new Thickness(10, 2, 0, 2);

				CheckBox bCheckBox = new CheckBox();
				bCheckBox.Name = "b" + i + "Box";
				bCheckBox.Margin = new Thickness(10, 2, 21, 2);

				CheckBox yCheckBox = new CheckBox();
				yCheckBox.Name = "y" + i + "Box";
				yCheckBox.Margin = new Thickness(10, 2, 20, 2);

				CheckBox aCheckBox = new CheckBox();
				aCheckBox.Name = "a" + i + "Box";
				aCheckBox.Margin = new Thickness(10, 2, 20, 2);

				stackPanel.Children.Add(checkBox);
				stackPanel.Children.Add(bCheckBox);
				stackPanel.Children.Add(yCheckBox);
				stackPanel.Children.Add(aCheckBox);

				ionNumbersContainer.Children.Add(stackPanel);
			}
		}

		private void CheckBoxOnUnchecked(object sender, RoutedEventArgs routedEventArgs)
		{
			throw new NotImplementedException();
		}

		private void CheckBoxOnChecked(object sender, RoutedEventArgs routedEventArgs)
		{
			throw new NotImplementedException();
		}

		private void AddChargeState(object sender, RoutedEventArgs e)
		{
			CheckBox checkbox = sender as CheckBox;
			if(checkbox.Content.ToString().Contains('1'))
			{
				this.XicBrowserViewModel.FragmentChargeStateList.Add(1);
			}
			else if (checkbox.Content.ToString().Contains('2'))
			{
				this.XicBrowserViewModel.FragmentChargeStateList.Add(2);
			}
			else if (checkbox.Content.ToString().Contains('3'))
			{
				this.XicBrowserViewModel.FragmentChargeStateList.Add(3);
			}

			this.XicBrowserViewModel.CreateLcAndImsSlicePlots(this.XicBrowserViewModel.CurrentFeature);
		}

		private void RemoveChargeState(object sender, RoutedEventArgs e)
		{
			CheckBox checkbox = sender as CheckBox;
			if (checkbox.Content.ToString().Contains('1'))
			{
				this.XicBrowserViewModel.FragmentChargeStateList.Remove(1);
			}
			else if (checkbox.Content.ToString().Contains('2'))
			{
				this.XicBrowserViewModel.FragmentChargeStateList.Remove(2);
			}
			else if (checkbox.Content.ToString().Contains('3'))
			{
				this.XicBrowserViewModel.FragmentChargeStateList.Remove(3);
			}

			this.XicBrowserViewModel.CreateLcAndImsSlicePlots(this.XicBrowserViewModel.CurrentFeature);
		}
	}
}
