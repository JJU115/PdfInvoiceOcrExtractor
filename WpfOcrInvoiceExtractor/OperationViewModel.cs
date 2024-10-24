using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfOcrInvoiceExtractor
{
    public class OperationViewModel : INotifyPropertyChanged
    {
        private bool _isInProgress;
        private bool _isCompleted;
        private bool _isFailed;
        private string _operationName;
        private string _failedReason;

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

        public string FailedReason
        {
            get => _failedReason;
            set
            {
                _failedReason = value;
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
