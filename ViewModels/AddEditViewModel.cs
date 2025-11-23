using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace ForestWPF.ViewModels
{
    public class AddEditViewModel : INotifyPropertyChanged
    {
        private string _species = "";
        private string _heightText = "";
        private string _diameterText = "";
        private string _ageText = "";
        private string _woodType = "хвойный";

        public string Species
        {
            get => _species;
            set { _species = value; OnPropertyChanged(); }
        }

        public string HeightText
        {
            get => _heightText;
            set { _heightText = value; OnPropertyChanged(); }
        }

        public string DiameterText
        {
            get => _diameterText;
            set { _diameterText = value; OnPropertyChanged(); }
        }

        public string AgeText
        {
            get => _ageText;
            set { _ageText = value; OnPropertyChanged(); }
        }

        public string WoodType
        {
            get => _woodType;
            set { _woodType = value; OnPropertyChanged(); }
        }

        public ICommand OkCommand { get; }

        public AddEditViewModel()
        {
            OkCommand = new RelayCommand(o =>
            {
                if (o is Window window)
                    window.DialogResult = true;
            });
        }

        public bool TryCreateTree(out Models.Tree tree)
        {
            tree = null;
            if (string.IsNullOrWhiteSpace(Species))
                return false;

            var textH = HeightText?.Replace(',', '.');
            var textD = DiameterText?.Replace(',', '.');

            if (!double.TryParse(textH, NumberStyles.Any, CultureInfo.InvariantCulture, out double h) ||
                !double.TryParse(textD, NumberStyles.Any, CultureInfo.InvariantCulture, out double d) ||
                !int.TryParse(AgeText, out int a))
                return false;

            tree = new Models.Tree
            {
                Species = Species.Trim(),
                Height = h,
                Diameter = d,
                Age = a,
                WoodType = WoodType
            };
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}