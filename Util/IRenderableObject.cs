using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BooBoo.Util
{
    internal interface IRenderableObject
    {
        public int renderPriority { get; set; }

        public abstract void Draw();
    }
}
