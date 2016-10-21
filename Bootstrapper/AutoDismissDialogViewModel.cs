using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bootstrapper
{
    public class AutoDismissDialogViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly DateTime _startTime = DateTime.Now;
        private readonly Timer _timer;
        private readonly Action _timedOut;
        private bool _timeOutActioned = false;
        private int _secondsRemaining;
        private int _percentRemaining;
        private int _autoDismissAfterMs;

        public AutoDismissDialogViewModel(int autoDismissAfterMs, string message, Action timedOut)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (timedOut == null) throw new ArgumentNullException(nameof(timedOut));

            _autoDismissAfterMs = autoDismissAfterMs;
            _timedOut = timedOut;
            Message = message;

            var granularity = TimeSpan.FromMilliseconds(20);
            _timer = new Timer(state => UpdateTimeRemaining(), null, granularity, granularity);
        }

        public string Message { get; private set; }

        public int SecondsRemaining
        {
            get { return _secondsRemaining; }
            private set
            {
                if (_secondsRemaining == value) return;
                _secondsRemaining = value;
                OnPropertyChanged();
            }
        }

        public int PercentRemaining
        {
            get { return _percentRemaining; }
            private set
            {
                if (_percentRemaining == value) return;
                _percentRemaining = value;
                OnPropertyChanged();
            }
        }

        private void UpdateTimeRemaining()
        {
            var timeSpan = _startTime.Add(TimeSpan.FromMilliseconds(_autoDismissAfterMs)) - DateTime.Now;
            var ms = Math.Min(0, timeSpan.Milliseconds);
            SetTimeRemaining((int)(100d/_autoDismissAfterMs* timeSpan.TotalMilliseconds), (int)timeSpan.TotalSeconds);

            if (PercentRemaining > 0 || _timeOutActioned) return;

            _timeOutActioned = true;
            _timedOut();
        }

        private void SetTimeRemaining(int percent, int seconds)
        {
            PercentRemaining = percent;
            SecondsRemaining = seconds;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
