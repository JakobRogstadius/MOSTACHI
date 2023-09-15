using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Commons
{
    public interface IAsyncWriter
    {
        void Push(params object[] values);
        void WriteAsync();
        void Stop();
    }

    public class AsyncBinaryWriter : IAsyncWriter, IDisposable
    {
        object _writeLock = new object();
        BinaryWriter _writer;
        bool _stop = false;
        readonly ConcurrentQueue<object> _writeQueue = new ConcurrentQueue<object>();

        public AsyncBinaryWriter(string filePath)
        {
            _writer = new BinaryWriter(File.Open(filePath, FileMode.Create));
        }

        public void Push(params object[] values)
        {
            lock (_writeLock)
            {
                foreach (var value in values)
                {
                    _writeQueue.Enqueue(value);
                }
            }
            while (_writeQueue.Count > 100000)
                Thread.Sleep(100);
        }

        public void Stop()
        {
            _stop = true;
        }

        public void WriteAsync()
        {
            while (!(_stop && _writeQueue.IsEmpty))
            {
                while (_writeQueue.TryDequeue(out object? obj))
                {
                    if (obj is long l)
                        _writer.Write(l);
                    else if (obj is double d)
                        _writer.Write(d);
                    else if (obj is float f)
                        _writer.Write(f);
                    else if (obj is int i)
                        _writer.Write(i);
                    else if (obj is string s)
                        _writer.Write(s);
                    else if (obj is bool b)
                        _writer.Write(b);
                    else if (obj is IEnumerable<int> arr0)
                    {
                        _writer.Write(arr0.Count());
                        foreach (int val in arr0)
                            _writer.Write(val);
                    }
                    else if (obj is IEnumerable<long> arr1)
                    {
                        _writer.Write(arr1.Count());
                        foreach (long val in arr1)
                            _writer.Write(val);
                    }
                    else if (obj is IEnumerable<(int, int)> arr2)
                    {
                        _writer.Write(arr2.Count());
                        foreach (var pair in arr2)
                        {
                            _writer.Write(pair.Item1);
                            _writer.Write(pair.Item2);
                        }
                    }
                    else if (obj is IEnumerable<(long, long)> arr3)
                    {
                        _writer.Write(arr3.Count());
                        foreach (var pair in arr3)
                        {
                            _writer.Write(pair.Item1);
                            _writer.Write(pair.Item2);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unhandled value type in DataWriter");
                        throw new NotImplementedException();
                    }
                }
                _writer.Flush();
                Thread.Sleep(100);
            }
        }

        public void Dispose()
        {
            _writer.Close();
            _writer.Dispose();
        }
    }
}
