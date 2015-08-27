using System.Windows.Controls;
using System.Collections.Generic;
using Starter3D.API.scene.nodes;
using Starter3D.API.resources;
using OpenTK;

namespace Starter3D.Plugin.SimpleMaterialEditor
{
  /// <summary>
  /// Interaction logic for MaterialEditorUI.xaml
  /// </summary>
  public partial class SimpleMaterialEditorView : UserControl
  {     

    //event for changes of shape
    public delegate void ShapeChangedHandler(int index);
    public event ShapeChangedHandler ShapeChanged;

    //event for changes of material
    public delegate void MaterialChangedHandler(string materialName);
    public event MaterialChangedHandler MaterialChanged;

    //event for changes of numeric parameters on the current material
    public delegate void NumericParameterChangedHandler(string key, float val);
    public event NumericParameterChangedHandler NumericParameterChanged;

    //event for changes of vector parameters on the current material
    public delegate void VectorParameterChangedHandler(string key, Vector3 val);
    public event VectorParameterChangedHandler VectorParameterChanged;

    private Dictionary<string, TextBox> textBoxDictionary = new Dictionary<string,TextBox>();
    private Dictionary<string, float> numericParametersDictionary = new Dictionary<string,float>();
    private Dictionary<string, Vector3> vectorParametersDictionary = new Dictionary<string,Vector3>();

    private SimpleMaterialEditorController _controller;



    private static string _sepString = "______";
    private static string[] _separator = { _sepString };

    public SimpleMaterialEditorView(IEnumerable<ShapeNode> shapes, IEnumerable<IMaterial> materials, SimpleMaterialEditorController controller)
    {
        InitializeComponent();

        //generate shapes combobox
        foreach (var shape in shapes) 
            shapesComboBox.Items.Add(shape.Shape.Name);

        //generate materials combobox
        foreach (var material in materials)
            materialsComboBox.Items.Add(material.Name);

        //save a pointer to the controller
        _controller = controller;
    }

    private void refreshEditPanel(IMaterial mat)
    {
        EditPanel.Children.Clear();
        textBoxDictionary.Clear();
        numericParametersDictionary.Clear();
        vectorParametersDictionary.Clear();
        
        //==========================================
        //add editable fields for vector parameters
        //==========================================

        //label
        Label vectorLabel = new Label();
        vectorLabel.Content = "Vector Attributes";
        EditPanel.Children.Add(vectorLabel);

        //vector attributes
        foreach (var vp in mat.VectorParameters)
        {
            //keep track of the vector in a dictionary
            vectorParametersDictionary.Add(vp.Key, vp.Value);

            //generate matching editable fields
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            EditPanel.Children.Add(sp);

            Label label = new Label();
            label.Content = vp.Key;            
            sp.Children.Add(label);

            TextBox tb1 = new TextBox();
            string _key1 = vp.Key + _sepString + "1";
            tb1.Name = _key1;
            tb1.Text = vp.Value.X+"";
            tb1.Tag = vp.Value.X;
            tb1.TextChanged += vectorTextBoxChanged;
            sp.Children.Add(tb1);
            textBoxDictionary.Add(_key1, tb1);  

            TextBox tb2 = new TextBox();
            string _key2 = vp.Key + _sepString + "2";
            tb2.Name = _key2;
            tb2.Text = vp.Value.Y + "";
            tb2.Tag = vp.Value.Y;
            tb2.TextChanged += vectorTextBoxChanged;
            sp.Children.Add(tb2);
            textBoxDictionary.Add(_key2, tb2);

            TextBox tb3 = new TextBox();
            string _key3 = vp.Key + _sepString + "3";
            tb3.Name = _key3;
            tb3.Text = vp.Value.Z + "";
            tb3.Tag = vp.Value.Z;
            tb3.TextChanged += vectorTextBoxChanged;
            sp.Children.Add(tb3);
            textBoxDictionary.Add(_key3, tb3);
        }

        //==========================================
        //add editable fields for numeric parameters
        //==========================================

        //label
        Label numericLabel = new Label();
        numericLabel.Content = "Numeric Attributes";
        EditPanel.Children.Add(numericLabel);

        //numeric attributes
        foreach (var np in mat.NumericParameters)
        {
            //keep track of the float in a dictionary
            numericParametersDictionary.Add(np.Key, np.Value);

            //generate matching editable fields
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            EditPanel.Children.Add(sp);

            Label label = new Label();
            label.Content = np.Key;
            sp.Children.Add(label);

            TextBox tb = new TextBox();
            tb.Name = np.Key;
            tb.Tag = np.Value;
            tb.Text = np.Value + "";
            tb.TextChanged += numericTextBoxChanged;
            sp.Children.Add(tb);
            textBoxDictionary.Add(np.Key, tb);
        }
    }

    void numericTextBoxChanged(object sender, TextChangedEventArgs e)
    {
        TextBox tb = (TextBox)sender;
        float newValue;
        if (float.TryParse(tb.Text, out newValue))
        {
            string key = tb.Name;
            numericParametersDictionary[key] = newValue;
            tb.Tag = newValue;
            NumericParameterChanged(key, newValue);
        }
        else
        {
            tb.Text = "" + (float)tb.Tag;
        }
    }
    void vectorTextBoxChanged(object sender, TextChangedEventArgs e)
    {
        TextBox tb = (TextBox)sender;
        float newValue;
        if (float.TryParse(tb.Text, out newValue))
        {
            string[] parts = tb.Name.Split(_separator, System.StringSplitOptions.RemoveEmptyEntries);
            string key = parts[0];
            int number = int.Parse(parts[1]);
            Vector3 vector = vectorParametersDictionary[key];

            //update the right component
            if(number == 1) 
                vector.X = newValue;
            else if(number == 2)
                vector.Y = newValue;
            else
                vector.Z = newValue;

            vectorParametersDictionary[key] = vector;//update the vector
            tb.Tag = newValue; //update the tag
            VectorParameterChanged(key, vector); //throw event
        }
        else
        {
            tb.Text = "" + (float)tb.Tag;
        }
    }

    private void shapesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ShapeChanged != null)
        {
            ShapeChanged(shapesComboBox.SelectedIndex);
        }
    }

    private void materialsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if(MaterialChanged != null)
        {
            MaterialChanged((string)materialsComboBox.SelectedValue);
            refreshEditPanel(_controller.CurrentMaterial);
        }
    }
  }
}
