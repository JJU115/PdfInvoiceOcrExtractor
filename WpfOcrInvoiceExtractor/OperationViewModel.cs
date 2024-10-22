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

        public OperationViewModel(string operationName)
        {
            OperationName = operationName;
            IsInProgress = true;
            IsCompleted = false;
        }

        public async Task CompleteOperationAsync()
        {
            // Simulate work being done
            await Task.Delay(2000);  // Simulate a 2 second operation
            IsInProgress = false;
            IsCompleted = true;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
