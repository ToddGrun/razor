﻿// <auto-generated/>
#pragma warning disable 1591
namespace Test
{
    #line default
    using global::System;
    using global::System.Collections.Generic;
    using global::System.Linq;
    using global::System.Threading.Tasks;
    using global::Microsoft.AspNetCore.Components;
    #line default
    #line hidden
    #nullable restore
    public partial class TestComponent : global::Microsoft.AspNetCore.Components.ComponentBase
    #nullable disable
    {
        #pragma warning disable 1998
        protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            __builder.OpenComponent<global::Test.MyComponent<
#nullable restore
#line (1,20)-(1,26) "x:\dir\subdir\Test\TestComponent.cshtml"
string

#line default
#line hidden
#nullable disable
            >>(0);
            __builder.AddComponentParameter(1, nameof(global::Test.MyComponent<string>.
#nullable restore
#line (1,33)-(1,37) "x:\dir\subdir\Test\TestComponent.cshtml"
Item

#line default
#line hidden
#nullable disable
            ), global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<string>(
#nullable restore
#line (1,38)-(1,43) "x:\dir\subdir\Test\TestComponent.cshtml"
Value

#line default
#line hidden
#nullable disable
            ));
            __builder.AddComponentParameter(2, nameof(global::Test.MyComponent<string>.ItemChanged), (global::System.Action<string>)(__value => Value = __value));
            __builder.CloseComponent();
        }
        #pragma warning restore 1998
#nullable restore
#line (2,8)-(4,1) "x:\dir\subdir\Test\TestComponent.cshtml"

    string Value;

#line default
#line hidden
#nullable disable

    }
}
#pragma warning restore 1591
