using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bunit.Components
{
    public partial class Shallow : Microsoft.AspNetCore.Components.ComponentBase
    {
#pragma warning disable 1998

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            __builder.AddMarkupContent(0, "<div>Shallow</div>");
        }

#pragma warning restore 1998
    }
}
