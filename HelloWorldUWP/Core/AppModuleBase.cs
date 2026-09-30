using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HelloWorldUWP.Core
{
    public abstract class AppModuleBase : IAppModule
    {
        public abstract string Id { get; }
        public abstract string Title { get; }
        public abstract string Description { get; }
        public abstract string IconGlyph { get; }
        public abstract Type TargetPageType { get; }
        public abstract int Order { get; }
        public virtual bool IsEnabled => true;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public virtual void RefreshLanguage()
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Description));
        }
    }
}
