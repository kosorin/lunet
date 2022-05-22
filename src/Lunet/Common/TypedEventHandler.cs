namespace Lunet.Common;

public delegate void TypedEventHandler<in TSender>(TSender sender);

public delegate void TypedEventHandler<in TSender, in TArgs>(TSender sender, TArgs args);
