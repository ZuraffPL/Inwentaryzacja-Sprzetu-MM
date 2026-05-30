using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwentaryzacjaSprzetu.Models;
using InwentaryzacjaSprzetu.Services;

namespace InwentaryzacjaSprzetu.ViewModels
{
    public partial class CategoryEditViewModel : ObservableObject
    {
        private readonly ICategoryService _categoryService;

        [ObservableProperty]
        private string? _code = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string? _description;

        [ObservableProperty]
        private string _validationMessage = string.Empty;

        public Category? Category { get; private set; }
        public bool IsEditMode => Category != null;

        public CategoryEditViewModel(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task InitializeAsync(Category? category = null)
        {
            Category = category;

            // Jeśli edytujemy, wypełnij pola
            if (category != null)
            {
                Code = category.Code ?? string.Empty;
                Name = category.Name;
                Description = category.Description;
            }
            else
            {
                // Dla nowej kategorii wyczyść pola
                Code = string.Empty;
                Name = string.Empty;
                Description = string.Empty;
                ValidationMessage = string.Empty;
            }

            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (!ValidateInput())
                return;

            try
            {
                ValidationMessage = "Zapisywanie...";
                
                if (Category == null) // Nowa kategoria
                {
                    var newCategory = new Category
                    {
                        Code = string.IsNullOrWhiteSpace(Code) ? null : Code.Trim(),
                        Name = Name.Trim(),
                        Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim()
                    };

                    await _categoryService.AddAsync(newCategory);
                }
                else // Edycja
                {
                    Category.Code = string.IsNullOrWhiteSpace(Code) ? null : Code.Trim();
                    Category.Name = Name.Trim();
                    Category.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();

                    await _categoryService.UpdateAsync(Category);
                }

                ValidationMessage = string.Empty;
                OnSaveCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Błąd podczas zapisywania: {ex.Message}";
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ValidationMessage = "Nazwa jest wymagana";
                return false;
            }

            ValidationMessage = string.Empty;
            return true;
        }

        public event Action? OnSaveCompleted;
    }
}