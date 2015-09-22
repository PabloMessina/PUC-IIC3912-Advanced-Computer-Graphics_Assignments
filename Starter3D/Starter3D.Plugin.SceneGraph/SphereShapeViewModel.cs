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
        private float _angularVelRadians;
        private float _angularVelDegrees;
        public ShapeNode SphereShape
        {
            get { return _sphereShape; }
        }

        public float AngularVelocityRadians
        {
            get { return _angularVelRadians; }
        }

        public float AngularVelocityDegrees
        {
            get { return _angularVelDegrees; }
            set {
                if (_angularVelDegrees != value)
                {
                    _angularVelDegrees = value;
                    _angularVelRadians = value * (float)Math.PI / 180;
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
