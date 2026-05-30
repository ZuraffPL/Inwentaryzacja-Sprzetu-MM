using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwentaryzacjaSprzetu.Models;
using InwentaryzacjaSprzetu.Services;

namespace InwentaryzacjaSprzetu.ViewModels
{
    public partial class DepartmentEditViewModel : ObservableObject
    {
        private readonly IDepartmentService _departmentService;

        [ObservableProperty]
        private Department _department = new();

        [ObservableProperty]
        private string _title = "Dodaj nowy dział";

        [ObservableProperty]
        private bool _isEditing = false;

        public DepartmentEditViewModel(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        public Task InitializeAsync(Department? department = null)
        {
            if (department != null)
            {
                Department = new Department
                {
                    Id = department.Id,
                    Code = department.Code,
                    Name = department.Name,
                    Description = department.Description,
                    IsActive = department.IsActive,
                    CreatedDate = department.CreatedDate
                };
                Title = "Edytuj dział";
                IsEditing = true;
            }
            else
            {
                Department = new Department();
                Title = "Dodaj nowy dział";
                IsEditing = false;
            }
            
            return Task.CompletedTask;
        }

        [RelayCommand]
        public async Task<bool> SaveAsync()
        {
            try
            {
                if (IsEditing)
                {
                    await _departmentService.UpdateAsync(Department);
                }
                else
                {
                    await _departmentService.CreateAsync(Department);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Department.Code) && 
                   !string.IsNullOrWhiteSpace(Department.Name);
        }
    }
}