using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;

namespace Starter3D.Plugin.RollerCoasterEditor
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class EditionModeControl : UserControl
    {
        private RadioButton _selectedRadioButton = null;
        public EditionModeControl()
        {
            InitializeComponent();
            this.Loaded += EditionModeControl_Loaded;
        }

        void EditionModeControl_Loaded(object sender, RoutedEventArgs e)
        {
            var controller = this.DataContext as RollerCoasterEditorController;
            if (_selectedRadioButton != null)
            {
                string content = _selectedRadioButton.Content as string;
                if (content == "Dropping Points")
                {
                    controller.SetMode(Mode.DroppingPoints);
                }
                else if (content == "Picking Points")
                {
                    controller.SetMode(Mode.PickingPoints);
                }
                else if (content == "Show Spline Closed")
                {
                    controller.SetMode(Mode.DisplayingSplineClosed);
                }
                else if (content == "Show as Roller Coaster")
                {
                    controller.SetMode(Mode.DisplayingRollerCoaster);
                }
            }
        }

        private void DeletePoint_Click(object sender, RoutedEventArgs e)
        {
            var controller = this.DataContext as RollerCoasterEditorController;
            controller.DeletePickedPoint();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            _selectedRadioButton = sender as RadioButton;
            var controller = this.DataContext as RollerCoasterEditorController;
            string content = _selectedRadioButton.Content as string;
            if (content == "Dropping Points")
            {
                controller.SetMode(Mode.DroppingPoints);
            }
            else if (content == "Picking Points")
            {
                controller.SetMode(Mode.PickingPoints);
            } else if (content == "Show Spline Closed")
            {
                controller.SetMode(Mode.DisplayingSplineClosed);
            }
            else if (content == "Show as Roller Coaster")
            {
                controller.SetMode(Mode.DisplayingRollerCoaster);
            }
        }

        private void GenerateRollerCoaster_Click(object sender, RoutedEventArgs e)
        {
            var controller = this.DataContext as RollerCoasterEditorController;
            controller.GenerateRollerCoasterRandomly();
        }

        private void SaveRollerCoaster_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".xml";
            dlg.AddExtension = true;
            dlg.Filter = "Text documents (.xml)|*.xml";
            dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "resources\\rollercoasters";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                var controller = this.DataContext as RollerCoasterEditorController;
                string filename = dlg.FileName;
                File.WriteAllText(filename, controller.ConvertSplineToXml());
            }
        }
        private void LoadRollerCoaster_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".xml";
            dlg.AddExtension = true;
            dlg.Filter = "Text documents (.xml)|*.xml";
            dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "resources\\rollercoasters";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                var controller = this.DataContext as RollerCoasterEditorController;
                controller.LoadSplienFromXmlFile(dlg.FileName);
            }
        }

        private void ClearRollerCoaster_Click(object sender, RoutedEventArgs e)
        {
            var controller = this.DataContext as RollerCoasterEditorController;
            controller.ClearRollerCoaster();
        }

    }
}
