// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class HttpClientUtility
    {
        private static readonly HttpClient s_httpClient = new HttpClient();

        public static async Task<HttpResponseMessage> GetAsync(string requestUri, Config config, EntityTagHeaderValue etag = null)
        {
            return await RetryUtility.Retry(
                () =>
                {
                    // Create new instance of HttpRequestMessage to avoid System.InvalidOperationException:
                    // "The request message was already sent. Cannot send the same request message multiple times."
                    var message = CreateHttpRequestMessage(requestUri, config);
                    message.Method = HttpMethod.Get;
                    if (etag != null)
                    {
                        message.Headers.IfNoneMatch.Add(etag);
                    }
                    return s_httpClient.SendAsync(message);
                },
                NeedRetry);

            bool NeedRetry(Exception ex)
            {
                return ex is HttpRequestException
                    || ex is TimeoutException
                    || ex is OperationCanceledException;
            }
        }

        public static Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, Config config, EntityTagHeaderValue etag = null)
        {
            var message = CreateHttpRequestMessage(requestUri, config);
            message.Method = HttpMethod.Put;
            message.Content = content;
            if (etag != null)
            {
                message.Headers.IfMatch.Add(etag);
            }
            return s_httpClient.SendAsync(message);
        }

        private static HttpRequestMessage CreateHttpRequestMessage(string requestUri, Config config)
        {
            var message = new HttpRequestMessage();

            foreach (var (baseUrl, rule) in config.Http)
            {
                if (requestUri.StartsWith(baseUrl))
                {
                    // TODO: merge query if requestUri also contains query
                    message.RequestUri = new Uri(requestUri + rule.Query);
                    foreach (var header in rule.Headers)
                    {
                        message.Headers.Add(header.Key, header.Value);
                    }
                    return message;
                }
            }

            message.RequestUri = new Uri(requestUri);
            return message;
        }
    }
}