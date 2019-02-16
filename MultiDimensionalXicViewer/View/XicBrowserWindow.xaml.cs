using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using InformedProteomics.Backend.Data.Spectrometry;
using MultiDimensionalPeakFinding.PeakDetection;
using MultiDimensionalXicViewer.ViewModel;
using Ookii.Dialogs.Wpf;

namespace MultiDimensionalXicViewer.View
{
    /// <summary>
    /// Interaction logic for XicBrowserWindow.xaml
    /// </summary>
    public partial class XicBrowserWindow : Window
    {
        public XicBrowserViewModel XicBrowserViewModel { get; set; }

        private List<CheckBox> m_bIonCheckBoxes;
        private List<CheckBox> m_yIonCheckBoxes;
        private List<CheckBox> m_aIonCheckBoxes;

        public XicBrowserWindow()
        {
            m_bIonCheckBoxes = new List<CheckBox>();
            m_yIonCheckBoxes = new List<CheckBox>();
            m_aIonCheckBoxes = new List<CheckBox>();

            XicBrowserViewModel = new XicBrowserViewModel();

            InitializeComponent();

            DataContext = XicBrowserViewModel;
        }

        private void UimfOpenButtonClick(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog and Set filter for file extension and default file extension
            var dialog = new VistaOpenFileDialog { DefaultExt = ".uimf", Filter = "UIMF Files (*.uimf)|*.uimf" };

            // Get the selected file name and display in a TextBox
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                // Open file
                var fileName = dialog.FileName;
                XicBrowserViewModel.OpenUimfFile(fileName);
            }
        }

        private void FindFeaturesButtonClick(object sender, RoutedEventArgs e)
        {
            var peptideSequence = peptideSelectedTextBox.Text;

            int.TryParse(chargeSelectedTextBox.Text, out var chargeState);

            int.TryParse(ppmToleranceSelectedTextBox.Text, out var ppmTolerance);

            XicBrowserViewModel.CurrentPeptide = peptideSequence;
            XicBrowserViewModel.CurrentTolerance = ppmTolerance;
            XicBrowserViewModel.CurrentChargeState = chargeState;

            FeatureDataGrid.SelectedItem = null;

            XicBrowserViewModel.OnFindFeatures();
        }

        private void FeatureSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = (sender as DataGrid)?.SelectedItem;

            if (selectedItem != null && ReferenceEquals(selectedItem.GetType(), typeof(FeatureBlob)))
            {
                var featureBlob = (FeatureBlob) selectedItem;
                XicBrowserViewModel.CreateLcAndImsSlicePlots(featureBlob);
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
            var peptideString = (sender as TextBox)?.Text;

            if (peptideString == null)
                return;

            var peptideLength = peptideString.Length;

            ionNumbersContainer.Children.Clear();
            m_bIonCheckBoxes.Clear();
            m_yIonCheckBoxes.Clear();
            m_aIonCheckBoxes.Clear();

            for(var i = 1; i <= peptideLength; i++)
            {
                var stackPanel = new StackPanel
                {
                    Name = "ionsNumber" + i + "Panel",
                    FlowDirection = FlowDirection.LeftToRight,
                    Orientation = Orientation.Horizontal
                };

                var checkBox = new CheckBox {Name = "AllBox" + i, Content = i.ToString()};
                checkBox.Checked += AllIonNumbersCheckBoxOnChecked;
                checkBox.Unchecked += AllIonNumbersCheckBoxOnUnchecked;
                checkBox.Margin = i < 10 ? new Thickness(10, 2, 7, 2) : new Thickness(10, 2, 0, 2);

                var bCheckBox = new CheckBox {Name = "b" + i + "_Box"};
                bCheckBox.Checked += IonCheckBoxOnChecked;
                bCheckBox.Unchecked += IonCheckBoxOnUnchecked;
                bCheckBox.Margin = i < 10 ? new Thickness(10, 3, 21, 2) : new Thickness(10, 4, 21, 2);
                if (bIonBox.IsChecked == true) bCheckBox.IsChecked = true;
                m_bIonCheckBoxes.Add(bCheckBox);

                var yCheckBox = new CheckBox {Name = "y" + i + "_Box"};
                yCheckBox.Checked += IonCheckBoxOnChecked;
                yCheckBox.Unchecked += IonCheckBoxOnUnchecked;
                yCheckBox.Margin = i < 10 ? new Thickness(10, 3, 21, 2) : new Thickness(10, 4, 21, 2);
                if (yIonBox.IsChecked == true) yCheckBox.IsChecked = true;
                m_yIonCheckBoxes.Add(yCheckBox);

                var aCheckBox = new CheckBox {Name = "a" + i + "_Box"};
                aCheckBox.Checked += IonCheckBoxOnChecked;
                aCheckBox.Unchecked += IonCheckBoxOnUnchecked;
                aCheckBox.Margin = i < 10 ? new Thickness(10, 3, 21, 2) : new Thickness(10, 4, 21, 2);
                if (aIonBox.IsChecked == true) aCheckBox.IsChecked = true;
                m_aIonCheckBoxes.Add(aCheckBox);

                stackPanel.Children.Add(checkBox);
                stackPanel.Children.Add(bCheckBox);
                stackPanel.Children.Add(yCheckBox);
                stackPanel.Children.Add(aCheckBox);

                ionNumbersContainer.Children.Add(stackPanel);
            }
        }

        private void IonCheckBoxOnChecked(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox))
                return;

            var fragmentIon = checkBox.Name.Split('_')[0];

            var fragmentIonList = XicBrowserViewModel.FragmentIonList;

            if (!fragmentIonList.Contains(fragmentIon))
            {
                fragmentIonList.Add(fragmentIon);

                RefreshPlots();
            }
        }

        private void IonCheckBoxOnUnchecked(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox))
                return;

            var fragmentIon = checkBox.Name.Split('_')[0];

            var fragmentIonList = XicBrowserViewModel.FragmentIonList;

            if (fragmentIonList.Remove(fragmentIon))
            {
                RefreshPlots();
            }
        }

        private void AllIonNumbersCheckBoxOnChecked(object sender, RoutedEventArgs routedEventArgs)
        {
            if (!(sender is CheckBox checkBox))
                return;

            var ionNumber = int.Parse(checkBox.Content.ToString());

            var fragmentIonList = XicBrowserViewModel.FragmentIonList;

            var possibleIonTypes = new List<string> { "b", "y", "a" };
            foreach (var ionType in possibleIonTypes)
            {
                var fragmentIon = ionType + ionNumber;
                if(!fragmentIonList.Contains(fragmentIon))
                {
                    fragmentIonList.Add(fragmentIon);
                }
            }

            var parent = (StackPanel)checkBox.Parent;
            foreach (var child in parent.Children)
            {
                ((CheckBox) child).IsChecked = true;
            }

            //List<string> possibleIonTypes = new List<string> {"b", "y", "a"};
            //List<int> possibleChargeStates = this.XicBrowserViewModel.FragmentChargeStateList;
            //List<NeutralLoss> possibleNeutralLossList = this.XicBrowserViewModel.FragmentNeutralLossList;

            //List<string> fragmentIonList = this.XicBrowserViewModel.FragmentIonList;

            //foreach (var ionType in possibleIonTypes)
            //{
            //    foreach (var chargeState in possibleChargeStates)
            //    {
            //        string chargeStateString = chargeState == 1 ? "" : new string('+', chargeState);

            //        foreach (var neutralLoss in possibleNeutralLossList)
            //        {
            //            string neutralLossString = neutralLoss == NeutralLoss.NoLoss ? "" : "-" + neutralLoss;

            //            string fragmentIon = ionType + ionNumber + chargeStateString + neutralLossString;
            //            if (!fragmentIonList.Contains(fragmentIon))
            //            {
            //                fragmentIonList.Add(fragmentIon);
            //            }
            //        }
            //    }
            //}

            RefreshPlots();
        }

        private void AllIonNumbersCheckBoxOnUnchecked(object sender, RoutedEventArgs routedEventArgs)
        {
            if (!(sender is CheckBox checkBox))
                return;

            var ionNumber = int.Parse(checkBox.Content.ToString());

            var fragmentIonList = XicBrowserViewModel.FragmentIonList;

            var possibleIonTypes = new List<string> { "b", "y", "a" };
            foreach (var ionType in possibleIonTypes)
            {
                var fragmentIon = ionType + ionNumber;
                fragmentIonList.Remove(fragmentIon);
            }

            var parent = (StackPanel)checkBox.Parent;
            foreach (var child in parent.Children)
            {
                ((CheckBox)child).IsChecked = false;
            }

            RefreshPlots();
        }

        private void AllIonLettersCheckBoxOnChecked(object sender, RoutedEventArgs e)
        {
            if (ionNumbersContainer == null) return;

            if (!(sender is CheckBox checkBox))
                return;

            var ionLetter = checkBox.Content.ToString();

            var fragmentIonList = XicBrowserViewModel.FragmentIonList;

            var possibleResidueNumbers = new List<int>();
            for (var i = 1; i <= ionNumbersContainer.Children.Count; i++)
            {
                possibleResidueNumbers.Add(i);
            }

            foreach (var possibleResidueNumber in possibleResidueNumbers)
            {
                var fragmentIon = ionLetter + possibleResidueNumber;
                if (!fragmentIonList.Contains(fragmentIon))
                {
                    fragmentIonList.Add(fragmentIon);
                }
            }

            // Check all appropriate checkboxes
            var ionCheckBoxList = new List<CheckBox>();
            if (ionLetter.Equals("b")) ionCheckBoxList = m_bIonCheckBoxes;
            if (ionLetter.Equals("y")) ionCheckBoxList = m_yIonCheckBoxes;
            if (ionLetter.Equals("a")) ionCheckBoxList = m_aIonCheckBoxes;

            foreach (var ionCheckBox in ionCheckBoxList)
            {
                ionCheckBox.IsChecked = true;
            }

            RefreshPlots();
        }

        private void AllIonLettersCheckBoxOnUnchecked(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox))
                return;

            var ionLetter = checkBox.Content.ToString();

            var fragmentIonList = XicBrowserViewModel.FragmentIonList;

            var possibleResidueNumbers = new List<int>();
            for (var i = 1; i <= ionNumbersContainer.Children.Count; i++)
            {
                possibleResidueNumbers.Add(i);
            }

            foreach (var possibleResidueNumber in possibleResidueNumbers)
            {
                var fragmentIon = ionLetter + possibleResidueNumber;
                fragmentIonList.Remove(fragmentIon);
            }

            // Check all appropriate checkboxes
            var ionCheckBoxList = new List<CheckBox>();
            if (ionLetter.Equals("b")) ionCheckBoxList = m_bIonCheckBoxes;
            if (ionLetter.Equals("y")) ionCheckBoxList = m_yIonCheckBoxes;
            if (ionLetter.Equals("a")) ionCheckBoxList = m_aIonCheckBoxes;

            foreach (var ionCheckBox in ionCheckBoxList)
            {
                ionCheckBox.IsChecked = false;
            }

            RefreshPlots();
        }

        private void AddChargeState(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox))
                return;

            if (checkBox.Content.ToString().Contains('1'))
            {
                XicBrowserViewModel.FragmentChargeStateList.Add(1);
            }
            else if (checkBox.Content.ToString().Contains('2'))
            {
                XicBrowserViewModel.FragmentChargeStateList.Add(2);
            }
            else if (checkBox.Content.ToString().Contains('3'))
            {
                XicBrowserViewModel.FragmentChargeStateList.Add(3);
            }

            RefreshPlots();
        }

        private void RemoveChargeState(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox))
                return;

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

            RefreshPlots();
        }

        private void RefreshPlots()
        {
            this.XicBrowserViewModel.CreateLcAndImsSlicePlots(this.XicBrowserViewModel.CurrentFeature);
        }

        private void WaterLossChecked(object sender, RoutedEventArgs e)
        {
            if(!this.XicBrowserViewModel.FragmentNeutralLossList.Contains(NeutralLoss.H2O))
            {
                this.XicBrowserViewModel.FragmentNeutralLossList.Add(NeutralLoss.H2O);
                RefreshPlots();
            }
        }

        private void WaterLossUnchecked(object sender, RoutedEventArgs e)
        {
            if(this.XicBrowserViewModel.FragmentNeutralLossList.Remove(NeutralLoss.H2O))
            {
                RefreshPlots();
            }
        }

        private void AmmoniaLossChecked(object sender, RoutedEventArgs e)
        {
            if (!this.XicBrowserViewModel.FragmentNeutralLossList.Contains(NeutralLoss.NH3))
            {
                this.XicBrowserViewModel.FragmentNeutralLossList.Add(NeutralLoss.NH3);
                RefreshPlots();
            }
        }

        private void AmmoniaLossUnchecked(object sender, RoutedEventArgs e)
        {
            if (this.XicBrowserViewModel.FragmentNeutralLossList.Remove(NeutralLoss.NH3))
            {
                RefreshPlots();
            }
        }
    }
}
