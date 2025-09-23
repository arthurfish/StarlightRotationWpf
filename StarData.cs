// StarData.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StarlightRotationWpf
{
    // A simple data model for a star point.
    // Implements INotifyPropertyChanged to support two-way data binding in the DataGrid.
    public class StarData : INotifyPropertyChanged
    {
        private int _no;
        private double _size;
        private double _angle;

        public int No
        {
            get => _no;
            set { _no = value; OnPropertyChanged(); }
        }

        public double Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(); }
        }

        public double Angle
        {
            get => _angle;
            set { _angle = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}