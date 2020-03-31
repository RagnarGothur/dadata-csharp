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
        private string Method
        { get; }

        private string Entity
        { get; }

        internal Key(string method, string entity)
        {
            this.Method = method;
            this.Entity = entity;
        }
    }

    //TODO: move it to another project
    public class OperationsLimiter
    {
        private const uint defaultLimit = 20;

        private readonly Mutex Mutex = new Mutex();
        private readonly Queue<DateTime> Expirations = new Queue<DateTime>();

        public void WaitForAllowing(uint maxReqPerSecond = defaultLimit)
        {
            Mutex.WaitOne();

            //Remove expired info
            while (Expirations.Count > 0 && ((DateTime.Now - Expirations.Peek()) > TimeSpan.FromSeconds(1)))
            {
                Expirations.Dequeue();
            }

            //Check whether there is place for another one
            if (Expirations.Count < maxReqPerSecond)
            {
                Expirations.Enqueue(DateTime.Now);
            }
            else
            {
                Thread.Sleep(Expirations.Peek() - DateTime.Now);
                WaitForAllowing(maxReqPerSecond);
            }

            Mutex.ReleaseMutex();
        }
    }

    public class DefaultExecutor : IRequestExecutor
    {
        private static readonly Dictionary<Key, OperationsLimiter> requestLimitGuard;
        private static readonly Mutex mutex = new Mutex();

        static DefaultExecutor()
        {
            requestLimitGuard = new Dictionary<Key, OperationsLimiter>();
        }

        public HttpWebResponse Execute(HttpWebRequest request)
        {
            OperationsLimiter requestLimiter = GetRequestLimiter(request);
            //Blocks thread until another request is allowed
            requestLimiter.WaitForAllowing();
            return (HttpWebResponse)request.GetResponse();
        }

        public HttpWebResponse ExecuteWithMaxReqPerSecond(HttpWebRequest request, uint maxReqPerSecond)
        {
            if (maxReqPerSecond == 0) throw new ArgumentException();

            OperationsLimiter requestLimiter = GetRequestLimiter(request);

            //Blocks thread until another request is allowed
            requestLimiter.WaitForAllowing(maxReqPerSecond);
            return this.Execute(request);
        }

        internal static OperationsLimiter GetRequestLimiter(HttpWebRequest request)
        {
            var uriParts = request.RequestUri.AbsoluteUri.Split('/');
            string entity = uriParts[uriParts.Length - 1];
            string method = uriParts[uriParts.Length - 2];

            Key key = new Key(method, entity);

            mutex.WaitOne();
            OperationsLimiter requestLimiter;
            try
            {
                requestLimiter = DefaultExecutor.requestLimitGuard[key];
            }
            //Fill requestLimitGuard with requestLimiter if it not exists
            //Maybe should be in another function
            catch (KeyNotFoundException)
            {
                requestLimiter = new OperationsLimiter();
                DefaultExecutor.requestLimitGuard.Add(key, requestLimiter);
            }
            mutex.ReleaseMutex();

            return requestLimiter;
        }
    }
}
