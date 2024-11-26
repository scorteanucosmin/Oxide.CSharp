extern alias References;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Oxide.CSharp.Common;

namespace Oxide.CSharp.CompilerStream
{
    internal class MessageBrokerService : IDisposable
    {
        private readonly Stream _input;

        private readonly Stream _output;

        private readonly Thread _workerThread;

        private readonly Queue<CompilerMessage> _messageQueue;

        private readonly Pooling.IArrayPool<byte> _pool;

        private volatile bool _running;

        private bool _disposed;

        private int _messageId;

        public event Action<CompilerMessage> OnMessageReceived;

        public MessageBrokerService(Stream input, Stream output)
        {
            _messageQueue = new Queue<CompilerMessage>();
            _pool = Pooling.ArrayPool<byte>.Shared;
            _input = input;
            _output = output;

            _running = true;

            _workerThread = new Thread(WorkerMethod)
            {
                CurrentCulture = Thread.CurrentThread.CurrentCulture,
                CurrentUICulture = Thread.CurrentThread.CurrentUICulture,
                Name = $"{GetType().FullName}+{Environment.TickCount}",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };

            _workerThread.Start();
        }

        public void SendMessage(CompilerMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            /*lock (_messageQueue)
            {
                _messageQueue.Enqueue(message);
            }*/

            WriteMessage(message);
        }

        private void WriteMessage(CompilerMessage message)
        {
            byte[] sourceArray = Constants.Serializer.Serialize(message);
            byte[] numArray = _pool.Take(sourceArray.Length + 4);
            try
            {
                int destinationIndex = sourceArray.Length.WriteBigEndian(numArray);
                Array.Copy(sourceArray, 0, numArray, destinationIndex, sourceArray.Length);
                _input.Write(numArray, 0, numArray.Length);
            }
            finally
            {
                _pool.Return(numArray);
            }
        }

        private CompilerMessage ReadMessage()
        {
            byte[] numArray1 = _pool.Take(4);
            int index1 = 0;
            try
            {
                while (index1 < numArray1.Length)
                {
                    index1 += _output.Read(numArray1, index1, numArray1.Length - index1);
                    if (index1 == 0)
                    {
                        return default;
                    }
                }

                int length = numArray1.ReadBigEndian();
                byte[] numArray2 = _pool.Take(length);
                int index2 = 0;
                try
                {
                    while (index2 < length)
                    {
                        index2 += _output.Read(numArray2, index2, length - index2);
                    }

                    return Constants.Serializer.Deserialize<CompilerMessage>(numArray2);
                }
                finally
                {
                    _pool.Return(numArray2);
                }
            }
            finally
            {
                _pool.Return(numArray1);
            }
        }

        private void WorkerMethod()
        {
            while (_running)
            {
                bool processed = false;
                lock (_messageQueue)
                {
                    for (int index = 0; index < 3; ++index)
                    {
                        if (_messageQueue.Count != 0)
                        {
                            WriteMessage(_messageQueue.Dequeue());
                            processed = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (OnMessageReceived != null)
                {
                    for (int index = 0; index < 3; ++index)
                    {
                        CompilerMessage message = ReadMessage();
                        if (message != null)
                        {
                            OnMessageReceived(message);
                            processed = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (!processed)
                {
                    Thread.Sleep(500);
                }
            }
        }

        public int SendShutdownMessage()
        {
            CompilerMessage message = new CompilerMessage
            {
                Id = _messageId++,
                Type = MessageType.Shutdown,
            };

            SendMessage(message);
            return message.Id;
        }

        public int SendCompileMessage(CompilerData project)
        {
            CompilerMessage message = new CompilerMessage
            {
                Id = _messageId++,
                Type = MessageType.Data,
                Data = Constants.Serializer.Serialize(project)
            };

            SendMessage(message);
            return message.Id;
        }

        public void Stop() => Dispose(true);

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _running = false;
            _disposed = true;
            if (disposing)
            {
                _messageQueue.Clear();
            }

            try
            {
                if (_workerThread.Join(10000))
                {
                    return;
                }
                _workerThread.Abort();
            }
            catch
            {

            }
        }

        ~MessageBrokerService() => Dispose(disposing: false);

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
