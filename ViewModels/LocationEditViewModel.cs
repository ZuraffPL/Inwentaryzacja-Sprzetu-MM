using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwentaryzacjaSprzetu.Models;
using InwentaryzacjaSprzetu.Services;

namespace InwentaryzacjaSprzetu.ViewModels
{
    public partial class LocationEditViewModel : ObservableObject
    {
        private readonly ILocationService _locationService;
        private readonly ILoggingService _loggingService;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string? _code;

        [ObservableProperty]
        private string? _address;

        [ObservableProperty]
        private string? _city;

        [ObservableProperty]
        private string? _postalCode;

        [ObservableProperty]
        private string? _description;

        [ObservableProperty]
        private string? _pavilionCode;

        [ObservableProperty]
        private string? _countryCode;

        [ObservableProperty]
        private bool _isActive = true;

        [ObservableProperty]
        private string _validationMessage = string.Empty;

        public Location? Location { get; private set; }
        public bool IsEditMode => Location != null;

        public LocationEditViewModel(ILocationService locationService, ILoggingService loggingService)
        {
            _locationService = locationService;
            _loggingService = loggingService;
            
            // Dodajmy debug info
            _loggingService.LogInfoAsync("LocationEditViewModel created").ConfigureAwait(false);
        }

        public async Task InitializeAsync(Location? location = null)
        {
            Location = location;

            // Jeśli edytujemy, wypełnij pola
            if (location != null)
            {
                Name = location.Name;
                Code = location.Code;
                Address = location.Address;
                City = location.City;
                PostalCode = location.PostalCode;
                Description = location.Description;
                PavilionCode = location.PavilionCode;
                CountryCode = location.CountryCode;
                IsActive = location.IsActive;
            }
            else
            {
                // Nowa lokalizacja - wyczyść wszystkie pola
                Name = string.Empty;
                Code = null;
                Address = null;
                City = null;
                PostalCode = null;
                Description = null;
                PavilionCode = null;
                CountryCode = null;
                IsActive = true;
            }

            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            await _loggingService.LogInfoAsync("LocationEditViewModel.SaveAsync() - START");
            
            if (!ValidateInput())
            {
                await _loggingService.LogWarningAsync("LocationEditViewModel.SaveAsync() - Validation failed");
                return;
            }

            try
            {
                ValidationMessage = "Zapisywanie...";
                await _loggingService.LogInfoAsync($"LocationEditViewModel.SaveAsync() - IsEditMode: {IsEditMode}");
                
                if (Location == null) // Nowa lokalizacja
                {
                    await _loggingService.LogInfoAsync("LocationEditViewModel.SaveAsync() - Creating new location");
                    var newLocation = new Location
                    {
                        Name = Name.Trim(),
                        Code = string.IsNullOrWhiteSpace(Code) ? null : Code.Trim(),
                        Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                        City = string.IsNullOrWhiteSpace(City) ? null : City.Trim(),
                        PostalCode = string.IsNullOrWhiteSpace(PostalCode) ? null : PostalCode.Trim(),
                        Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                        PavilionCode = string.IsNullOrWhiteSpace(PavilionCode) ? null : PavilionCode.Trim().ToUpperInvariant(),
                        CountryCode = string.IsNullOrWhiteSpace(CountryCode) ? null : CountryCode.Trim().ToUpperInvariant(),
                        IsActive = IsActive
                    };

                    await _loggingService.LogInfoAsync($"LocationEditViewModel.SaveAsync() - New location: Name={newLocation.Name}, Code={newLocation.Code}");
                    var result = await _locationService.AddAsync(newLocation);
                    await _loggingService.LogInfoAsync($"LocationEditViewModel.SaveAsync() - Location added with ID: {result.Id}");
                }
                else // Edycja
                {
                    await _loggingService.LogInfoAsync($"LocationEditViewModel.SaveAsync() - Updating location ID: {Location.Id}");
                    Location.Name = Name.Trim();
                    Location.Code = string.IsNullOrWhiteSpace(Code) ? null : Code.Trim();
                    Location.Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim();
                    Location.City = string.IsNullOrWhiteSpace(City) ? null : City.Trim();
                    Location.PostalCode = string.IsNullOrWhiteSpace(PostalCode) ? null : PostalCode.Trim();
                    Location.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
                    Location.PavilionCode = string.IsNullOrWhiteSpace(PavilionCode) ? null : PavilionCode.Trim().ToUpperInvariant();
                    Location.CountryCode = string.IsNullOrWhiteSpace(CountryCode) ? null : CountryCode.Trim().ToUpperInvariant();
                    Location.IsActive = IsActive;

                    await _locationService.UpdateAsync(Location);
                    await _loggingService.LogInfoAsync("LocationEditViewModel.SaveAsync() - Location updated");
                }

                ValidationMessage = string.Empty;
                await _loggingService.LogInfoAsync("LocationEditViewModel.SaveAsync() - Invoking OnSaveCompleted");
                OnSaveCompleted?.Invoke();
                await _loggingService.LogInfoAsync("LocationEditViewModel.SaveAsync() - OnSaveCompleted invoked");
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("LocationEditViewModel.SaveAsync() - Error occurred", ex);
                ValidationMessage = $"Błąd podczas zapisywania: {ex.Message}";
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ValidationMessage = "Nazwa lokalizacji jest wymagana";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Code))
            {
                ValidationMessage = "Kod lokalizacji jest wymagany";
                return false;
            }

            ValidationMessage = string.Empty;
            return true;
        }

        public event Action? OnSaveCompleted;
    }
}