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

namespace Starter3D.Plugin.Physics
{
    /// <summary>
    /// Interaction logic for PhysicsView.xaml
    /// </summary>
    public partial class PhysicsView : UserControl
    {
        private PhysicsController _controller;
        public PhysicsView(PhysicsController controller)
        {
            _controller = controller;
            InitializeComponent();
        }

        private void RestartSimulation_Click(object sender, RoutedEventArgs e)
        {
            _controller.RestartSimulation();
            toggleSimulationBtn.Content = "Pause Simulation";
        }

        private void ToggleSimulation_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if ((string)(btn.Content) == "Pause Simulation")
            {
                _controller.PauseSimulation();
                btn.Content = "Resume Simulation";
            }
            else
            {
                _controller.ResumeSimulation();
                btn.Content = "Pause Simulation";
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            if (cb.IsChecked == true)
            {
                _controller.EnableCollisionDetection();
            }
            else
            {
                _controller.DisableCollisionDetection();
            }
        }
    }
}
