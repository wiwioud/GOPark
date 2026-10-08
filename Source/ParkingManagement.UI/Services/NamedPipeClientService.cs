using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using ParkingManagement.UI.ViewModels;

namespace ParkingManagement.UI.Services
{
    public class NamedPipeClientService : ObservableObject
    {
        private const string PipeName = "ParkingMonitoringPipe";
        
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }
        
        public event Action<string>? OnNotificationReceived;

        public async Task StartListeningAsync()
        {
            while (true)
            {
                try
                {
                    using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.In, PipeOptions.Asynchronous);
                    
                    await client.ConnectAsync(5000);
                    IsConnected = true;
                    
                    using var reader = new StreamReader(client);
                    while (!reader.EndOfStream)
                    {
                        var message = await reader.ReadLineAsync();
                        if (!string.IsNullOrEmpty(message))
                        {
                            try
                            {
                                var notification = JsonSerializer.Deserialize<NotificationMessage>(message);
                                if (notification != null)
                                {
                                    OnNotificationReceived?.Invoke(notification.Message);
                                }
                            }
                            catch
                            {
                                OnNotificationReceived?.Invoke(message);
                            }
                        }
                    }
                }
                catch (TimeoutException)
                {
                    IsConnected = false;
                    await Task.Delay(2000);
                }
                catch (Exception)
                {
                    IsConnected = false;
                    await Task.Delay(5000);
                }
            }
        }
    }

    public class NotificationMessage
    {
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
