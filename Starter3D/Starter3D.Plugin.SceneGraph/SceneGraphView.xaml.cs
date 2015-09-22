using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Starter3D.Plugin.SceneGraph
{
  /// <summary>
  /// Interaction logic for MaterialEditorUI.xaml
  /// </summary>
  public partial class SceneGraphView : UserControl
  {
      SceneGraphController controller;
    public SceneGraphView(SceneGraphController controller)
    {
      InitializeComponent();
      this.DataContext = controller;
      this.controller = controller;
    }

    private void TreeViewItem_PreviewMouseDown(object sender, System.Windows.RoutedEventArgs e)
    {
        //var x = VisualTreeHelper.GetParent();
        DependencyObject x = e.OriginalSource as DependencyObject;
        while (!(x is TreeViewItem))
            x = VisualTreeHelper.GetParent(x);

        var item = x as TreeViewItem;
        var viewmodel = item.DataContext as ShapeTreeViewModel;
        controller.MouseDownOnThisShapeTreeViewModel(viewmodel);
        e.Handled = true;
    }

    private void TreeViewItem_MouseUp(object sender, System.Windows.RoutedEventArgs e)
    {
        var item = sender as TreeViewItem;
        var viewmodel = item.DataContext as ShapeTreeViewModel;
        controller.MouseUpOnThisShapeTreeViewModel(viewmodel);
        e.Handled = true;

    }

    void TreeViewItem_Selected(object sender, RoutedEventArgs e)
    {
        var item = sender as TreeViewItem;
        var viewmodel = item.DataContext as ShapeTreeViewModel;
        controller.PickedShapeTreeViewModel = viewmodel;
        controller.DebugDisplayText += "TreeViewItem_Selected called()\n\tviewmodel = " + viewmodel.DisplayText+"\n";
        e.Handled = true;
    }

    private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var scrollviewer = sender as ScrollViewer;        
        scrollviewer.ScrollToBottom();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        controller.makeSelectedTreeViewModelRoot();
    }

    private void AnimationButtonClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var tag = button.Tag as string;
        if (tag == "stopped")
        {
            button.Tag = "running";
            button.Content = "Stop Animation";
            controller.RunAnimation();
        }
        else
        {
            button.Tag = "stopped";
            button.Content = "Run Animation";
            controller.StopAnimation();
        }
    }

  }
}

