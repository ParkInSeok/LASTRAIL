using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Networking;

namespace LastRail.Run
{
    public sealed class UnityWebRequestRunApiHttpAdapter : IUnityWebRequestAdapter, IDisposable
    {
        private readonly string baseUrl;
        private readonly int timeoutSeconds;
        private readonly HashSet<UnityWebRequest> requests=new HashSet<UnityWebRequest>();
        private readonly int ownerThread=Thread.CurrentThread.ManagedThreadId;
        private bool disposed;
        public int InFlightCount=>requests.Count;

        public UnityWebRequestRunApiHttpAdapter(string baseUrl) : this(baseUrl, 0) { }

        public UnityWebRequestRunApiHttpAdapter(string baseUrl, int timeoutSeconds)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("Base URL cannot be empty.", nameof(baseUrl));
            }

            this.baseUrl = baseUrl.TrimEnd('/');
            if (timeoutSeconds < 0 || timeoutSeconds > 300) throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            this.timeoutSeconds = timeoutSeconds;
        }

        public async Task<RunApiHttpResponse> SendAsync(UnityWebRequestTransportRequest request)
        {
            if(disposed)throw new ObjectDisposedException(nameof(UnityWebRequestRunApiHttpAdapter));
            if(Thread.CurrentThread.ManagedThreadId!=ownerThread)throw new InvalidOperationException("Unity HTTP owner thread required");
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using (var unityRequest = BuildRequest(request))
            {
                requests.Add(unityRequest);
                UnityWebRequestAsyncOperation operation = null;
                var completion = new TaskCompletionSource<bool>();
                Action<UnityEngine.AsyncOperation> completed=_=>completion.TrySetResult(true);
                try {
                operation = unityRequest.SendWebRequest();
                operation.completed += completed;
                if(operation.isDone)completion.TrySetResult(true);
                await completion.Task;
                if(disposed)throw new ObjectDisposedException(nameof(UnityWebRequestRunApiHttpAdapter));

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

                return RunApiHttpResponse.Success((int)unityRequest.responseCode, body,
                    unityRequest.GetResponseHeaders()?.Select(h=>new RunApiHttpHeader(h.Key,h.Value)));
                } finally {if(operation!=null)operation.completed-=completed;requests.Remove(unityRequest);}
            }
        }

        public void Dispose()
        {
            if(Thread.CurrentThread.ManagedThreadId!=ownerThread)throw new InvalidOperationException("Unity HTTP owner thread required");
            if(disposed)return;disposed=true;
            foreach(var request in requests.ToArray())request.Abort();
        }

        private UnityWebRequest BuildRequest(UnityWebRequestTransportRequest request)
        {
            var unityRequest = new UnityWebRequest(baseUrl + request.EndpointPath, request.Method);
            unityRequest.timeout = timeoutSeconds;
            unityRequest.redirectLimit = 0;
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
