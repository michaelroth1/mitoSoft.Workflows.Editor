using System.Reflection;
using Microsoft.Extensions.Configuration;
using ReactiveUI;
using Splat;
using mitoSoft.Workflows.Editor.Helpers.Converters;
using WritableJsonConfiguration;
using Application = System.Windows.Application;
using System.Windows.Threading;
using System.Windows;

namespace mitoSoft.Workflows.Editor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetCallingAssembly());
            Locator.CurrentMutable.RegisterConstant(new ConverterBoolAndVisibility(), typeof(IBindingTypeConverter));

            IConfigurationRoot configuration;
            configuration = WritableJsonConfigurationFabric.Create("Settings.json");
            Locator.CurrentMutable.RegisterConstant(configuration, typeof(IConfiguration));

            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        }

        void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var message = e.Exception?.InnerException?.Message;

            if (string.IsNullOrEmpty(message))
            {
                message = e.Exception.Message;
            }

            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            e.Handled = true;
        }
    }
}
