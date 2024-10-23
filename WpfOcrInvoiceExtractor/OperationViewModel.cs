using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfOcrInvoiceExtractor
{
    public class OperationViewModel : INotifyPropertyChanged
    {
        private bool _isInProgress;
        private bool _isCompleted;
        private bool _isFailed;
        private string _operationName;

        public event PropertyChangedEventHandler PropertyChanged;

        public string OperationName
        {
            get => _operationName;
            set
            {
                _operationName = value;
                OnPropertyChanged();
            }
        }

        public bool IsInProgress
        {
            get => _isInProgress;
            set
            {
                _isInProgress = value;
                OnPropertyChanged();
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                OnPropertyChanged();
            }
        }

        public bool IsFailed
        {
            get => _isFailed;
            set
            {
                _isFailed = value;
                OnPropertyChanged();
            }
        }

        public OperationViewModel(string operationName)
        {
            OperationName = operationName;
            IsInProgress = true;
            IsCompleted = false;
        }


        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
