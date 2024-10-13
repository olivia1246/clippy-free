using Clippy.Core.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Clippy.Core.Services
{
    public class ChatGPTService : IChatService
    {
        private const string ClippyStart = "Hi! I'm Clippy, your Windows assistant. Would you like to get some assistance?";
        private const string Instruction = "You are in an app that revives Microsoft Clippy in Windows. Speak in a Clippy style and try to stay as concise/short as possible and not output long messages.";
        public ObservableCollection<IMessage> Messages { get; } = new ObservableCollection<IMessage>();

        private ISettingsService Settings;
        private static readonly HttpClient httpClient = new HttpClient();

        public ChatGPTService(ISettingsService settings)
        {
            Settings = settings;
            Add(new ClippyMessage(ClippyStart, true)); // Initialize Clippy start message
        }

        public void Refresh()
        {
            Messages.Clear();
            Add(new ClippyMessage(ClippyStart, true)); // Refresh chat and reset start message
        }

        public async Task SendAsync(IMessage message)
        {
            Add(message); // Send user message to UI
            List<Dictionary<string, string>> GPTMessages = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "role", "system" }, { "content", Instruction } }
            };

            foreach (IMessage m in Messages)
            {
                if (m is ClippyMessage)
                    GPTMessages.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", m.Message } });
                else if (m is UserMessage)
                    GPTMessages.Add(new Dictionary<string, string> { { "role", "user" }, { "content", m.Message } });
            }

            await Task.Delay(300); // Simulate delay
            ClippyMessage Response = new ClippyMessage(true);
            Add(Response); // Add an empty message to show a response preview

            GPTMessages.Add(new Dictionary<string, string> { { "role", "user" }, { "content", message.Message } });

            string apiResponse = await SendHttpRequestAsync(GPTMessages);

            if (!string.IsNullOrEmpty(apiResponse))
            {
                Response.Message = apiResponse;
            }
            else
            {
                Response.Message = "Unfortunately, an error occurred";
                Response.IsLatest = false;
            }
        }

        private async Task<string> SendHttpRequestAsync(List<Dictionary<string, string>> gptMessages)
        {
            string apiUrl = "https://powerful-meris-olivia-s-projects-b18b9350.koyeb.app/v1/chat/completions";
            string jsonPayload = CreateJsonPayload(gptMessages);

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await httpClient.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode(); // Throws an exception if the status code is not success
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
                return null;
            }

            string responseString = await response.Content.ReadAsStringAsync();
            return ParseResponse(responseString);
        }

        private string CreateJsonPayload(List<Dictionary<string, string>> messages)
        {
            // Convert the list of messages to JSON format
            var payload = new
            {
                model = "gpt-3.5-turbo",
                messages = messages,
                max_tokens = Settings.Tokens
            };

            return JsonSerializer.Serialize(payload);
        }

        private string ParseResponse(string response)
        {
            // Parse the response and extract the content from the first "choice"
            try
            {
                using (JsonDocument document = JsonDocument.Parse(response))
                {
                    JsonElement choices = document.RootElement.GetProperty("choices");
                    if (choices.GetArrayLength() > 0)
                    {
                        return choices[0].GetProperty("message").GetProperty("content").GetString();
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing response: {ex.Message}");
            }

            return "Error parsing response";
        }

        private void Add(IMessage Message)
        {
            foreach (IMessage message in Messages)
            {
                if (message is ClippyMessage)
                    ((ClippyMessage)message).IsLatest = false;
            }
            Messages.Add(Message);
        }
    }
}
