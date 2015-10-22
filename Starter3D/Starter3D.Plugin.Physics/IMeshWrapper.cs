using Starter3D.API.renderer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starter3D.Plugin.Physics
{
    public interface IMeshWrapper
    {
        void Configure(IRenderer renderer);
        void Render(IRenderer renderer);
    }
}
