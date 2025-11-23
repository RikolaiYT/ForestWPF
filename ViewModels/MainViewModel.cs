using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace ForestWPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Models.Tree> ConiferousTrees { get; } = new();
        public ObservableCollection<Models.Tree> DeciduousTrees { get; } = new();
        public ObservableCollection<string> SortOptions { get; } = new()
        {
            "По высоте",
            "По диаметру",
            "По виду",
            "По возрасту (убыв.)"
        };

        private bool _isConiferousSelected = true;
        public bool IsConiferousSelected
        {
            get => _isConiferousSelected;
            set
            {
                _isConiferousSelected = value;
                OnPropertyChanged(nameof(IsConiferousSelected));
                OnPropertyChanged(nameof(IsDeciduousSelected));
                OnPropertyChanged(nameof(SelectedTrees));
            }
        }

        public bool IsDeciduousSelected
        {
            get => !_isConiferousSelected;
            set => IsConiferousSelected = !value;
        }

        public ObservableCollection<Models.Tree> SelectedTrees
            => IsConiferousSelected ? ConiferousTrees : DeciduousTrees;

        private Models.Tree? _selectedTree;
        public Models.Tree? SelectedTree
        {
            get => _selectedTree;
            set { _selectedTree = value; OnPropertyChanged(); }
        }

        public string SelectedSort { get; set; } = "По высоте";
        public string SearchSpecies { get; set; } = string.Empty;

        private float _coniferousArea = 0;
        private float _deciduousArea = 0;

        public string ConiferousInfo => GetWoodInfo(ConiferousTrees, _coniferousArea);
        public string DeciduousInfo => GetWoodInfo(DeciduousTrees, _deciduousArea);

        public MainViewModel()
        {
            LoadConiferousCommand = new RelayCommand(_ => LoadFromFile(true));
            LoadDeciduousCommand = new RelayCommand(_ => LoadFromFile(false));
            AddTreeCommand = new RelayCommand(_ => AddTree());
            DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected(), _ => SelectedTree != null);
            SortCommand = new RelayCommand(_ => ApplySort());
            SearchCommand = new RelayCommand(_ => Search());
            ShowTallestCommand = new RelayCommand(_ => ShowTallest());
            ShowThickestCommand = new RelayCommand(_ => ShowThickest());
            ShowAveragesCommand = new RelayCommand(_ => ShowAverages());
        }

        #region Commands
        public RelayCommand LoadConiferousCommand { get; }
        public RelayCommand LoadDeciduousCommand { get; }
        public RelayCommand AddTreeCommand { get; }
        public RelayCommand DeleteSelectedCommand { get; }
        public RelayCommand SortCommand { get; }
        public RelayCommand SearchCommand { get; }
        public RelayCommand ShowTallestCommand { get; }
        public RelayCommand ShowThickestCommand { get; }
        public RelayCommand ShowAveragesCommand { get; }
        #endregion

        private void LoadFromFile(bool coniferous)
        {
            var ofd = new OpenFileDialog { Filter = "Text files|*.txt|All files|*.*" };
            if (ofd.ShowDialog() != true) return;

            try
            {
                using var sr = new StreamReader(ofd.FileName);
                var firstLine = sr.ReadLine();
                if (!float.TryParse(firstLine?.Trim().Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out float area))
                {
                    MessageBox.Show("Не удалось прочитать площадь из файла.");
                    return;
                }

                var collection = coniferous ? ConiferousTrees : DeciduousTrees;
                collection.Clear();

                if (coniferous) _coniferousArea = area;
                else _deciduousArea = area;

                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 5) continue;

                    var species = parts[0];
                    double height = double.TryParse(parts[1].Replace(',', '.'), out var h) ? h : 0;
                    double diameter = double.TryParse(parts[2].Replace(',', '.'), out var d) ? d : 0;
                    int age = int.TryParse(parts[3], out var a) ? a : 0;
                    var woodType = string.Join(" ", parts.Skip(4));

                    var tree = new Models.Tree
                    {
                        Species = species,
                        Height = height,
                        Diameter = diameter,
                        Age = age,
                        WoodType = woodType
                    };

                    collection.Add(tree);
                }

                OnPropertyChanged(nameof(ConiferousInfo));
                OnPropertyChanged(nameof(DeciduousInfo));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке: " + ex.Message);
            }
        }

        private void AddTree()
        {
            var dlg = new Views.AddEditTreeWindow();
            var vm = new AddEditViewModel();
            dlg.DataContext = vm;

            if (dlg.ShowDialog() == true && vm.TryCreateTree(out var tree))
            {
                var collection = IsConiferousSelected ? ConiferousTrees : DeciduousTrees;
                collection.Add(tree);
                OnPropertyChanged(nameof(ConiferousInfo));
                OnPropertyChanged(nameof(DeciduousInfo));
            }
        }

        private void DeleteSelected()
        {
            if (SelectedTree == null) return;
            var collection = IsConiferousSelected ? ConiferousTrees : DeciduousTrees;
            collection.Remove(SelectedTree);
            SelectedTree = null;
            OnPropertyChanged(nameof(ConiferousInfo));
            OnPropertyChanged(nameof(DeciduousInfo));
        }

        private void ApplySort()
        {
            var collection = IsConiferousSelected ? ConiferousTrees : DeciduousTrees;
            var list = collection.ToList();

            list = SelectedSort switch
            {
                "По высоте" => list.OrderByDescending(t => t.Height).ToList(),
                "По диаметру" => list.OrderByDescending(t => t.Diameter).ToList(),
                "По виду" => list.OrderBy(t => t.Species).ToList(),
                "По возрасту (убыв.)" => list.OrderByDescending(t => t.Age).ToList(),
                _ => list
            };

            collection.Clear();
            foreach (var t in list) collection.Add(t);
        }

        private void Search()
        {
            if (string.IsNullOrWhiteSpace(SearchSpecies))
            {
                MessageBox.Show("Введите вид для поиска");
                return;
            }

            var found = SelectedTrees
                .FirstOrDefault(t => t.Species.Equals(SearchSpecies, StringComparison.OrdinalIgnoreCase));

            if (found != null)
            {
                SelectedTree = found;
                MessageBox.Show("Найдено: " + found.Species);
            }
            else
                MessageBox.Show("Дерево не найдено");
        }

        private void ShowTallest() => ShowExtreme(t => t.Height, "высокое", "м");
        private void ShowThickest() => ShowExtreme(t => t.Diameter, "толстое", "см");

        private void ShowExtreme(Func<Models.Tree, double> selector, string what, string unit)
        {
            if (!SelectedTrees.Any())
            {
                MessageBox.Show("Нет данных");
                return;
            }
            var t = SelectedTrees.OrderByDescending(selector).First();
            MessageBox.Show($"Самое {what}: {t.Species}, {selector(t)} {unit}");
        }

        private void ShowAverages()
        {
            if (!SelectedTrees.Any())
            {
                MessageBox.Show("Нет данных");
                return;
            }
            var avgHeight = SelectedTrees.Average(t => t.Height);
            var avgAge = SelectedTrees.Average(t => t.Age);
            MessageBox.Show($"Средняя высота: {avgHeight:F2} м\nСредний возраст: {avgAge:F2} л.");
        }

        private string GetWoodInfo(ObservableCollection<Models.Tree> trees, float area)
        {
            int qty = trees.Count;
            double density = area > 0 ? qty / area : 0;
            double avgAge = qty > 0 ? trees.Average(t => t.Age) : 0;
            return $"Кол-во: {qty}. Площадь: {area} га. Плотность: {density:F2} дерев/га. Средний возраст: {avgAge:F2}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}