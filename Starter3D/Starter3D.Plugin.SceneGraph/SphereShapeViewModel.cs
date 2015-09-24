using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starter3D.API.scene.nodes;

namespace Starter3D.Plugin.SceneGraph
{
    public class SphereShapeViewModel : ViewModelBase
    {
        private ShapeNode _sphereShape;
        private double _angularVelRadians;
        private double _angularVelDegrees;
        public ShapeNode SphereShape
        {
            get { return _sphereShape; }
        }

        public double AngularVelocityRadians
        {
            get { return _angularVelRadians; }
        }

        public double AngularVelocityDegrees
        {
            get { return _angularVelDegrees; }
            set {
                if (_angularVelDegrees != value)
                {
                    if (double.IsNaN(value))
                        return;

                    _angularVelDegrees = value;
                    _angularVelRadians = value * Math.PI / 180;
                    OnPropertyChanged("AngularVelocityDegrees");
                }
            }
        }

        public string DisplayText
        {
            get { return ShapeTreeViewModel.tagsDictionary[_sphereShape]; }
        }

        public SphereShapeViewModel(ShapeNode sphere)
        {
            _sphereShape = sphere;
            _angularVelRadians = 0;
            _angularVelDegrees = 0;
        }

    }
}
