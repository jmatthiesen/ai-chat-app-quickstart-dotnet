using Azure.Core.Diagnostics;
using Azure.Identity;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;

public class Message
{
    public string Role
    {
        get; set;
    }

    public string Content
    {
        get; set;
    }
}
internal class ChatHandler
{
    public ChatHandler()
    {
    }

    internal async Task<Message> Chat(List<Message> messages)
    {
        IKernelBuilder builder = Kernel.CreateBuilder();
        InitBuilder(builder);

        Kernel kernel = builder.Build();

        ChatHistory history = new ChatHistory("You are a helpful assistant.");
        foreach (Message message in messages)
        {
            history.AddMessage(new AuthorRole(message.Role), message.Content);
        }

        IChatCompletionService chat = kernel.GetRequiredService<IChatCompletionService>();
        ChatMessageContent response = await chat.GetChatMessageContentAsync(history);

        return new Message()
        {
            Role = response.Role.ToString(),
            Content = (response.Items[0] as TextContent).Text
        };
    }

    internal async IAsyncEnumerable<string> Stream(List<Message> messages)
    {
        IKernelBuilder builder = Kernel.CreateBuilder();
        InitBuilder(builder);

        Kernel kernel = builder.Build();

        ChatHistory history = new ChatHistory("You are a helpful assistant.");
        foreach (Message message in messages)
        {
            history.AddMessage(new AuthorRole(message.Role), message.Content);
        }

        IChatCompletionService chat = kernel.GetRequiredService<IChatCompletionService>();
        IAsyncEnumerable<StreamingChatMessageContent> response = chat.GetStreamingChatMessageContentsAsync(history);

        await foreach (StreamingChatMessageContent content in response)
        {
            yield return (content.Content);
        }
    }

    private static void InitBuilder(IKernelBuilder builder)
    {
        string openai_host = Environment.GetEnvironmentVariable("OPENAI_HOST");

        if (String.IsNullOrEmpty(openai_host))
        {
            openai_host = "openai";
        }

        if (openai_host.Equals("local", StringComparison.OrdinalIgnoreCase))
        {
            string endpoint = Environment.GetEnvironmentVariable("LOCAL_MODELS_ENDPOINT");
            if (!String.IsNullOrEmpty(endpoint))
            {
                Uri uri = new Uri(endpoint);
#pragma warning disable SKEXP0010
                builder.AddOpenAIChatCompletion("gpt-3.5", uri, "no key needed");
            }
        }
        else if (openai_host.Equals("github", StringComparison.OrdinalIgnoreCase))
        {
            string endpoint = Environment.GetEnvironmentVariable("GITHUB_MODELS_ENDPOINT");
            string token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            string model = Environment.GetEnvironmentVariable("GITHUB_MODELS_NAME");
            if (!String.IsNullOrEmpty(endpoint) && !String.IsNullOrEmpty(token) && !String.IsNullOrEmpty(model))
            {
                Uri uri = new Uri(endpoint);
                builder.AddOpenAIChatCompletion(model, uri, token);
            }
        }
        else
        {
            string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            string model = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHATGPT_DEPLOYMENT");
            if (!String.IsNullOrEmpty(endpoint) && !String.IsNullOrEmpty(model))
            {
                string key = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
                if (!String.IsNullOrEmpty(key))
                {
                    builder.AddAzureOpenAIChatCompletion(model, endpoint, key);
                }
                else
                {
                }
                builder.AddAzureOpenAIChatCompletion(model, endpoint, new DefaultAzureCredential());
            }
        }
    }
}