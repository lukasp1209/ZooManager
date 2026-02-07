using System.Windows.Controls;
using ZooManager.Core.Interfaces;
using ZooManager.UI.ViewModels;

namespace ZooManager.UI.Views
{
    public partial class EmployeesView : UserControl
    {
        private readonly EmployeesViewModel _vm;

        public EmployeesView(IPersistenceService persistenceService, IAuthenticationService authService)
        {
            InitializeComponent();

            _vm = new EmployeesViewModel(persistenceService, authService);
            DataContext = _vm;

            NewEmpPassword.PasswordChanged += (_, _) =>
            {
                _vm.NewEmpPassword = NewEmpPassword.Password;
            };

            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(EmployeesViewModel.NewEmpPassword))
                {
                    var target = _vm.NewEmpPassword ?? string.Empty;
                    if (NewEmpPassword.Password != target)
                        NewEmpPassword.Password = target;
                }
            };
        }
    }
}