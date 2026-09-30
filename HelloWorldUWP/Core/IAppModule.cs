using System;
using System.ComponentModel;

namespace HelloWorldUWP.Core
{
    /// <summary>
    /// Contract implemented by every pluggable module in the UdS app.
    /// </summary>
    public interface IAppModule : INotifyPropertyChanged
    {
        /// <summary>
        /// Unique identifier for the module (e.g. "mensa", "news", "map").
        /// </summary>
        string Id { get; }

        /// <summary>
        /// User-visible title displayed in the navigation drawer and header.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Short description of the module.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Segoe MDL2 Assets font icon glyph (e.g. "\uE7BE" for Food/Mensa).
        /// </summary>
        string IconGlyph { get; }

        /// <summary>
        /// The XAML Page type to navigate to when this module is selected.
        /// </summary>
        Type TargetPageType { get; }

        /// <summary>
        /// Display order in the navigation drawer.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Whether this module is currently enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Refreshes module title and description when language changes.
        /// </summary>
        void RefreshLanguage();
    }
}
