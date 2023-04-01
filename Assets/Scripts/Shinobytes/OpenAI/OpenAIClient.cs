/* 
 * This file is part of RavenBot (https://www.github.com/zerratar/ravenbot/).
 * Copyright (c) 2017-2023 Shinobytes, Karl Patrik Johansson, zerratar@gmail.com
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.  
 **/

using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;

using Shinobytes.OpenAI.Models;

namespace Shinobytes.OpenAI
{
    public class OpenAIClient : IOpenAIClient, IDisposable
    {
        private bool disposed;
        private readonly HttpClient client;
        private readonly IOpenAIClientSettings settings;
        private readonly Func<IOpenAIClientSettings> getSettings;

        public OpenAIClient(Func<IOpenAIClientSettings> getSettings)
        {
            client = new HttpClient();
            this.getSettings = getSettings;
        }

        public OpenAIClient(IOpenAIClientSettings settings)
        {
            this.settings = settings;
            client = new HttpClient();
        }

        public async Task<ImageResponse> GenerateImageAsync(string prompt, string size = "512x512", int count = 1)
        {
            return await RequestAsync<ImageRequest, ImageResponse>("https://api.openai.com/v1/images/generations", ImageRequest.Create(prompt, size, count));
        }

        public async Task<ChatCompletionResponse> GetCompletionAsync(string prompt, params ChatMessage[] previousMessages)
        {
            var msgs = new List<ChatMessage>();
            msgs.AddRange(previousMessages);
            msgs.Add(ChatMessage.Create("user", prompt));

            return await RequestAsync<ChatCompletionRequest, ChatCompletionResponse>("https://api.openai.com/v1/chat/completions", new ChatCompletionRequest
            {
                //Model = "text-davinci-003",
                Model = "gpt-3.5-turbo",
                //Model = "davinci:ft-shinobytes-2023-02-20-14-02-16",
                // Prompt = "What if Nicholas Cage played the lead role in Superman?",
                Messages = msgs.ToArray()
            });
        }

        private async Task<TResult> RequestAsync<TRequest, TResult>(string url, TRequest model)
        {
            var s = settings;
            if (s == null)
            {
                s = getSettings();
            }

            using (var httpReq = new HttpRequestMessage(HttpMethod.Post, url))
            {
                httpReq.Headers.Add("Authorization", $"Bearer {s.AccessToken}");

                var requestString = JsonConvert.SerializeObject(model);
                httpReq.Content = new StringContent(requestString, Encoding.UTF8, "application/json");
                using (var httpResponse = await client.SendAsync(httpReq))
                {
                    var responseString = await httpResponse.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(responseString))
                    {
                        return JsonConvert.DeserializeObject<TResult>(responseString);
                    }

                    return default;
                }
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            client.Dispose();
        }
    }
}