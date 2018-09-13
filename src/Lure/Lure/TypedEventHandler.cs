using System;

namespace Lure
{
    public delegate void TypedEventHandler<in TSender>(TSender sender, EventArgs args);

    public delegate void TypedEventHandler<in TSender, in TArgs>(TSender sender, TArgs args);
}
