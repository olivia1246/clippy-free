using Clippy.Core.Classes;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Clippy.Core.Services
{
    public class ChatGPTService : IChatService
    {
        // Constants and fields
        private const string ClippyStart = "Hi! I'm Clippy, your Windows assistant. Would you like to get some assistance?";
        private const string Instruction = "You are in an app that revives Microsoft Clippy in Windows. Speak in a Clippy style and try to stay as concise/short as possible and not output long messages.";
        public ObservableCollection<IMessage> Messages { get; } = new ObservableCollection<IMessage>();
        
        private OpenAIService AI;
        private ISettingsService Settings;

        public ChatGPTService(ISettingsService settings)
        {
            Settings = settings;
            Add(new ClippyMessage(ClippyStart, true));
            SetAPI();
        }

        public void Refresh()
        {
            Messages.Clear();
            Add(new ClippyMessage(ClippyStart, true));
        }

        public async Task SendAsync(IMessage message)
        {
            Add(message);
            // Build messages for the GPT
            List<ChatMessage> GPTMessages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(Instruction)
            };

            foreach (IMessage m in Messages)
            {
                if (m is ClippyMessage)
                    GPTMessages.Add(ChatMessage.FromAssistant(m.Message));
                else if (m is UserMessage)
                    GPTMessages.Add(ChatMessage.FromUser(m.Message));
            }

            await Task.Delay(300);
            ClippyMessage Response = new ClippyMessage(true);
            Add(Response);
            GPTMessages.Add(ChatMessage.FromUser(message.Message));

            var completionResult = await AI.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = GPTMessages,
                Model = Models.ChatGpt3_5Turbo,
                MaxTokens = Settings.Tokens,
            });

            if (completionResult.Successful)
            {
                Response.Message = completionResult.Choices.First().Message.Content;
            }
            else
            {
                Response.Message = $"Unfortunately, an error occurred `{completionResult.Error.Message}`";
                Response.IsLatest = false;
            }
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

        private void SetAPI()
        {
            AI = new OpenAIService(new OpenAiOptions()
            {
                BaseUrl = "https://powerful-meris-olivia-s-projects-b18b9350.koyeb.app/v1/"
            });
        }
    }
}
