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
    /// Interaction logic for RollerCoasterEditorView.xaml
    /// </summary>
    public partial class RollerCoasterEditorView : UserControl
    {
        private RollerCoasterEditorController _controller;
        public RollerCoasterEditorView(RollerCoasterEditorController controller)
        {
            InitializeComponent();
            _controller = controller;
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            _controller.KeyDown((int)e.Key);
            e.Handled = true;
        }
    }
}
