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

namespace Starter3D.Plugin.RollerCoasterEditor
{
    /// <summary>
    /// Interaction logic for AnimationModeControl.xaml
    /// </summary>
    public partial class AnimationModeControl : UserControl
    {
        public AnimationModeControl()
        {
            InitializeComponent();
            this.Loaded += AnimationModeControl_Loaded;
        }

        void AnimationModeControl_Loaded(object sender, RoutedEventArgs e)
        {
            var controller = this.DataContext as RollerCoasterEditorController;
            controller.SetMode(Mode.RunningAnimation);
        }
    }
}
