using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace HelloWorldUWP.Core
{
    /// <summary>
    /// Central registry managing all available modules in the UdS app.
    /// </summary>
    public class ModuleRegistry
    {
        private static readonly Lazy<ModuleRegistry> _instance = 
            new Lazy<ModuleRegistry>(() => new ModuleRegistry());

        public static ModuleRegistry Instance => _instance.Value;

        public ObservableCollection<IAppModule> Modules { get; } = new ObservableCollection<IAppModule>();

        private ModuleRegistry()
        {
            LocalizationService.Instance.LanguageChanged += (s, e) =>
            {
                RefreshAllLanguages();
            };
        }

        /// <summary>
        /// Refreshes all module titles and descriptions when the active language changes.
        /// </summary>
        public void RefreshAllLanguages()
        {
            foreach (var module in Modules)
            {
                module.RefreshLanguage();
            }
        }

        /// <summary>
        /// Registers a new module into the application shell.
        /// </summary>
        public void RegisterModule(IAppModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));

            var existing = Modules.FirstOrDefault(m => m.Id == module.Id);
            if (existing != null)
            {
                var index = Modules.IndexOf(existing);
                Modules[index] = module;
            }
            else
            {
                // Insert maintaining order
                int insertIndex = 0;
                while (insertIndex < Modules.Count && Modules[insertIndex].Order <= module.Order)
                {
                    insertIndex++;
                }
                Modules.Insert(insertIndex, module);
            }
        }

        /// <summary>
        /// Retrieves a registered module by its unique identifier.
        /// </summary>
        public IAppModule GetModule(string id)
        {
            return Modules.FirstOrDefault(m => m.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
