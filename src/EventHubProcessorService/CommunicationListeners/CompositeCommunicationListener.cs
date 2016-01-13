using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;

namespace EventHubProcessorService
{
    /// <summary>
    /// a composite listener is an implementation of ICommunicationListener
    /// surfaced to Service Fabric as one listener but can be a # of 
    /// _listeners grouped together. Supports adding _listeners even after OpenAsync()
    /// has been called for the listener
    /// </summary>
    public class CompositeCommunicationListener : ICommunicationListener
    {
        private readonly Dictionary<string, ICommunicationListener> _listeners = new Dictionary<string, ICommunicationListener>();
        private readonly Dictionary<string, CommunicationListenerStatus> _statuses = new Dictionary<string, CommunicationListenerStatus>();
        private readonly AutoResetEvent _listenerLock = new AutoResetEvent(true);

        public CompositeCommunicationListener() : this(null)
        {
        }

        public CompositeCommunicationListener(Dictionary<string, ICommunicationListener> listeners)
        {
            if (null == listeners) return;

            foreach (var listener in listeners)
            {
                _listeners.Add(listener.Key, listener.Value);
                _statuses.Add(listener.Key, CommunicationListenerStatus.Closed);
            }
        }

        public Func<CompositeCommunicationListener, Dictionary<string, string>, string> OnCreateListeningAddress { get; set; }

        public async Task ClearAll()
        {
            foreach (var key in _listeners.Keys)
            {
                await RemoveListenerAsync(key);
            }
        }

        public CommunicationListenerStatus Status { get; private set; } = CommunicationListenerStatus.Closed;

        public void Abort()
        {
            try
            {
                _listenerLock.WaitOne();

                Status = CommunicationListenerStatus.Aborting;
                foreach (var listener in _listeners)
                {
                    AbortListener(listener.Key, listener.Value);
                }

                Status = CommunicationListenerStatus.Aborted;
            }
            finally
            {
                _listenerLock.Set();
            }
        }

        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            try
            {
                _listenerLock.WaitOne();
                Status = CommunicationListenerStatus.Closing;

                var tasks = _listeners.Select(listener => CloseListener(listener.Key, listener.Value, cancellationToken)).ToList();

                await Task.WhenAll(tasks);
                Status = CommunicationListenerStatus.Closed;
            }
            finally
            {
                _listenerLock.Set();
            }
        }

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                ValidateListeners();

                _listenerLock.WaitOne();

                Status = CommunicationListenerStatus.Opening;

                var tasks = _listeners.Select(listener => Task.Run(async () =>
                {
                    await OpenListener(listener.Key, listener.Value, cancellationToken);
                }, cancellationToken)).ToList();

                await Task.WhenAll(tasks);
                
                InitializeOnCreateListeningAddress();
                Status = CommunicationListenerStatus.Opened;
                return string.Empty;
            }
            finally
            {
                _listenerLock.Set();
            }
        }


        public CommunicationListenerStatus GetListenerStatus(string listenerName)
        {
            if (!_statuses.ContainsKey(listenerName))
            {
                throw new InvalidOperationException($"Listener with the name {listenerName} does not exist");
            }

            return _statuses[listenerName];
        }

        public async Task AddListenerAsync(string listenerName, ICommunicationListener listener)
        {
            try
            {
                if (null == listener)
                {
                    throw new ArgumentNullException(nameof(listener));
                }

                if (_listeners.ContainsKey(listenerName))
                {
                    throw new InvalidOperationException($"Listener with the name {listenerName} already exists");
                }


                _listenerLock.WaitOne();

                _listeners.Add(listenerName, listener);
                _statuses.Add(listenerName, CommunicationListenerStatus.Closed);
                
                if (CommunicationListenerStatus.Opened == Status)
                {
                    await OpenListener(listenerName, listener, CancellationToken.None);
                }
            }
            finally
            {
                _listenerLock.Set();
            }
        }

        public async Task RemoveListenerAsync(string listenerName)
        {
            ICommunicationListener listener = null;

            try
            {
                if (!_listeners.ContainsKey(listenerName))
                {
                    throw new InvalidOperationException($"Listener with the name {listenerName} does not exists");
                }

                listener = _listeners[listenerName];


                _listenerLock.WaitOne();
                await CloseListener(listenerName, listener, CancellationToken.None);
            }
            catch (AggregateException)
            {
                if (null != listener)
                {
                    try
                    {
                        listener.Abort();
                    }
                    catch
                    {
                        /*no op*/
                    }
                }
            }
            finally
            {
                _listeners.Remove(listenerName);
                _statuses.Remove(listenerName);

                _listenerLock.Set();
            }
        }

        private void InitializeOnCreateListeningAddress()
        {
            if (null == OnCreateListeningAddress)
            {
                OnCreateListeningAddress = (listener, addresses) =>
                {
                    var sb = new StringBuilder();
                    foreach (var address in addresses.Values)
                    {
                        sb.Append(string.Concat(address, ";"));
                    }

                    return sb.ToString();
                };
            }
        }

        private void ValidateListeners()
        {
            if (_listeners.Any(kvp => null == kvp.Value))
            {
                throw new InvalidOperationException("can not have null _listeners");
            }
        }

        private async Task<string> OpenListener(
            string listenerName,
            ICommunicationListener listener,
            CancellationToken canceltoken)
        {
            _statuses[listenerName] = CommunicationListenerStatus.Opening;
            var address = await listener.OpenAsync(canceltoken);
            _statuses[listenerName] = CommunicationListenerStatus.Opened;

            return address;
        }
        
        private async Task CloseListener(
            string listenerName,
            ICommunicationListener listener,
            CancellationToken cancelToken)
        {
            _statuses[listenerName] = CommunicationListenerStatus.Closing;
            await listener.CloseAsync(cancelToken);
            _statuses[listenerName] = CommunicationListenerStatus.Closed;
        }

        private void AbortListener(
            string listenerName,
            ICommunicationListener listener)
        {
            _statuses[listenerName] = CommunicationListenerStatus.Aborting;
            listener.Abort();
            _statuses[listenerName] = CommunicationListenerStatus.Aborted;
        }
    }
}