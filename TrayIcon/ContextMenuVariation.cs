using System;

namespace NullSoftware.ToolKit
{
    /// <summary>
    /// Context menu generation mode.
    /// </summary>
    public enum ContextMenuVariation : byte
    {

#if NETCOREAPP3_1_OR_GREATER
        [Obsolete]
#endif
        /// <summary>
        /// Use <see cref="System.Windows.Forms.ContextMenu"/>.
        /// </summary>
        /// <remarks>
        /// This options will no longer work in new .NET versions starting from .NET Core 3.1 due to deprecation. 
        /// See more at <see href="https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.contextmenu">documentation page</see>.
        /// </remarks>
        ContextMenu,

        /// <summary>
        /// Use <see cref="System.Windows.Forms.ContextMenuStrip"/>.
        /// </summary>
        ContextMenuStrip
    }
}
