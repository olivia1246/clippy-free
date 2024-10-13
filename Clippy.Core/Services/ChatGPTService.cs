using Clippy.Core.Classes;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clippy.Core.Services
{
    public class ChatGPTService : IChatService
    {
        private const string ClippyStart = "Hi! I'm Clippy, your Windows assistant. Would you like to get some assistance?";
        private const string Instruction = "You are in an app that revives Microsoft Clippy in Windows. Speak in a Clippy style and try to stay as concise/short as possible and not output long messages.";
        public ObservableCollection<IMessage> Messages { get; } = new ObservableCollection<IMessage>();

        private OpenAIService? AI; // Nullable OpenAIService
        private ISettingsService Settings;

        public ChatGPTService(ISettingsService settings)
        {
            Settings = settings;
            AI = new OpenAIService(new OpenAiOptions() 
            {
                BaseUrl = "https://powerful-meris-olivia-s-projects-b18b9350.koyeb.app/v1/" // Initialize AI
            });
            Add(new ClippyMessage(ClippyStart, true)); // Initialize Clippy start message
        }

        public void Refresh()
        {
            Messages.Clear();
            Add(new ClippyMessage(ClippyStart, true)); // Refresh chat and reset start message
        }

        public async Task SendAsync(IMessage message)
        {
            Add(message); // Add user message to the UI
            List<ChatMessage> GPTMessages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(Instruction)
            };

            // Add all previous messages
            foreach (IMessage m in Messages)
            {
                if (m is ClippyMessage)
                    GPTMessages.Add(ChatMessage.FromAssistant(m.Message ?? "Empty message")); // Ensure no null values
                else if (m is UserMessage)
                    GPTMessages.Add(ChatMessage.FromUser(m.Message ?? "Empty message")); // Ensure no null values
            }

            await Task.Delay(300); // Simulate delay for user experience
            ClippyMessage response = new ClippyMessage(true); // Placeholder for the response
            Add(response); // Add an empty message to show response preview

            // Add the user's new message to the list
            GPTMessages.Add(ChatMessage.FromUser(message.Message ?? "Empty message"));

            var completionResult = await AI.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = GPTMessages,
                Model = Models.ChatGpt3_5Turbo, // Change the model if necessary
                MaxTokens = Settings.Tokens, // Use token limit from settings
            });

            if (completionResult.Successful)
            {
                response.Message = completionResult.Choices.First().Message.Content; // Set response
            }
            else
            {
                response.Message = $"Unfortunately, an error occurred `{completionResult.Error.Message}`";
                response.IsLatest = false;
            }
        }

        private void Add(IMessage message)
        {
            // Mark all previous Clippy messages as not the latest
            foreach (IMessage m in Messages)
            {
                if (m is ClippyMessage)
                    ((ClippyMessage)m).IsLatest = false;
            }

            Messages.Add(message); // Add the new message
        }

        /// <summary>
        /// Set up the API with the necessary base URL and options.
        /// </summary>
        private void SetAPI()
        {
            AI = new OpenAIService(new OpenAiOptions
            {
                BaseUrl = "https://powerful-meris-olivia-s-projects-b18b9350.koyeb.app/v1/" // Base URL for the AI service
            });
        }
    }
}
