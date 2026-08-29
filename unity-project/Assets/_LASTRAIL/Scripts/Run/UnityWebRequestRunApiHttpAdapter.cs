using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace LastRail.Run
{
    public sealed class UnityWebRequestRunApiHttpAdapter : IUnityWebRequestAdapter
    {
        private readonly string baseUrl;

        public UnityWebRequestRunApiHttpAdapter(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("Base URL cannot be empty.", nameof(baseUrl));
            }

            this.baseUrl = baseUrl.TrimEnd('/');
        }

        public async Task<RunApiHttpResponse> SendAsync(UnityWebRequestTransportRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using (var unityRequest = BuildRequest(request))
            {
                var operation = unityRequest.SendWebRequest();
                var completion = new TaskCompletionSource<bool>();
                operation.completed += _ => completion.TrySetResult(true);
                await completion.Task;

                if (unityRequest.result == UnityWebRequest.Result.ConnectionError
                    || unityRequest.result == UnityWebRequest.Result.DataProcessingError)
                {
                    return RunApiHttpResponse.NetworkError(string.IsNullOrWhiteSpace(unityRequest.error) ? "UnityWebRequest failed." : unityRequest.error);
                }

                var body = unityRequest.downloadHandler == null ? "{}" : unityRequest.downloadHandler.text;
                if (string.IsNullOrWhiteSpace(body))
                {
                    body = "{}";
                }

                return RunApiHttpResponse.Success((int)unityRequest.responseCode, body);
            }
        }

        private UnityWebRequest BuildRequest(UnityWebRequestTransportRequest request)
        {
            var unityRequest = new UnityWebRequest(baseUrl + request.EndpointPath, request.Method);
            unityRequest.downloadHandler = new DownloadHandlerBuffer();

            if (!string.IsNullOrWhiteSpace(request.BodyJson))
            {
                unityRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(request.BodyJson));
                unityRequest.SetRequestHeader("Content-Type", "application/json");
            }

            foreach (var header in request.Headers)
            {
                unityRequest.SetRequestHeader(header.Name, header.Value);
            }

            return unityRequest;
        }
    }
}
