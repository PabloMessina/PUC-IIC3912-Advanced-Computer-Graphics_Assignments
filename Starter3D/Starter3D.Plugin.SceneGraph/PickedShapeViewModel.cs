using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starter3D.API.scene.nodes;
using OpenTK;

namespace Starter3D.Plugin.SceneGraph
{
    public class PickedShapeViewModel : ViewModelBase
    {
        private ShapeNode _shapeNode = null;
        private Vector3 rotAxis;
        private float rotAngle;
        private Vector3 position;
        private Vector3 scale;

        public ShapeNode ShapeNode
        {
            get { return _shapeNode; }
            set
            {
                if (_shapeNode != value)
                {
                    _shapeNode = value;
                    _shapeNode.Rotation.ToAxisAngle(out rotAxis, out rotAngle);
                    rotAngle = (float)(rotAngle * 180 / Math.PI);
                    position = _shapeNode.Position;
                    scale = _shapeNode.Scale;
                    OnPropertyChanged();
                }
            }
        }

        #region Rotation getters and setters

        //rotation angle
        public float RotationAngle {
            get { return rotAngle; }
            set
            {
                if (rotAngle != value)
                {
                    var temp = (float)(value * Math.PI / 180);
                    rotAngle = value;
                    _shapeNode.setRotationAngle(temp);
                    OnPropertyChanged("RotationAngle");
                }
            }
        }

        //rotation axis X
        public float RotationAxis_X
        {
            get { return rotAxis.X; }
            set
            {
                if (rotAxis.X != value)
                {
                    rotAxis.X = value;
                    _shapeNode.setRotationAxis(rotAxis);
                    OnPropertyChanged("RotationAxis_X");
                }
            }
        }

        //rotation axis Y
        public float RotationAxis_Y
        {
            get { return rotAxis.Y; }
            set
            {
                if (rotAxis.Y != value)
                {
                    rotAxis.Y = value;
                    _shapeNode.setRotationAxis(rotAxis);
                    OnPropertyChanged("RotationAxis_Y");
                }
            }
        }

        //rotation axis Z
        public float RotationAxis_Z
        {
            get { return rotAxis.Z; }
            set
            {
                if (rotAxis.Z != value)
                {
                    rotAxis.Z = value;
                    _shapeNode.setRotationAxis(rotAxis);
                    OnPropertyChanged("RotationAxis_Z");
                }
            }
        }

        #endregion

        #region Position getters and setters
        //X
        public float Position_X
        {
            get { return _shapeNode.Position.X; }
            set
            {
                if (position.X != value)
                {
                    position.X = value;
                    _shapeNode.Position = position;
                    OnPropertyChanged("Position_X");
                }
            }
        }
        //Y
        public float Position_Y
        {
            get { return _shapeNode.Position.Y; }
            set
            {
                if (position.Y != value)
                {
                    position.Y = value;
                    _shapeNode.Position = position;
                    OnPropertyChanged("Position_Y");
                }
            }
        }
        //Z
        public float Position_Z
        {
            get { return _shapeNode.Position.Z; }
            set
            {
                if (position.Z != value)
                {
                    position.Z = value;
                    _shapeNode.Position = position;
                    OnPropertyChanged("Position_Z");
                }
            }
        }
        #endregion

        #region Scale getters and setters
        //X
        public float Scale_X
        {
            get { return _shapeNode.Scale.X; }
            set
            {
                if (scale.X != value)
                {
                    scale.X = value;
                    _shapeNode.Scale = scale;
                    OnPropertyChanged("Scale_X");
                }
            }
        }
        //Y
        public float Scale_Y
        {
            get { return _shapeNode.Scale.Y; }
            set
            {
                if (scale.Y != value)
                {
                    scale.Y = value;
                    _shapeNode.Scale = scale;
                    OnPropertyChanged("Scale_Y");
                }
            }
        }
        //Z
        public float Scale_Z
        {
            get { return _shapeNode.Scale.Z; }
            set
            {
                if (scale.Z != value)
                {
                    scale.Z = value;
                    _shapeNode.Scale = scale;
                    OnPropertyChanged("Scale_Z");
                }
            }
        }
        #endregion        

        public bool HasShape
        {
            get { return _shapeNode != null; }
        }

        public PickedShapeViewModel() { }
    }
}
