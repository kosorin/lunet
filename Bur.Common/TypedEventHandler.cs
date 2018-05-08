using System;

namespace Bur.Common
{
    public delegate void TypedEventHandler<in TSender>(TSender sender, EventArgs e);

    public delegate void TypedEventHandler<in TSender, in TArgs>(TSender sender, TArgs e);
}
