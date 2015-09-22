using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Starter3D.API.scene.nodes;

namespace Starter3D.Plugin.SceneGraph
{
    public class ShapeTreeViewModel : ViewModelBase
    {
        private ShapeNode _shapeNode = null;        
        public ObservableCollection<ShapeTreeViewModel> Children { get; private set; }
        public String DisplayText
        {
            get
            {
                var tag = tagsDictionary.ContainsKey(_shapeNode) ?
                    tagsDictionary[_shapeNode] : "NO_TAG";

                return tag +" hashcode: " + _shapeNode.GetHashCode();
            }
        }

        public ShapeTreeViewModel Parent { get; set; }

        public ShapeNode ShapeNode
        {
            get { return _shapeNode; }
            set
            {
                if (_shapeNode != value)
                {
                    _shapeNode = value;
                    OnPropertyChanged();
                }
            }
        }

        public ShapeTreeViewModel(ShapeNode shapeNode, ShapeTreeViewModel parent)
        {
            _shapeNode = shapeNode;
            Parent = parent;
            Children = new ObservableCollection<ShapeTreeViewModel>();
            foreach(ShapeNode shape_child in _shapeNode.Children) {
                Children.Add(new ShapeTreeViewModel(shape_child,this));
            }

        }

        public bool IsAncestorOf(ShapeTreeViewModel other) {        
            if(other == null)
                return false;
            var temp = other;
            while(temp.Parent != null) {
                temp = temp.Parent;
                if(temp == this)
                    return true;
            }
            return false;
        }


        #region STATIC PART

        internal static Dictionary<ShapeNode, string> tagsDictionary = new Dictionary<ShapeNode, string>();
        private static int rectangleCount = 0;
        private static int circleCount = 0;
        public static void SetTagsRecursively(ShapeNode shape) {

            tagsDictionary[shape] = (shape.Children.Count() == 0) ? 
                "Rectangle_" + rectangleCount++ : "Circle_" + circleCount++;

            foreach (ShapeNode snchild in shape.Children)
                SetTagsRecursively(snchild);
        }

        #endregion

    }
}
