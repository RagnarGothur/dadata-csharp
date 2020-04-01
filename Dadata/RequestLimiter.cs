using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;

public interface IRequestExecutor
{
    HttpWebResponse Execute(HttpWebRequest request);
    HttpWebResponse ExecuteWithMaxReqPerSecond(HttpWebRequest request, uint maxReqPerSecond);
}

namespace RequestLimiter
{
    internal struct Key
    {
        private string _method
        { get; }

        private string _entity
        { get; }

        internal Key(string method, string entity)
        {
            this._method = method;
            this._entity = entity;
        }
    }

    //TODO: move it to another project
    public class RequestLimiter
    {
        private const uint defaultLimit = 20;

        private readonly Mutex mutex = new Mutex();
        private readonly Queue<DateTime> expirations = new Queue<DateTime>();

        public void WaitForAllowing(uint maxReqPerSecond = defaultLimit)
        {
            mutex.WaitOne();

            //Remove expired info
            while (expirations.Count > 0 && ((DateTime.Now - expirations.Peek()) > TimeSpan.FromSeconds(1)))
            {
                expirations.Dequeue();
            }

            //Check whether there is place for another one
            if (expirations.Count < maxReqPerSecond)
            {
                expirations.Enqueue(DateTime.Now);
            }
            else
            {
                Thread.Sleep(expirations.Peek() - DateTime.Now);
                WaitForAllowing(maxReqPerSecond);
            }

            mutex.ReleaseMutex();
        }
    }

    public class DefaultExecutor : IRequestExecutor
    {
        private static readonly Lazy<DefaultExecutor> instanceHolder =
            new Lazy<DefaultExecutor>(() => new DefaultExecutor(), LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly Lazy<Dictionary<Key, RequestLimiter>> requestLimitGuard =
            new Lazy<Dictionary<Key, RequestLimiter>>(() => new Dictionary<Key, RequestLimiter>(), LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly Mutex mutex = new Mutex();

        public static DefaultExecutor Instance
        {
            get { return instanceHolder.Value; }
        }

        public HttpWebResponse Execute(HttpWebRequest request)
        {
            RequestLimiter requestLimiter = GetRequestLimiter(request);

            //Blocks thread until another request is allowed
            requestLimiter.WaitForAllowing();
            return (HttpWebResponse)request.GetResponse();
        }

        public HttpWebResponse ExecuteWithMaxReqPerSecond(HttpWebRequest request, uint maxReqPerSecond)
        {
            if (maxReqPerSecond == 0) throw new ArgumentException();

            RequestLimiter requestLimiter = GetRequestLimiter(request);

            //Blocks thread until another request is allowed
            requestLimiter.WaitForAllowing(maxReqPerSecond);
            return this.Execute(request);
        }

        internal static RequestLimiter GetRequestLimiter(HttpWebRequest request)
        {
            var uriParts = request.RequestUri.AbsoluteUri.Split('/');
            string entity = uriParts[uriParts.Length - 1];
            string method = uriParts[uriParts.Length - 2];

            Key key = new Key(method, entity);

            mutex.WaitOne();
            RequestLimiter requestLimiter;
            try
            {
                requestLimiter = DefaultExecutor.requestLimitGuard.Value[key];
            }
            //Fill requestLimitGuard with requestLimiter if it not exists
            //Maybe should be in another function
            catch (KeyNotFoundException)
            {
                requestLimiter = new RequestLimiter();
                DefaultExecutor.requestLimitGuard.Value.Add(key, requestLimiter);
            }
            mutex.ReleaseMutex();

            return requestLimiter;
        }
    }
}
