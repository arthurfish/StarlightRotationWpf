// StarSelectionViewModel.cs
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace StarlightRotationWpf
{
    public class StarSelectionViewModel : INotifyPropertyChanged
    {
        private const string StarDataFilePath = "StarData.json";
        private readonly HardwareService _hardwareService;

        public ObservableCollection<StarData> Stars { get; private set; }

        private StarData _selectedStar;
        public StarData SelectedStar
        {
            get => _selectedStar;
            set
            {
                _selectedStar = value;
                OnPropertyChanged();
                // 当用户选择一个星点时，用它的数据填充编辑框
                if (_selectedStar != null)
                {
                    EditStarNo = _selectedStar.No;
                    EditStarSize = _selectedStar.Size;
                    EditStarAngle = _selectedStar.Angle;
                }
            }
        }

        private int _targetPosition = 1;
        public int TargetPosition
        {
            get => _targetPosition;
            set { _targetPosition = value; OnPropertyChanged(); }
        }

        public int StarTableIndex
        {
            get => TargetPosition - 1;
            set { 
                OnPropertyChanged();
            }
        }

        // 用于绑定“编辑星点”文本框的属性
        private int _editStarNo;
        public int EditStarNo { get => _editStarNo; set { _editStarNo = value; OnPropertyChanged(); } }

        private double _editStarSize;
        public double EditStarSize { get => _editStarSize; set { _editStarSize = value; OnPropertyChanged(); } }

        private double _editStarAngle;
        public double EditStarAngle { get => _editStarAngle; set { _editStarAngle = value; OnPropertyChanged(); } }

        // Commands
        public ICommand MoveToPositionCommand { get; }
        public ICommand UpdateStarCommand { get; }
        public ICommand IncrementPositionCommand { get; }
        public ICommand DecrementPositionCommand { get; }


        public StarSelectionViewModel(HardwareService hardwareService)
        {
            _hardwareService = hardwareService;
            LoadStars(); // 持久化逻辑

            MoveToPositionCommand = new RelayCommand(MoveFilterWheel);
            IncrementPositionCommand = new RelayCommand(IncrementPosition);
            DecrementPositionCommand = new RelayCommand(DecrementPosition);
            UpdateStarCommand = new RelayCommand(UpdateAndSaveStar);

            // 读取滤光轮的初始位置
            int initialPosition = 0;
            FilterWheelApi.GetPosition(_hardwareService.FilterWheelHandle, out initialPosition);
            TargetPosition = initialPosition > 0 ? initialPosition : 1;
        }

        private void MoveFilterWheel()
        {
            if (_hardwareService.FilterWheelHandle < 0) return;

            Trace.WriteLine($"Moving filter wheel to position: {TargetPosition}");
            var result = FilterWheelApi.SetPosition(_hardwareService.FilterWheelHandle, TargetPosition);
            if (result != 0)
            {
                Trace.WriteLine($"Filter wheel error: {FilterWheelApi.GetErrorMessage(result)}");
            }
            StarTableIndex = 666;
        }

        private void UpdateAndSaveStar()
        {
            // 在列表中查找具有匹配序号的星点
            var starToUpdate = Stars.FirstOrDefault(s => s.No == EditStarNo);
            if (starToUpdate != null)
            {
                // 更新该星点的数据
                starToUpdate.Size = EditStarSize;
                starToUpdate.Angle = EditStarAngle;
            }
            // (可选) 如果找不到，可以添加一个新的星点
            // else 
            // {
            //     Stars.Add(new StarData { No = EditStarNo, Size = EditStarSize, Angle = EditStarAngle });
            // }

            SaveStars(); // 保存更改到文件
            System.Diagnostics.Trace.WriteLine("Star data updated and saved.");
        }

        private void LoadStars()
        {
            if (File.Exists(StarDataFilePath))
            {
                try
                {
                    string json = File.ReadAllText(StarDataFilePath);
                    var loadedStars = JsonSerializer.Deserialize<ObservableCollection<StarData>>(json);
                    Stars = new ObservableCollection<StarData>(loadedStars);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Error loading star data: {ex.Message}");
                    LoadDefaultStars();
                }
            }
            else
            {
                LoadDefaultStars();
                SaveStars(); // 如果文件不存在，创建并保存默认数据
            }
        }

        private void SaveStars()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Stars, options);
                File.WriteAllText(StarDataFilePath, json);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error saving star data: {ex.Message}");
            }
        }

        private void LoadDefaultStars()
        {
            Stars = new ObservableCollection<StarData>
            {
                new StarData { No = 1, Size = 0.014, Angle = 0.002 },
                new StarData { No = 2, Size = 0.035, Angle = 0.005 },
                new StarData { No = 3, Size = 0.056, Angle = 0.008 },
                new StarData { No = 4, Size = 0.084, Angle = 0.012 },
                new StarData { No = 5, Size = 0.11,  Angle = 0.016 },
                new StarData { No = 6, Size = 0.14,  Angle = 0.02  }
            };
        }

        private void IncrementPosition()
        {
            TargetPosition += 1;
            MoveFilterWheel();
            StarTableIndex = 666;
        }

        private void DecrementPosition()
        {
            TargetPosition -= 1;
            MoveFilterWheel();
            StarTableIndex = 666;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}