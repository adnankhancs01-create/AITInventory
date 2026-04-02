using System;
using System.Collections.Generic;
using System.Text;

namespace AIT_Inventory_App
{
    public class NotificationServiceMessage
    {
        public event Action<string, string>? OnMessage;
        // type = "success" or "error"
        // message = actual text

        public void ShowSuccess(string message) => OnMessage?.Invoke("success", message);
        public void ShowError(string message) => OnMessage?.Invoke("error", message);
    }
}
