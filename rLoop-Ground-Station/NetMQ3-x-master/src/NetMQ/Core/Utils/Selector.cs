﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;


namespace NetMQ.Core.Utils
{
    /// <summary>
    /// A SelectItem is a pairing of a (Socket or SocketBase) and a PollEvents value.
    /// </summary>
    internal sealed class SelectItem
    {
        public SelectItem(SocketBase socket, PollEvents @event)
        {
            Socket = socket;
            Event = @event;
        }

        public SelectItem(Socket fileDescriptor, PollEvents @event)
        {
            FileDescriptor = fileDescriptor;
            Event = @event;
        }

        public Socket FileDescriptor { get; private set; }

        public SocketBase Socket { get; private set; }

        public PollEvents Event { get; private set; }

        public PollEvents ResultEvent { get; set; }
    }

    /// <summary>
    /// A Selector holds three lists of Sockets - for read, write, and error,
    /// and provides a Select method.
    /// </summary>
    internal sealed class Selector
    {
        private readonly List<Socket> m_checkRead = new List<Socket>();
        private readonly List<Socket> m_checkWrite = new List<Socket>();
        private readonly List<Socket> m_checkError = new List<Socket>();

        /// <summary>
        /// </summary>
        /// <param name="items">  (must not be null)</param>
        /// <param name="itemsCount"></param>
        /// <param name="timeout">a time-out period, in milliseconds</param>
        /// <returns></returns>
        /// <exception cref="FaultException">The internal select operation failed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
        /// <exception cref="TerminatingException">The socket has been stopped.</exception>
        public bool Select(SelectItem[] items, int itemsCount, long timeout)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            if (itemsCount == 0)
            {
                return false;
            }

            bool firstPass = true;
            int numberOfEvents = 0;

            Stopwatch stopwatch = null;

            while (true)
            {
                long currentTimeoutMicroSeconds;

                if (firstPass)
                {
                    currentTimeoutMicroSeconds = 0;
                }
                else if (timeout < 0)
                {
                    // Consider everything below 0 to be infinite
                    currentTimeoutMicroSeconds = -1;
                }
                else
                {
                    currentTimeoutMicroSeconds = (timeout - stopwatch.ElapsedMilliseconds) * 1000;

                    if (currentTimeoutMicroSeconds < 0)
                    {
                        currentTimeoutMicroSeconds = 0;
                    }
                    else if (currentTimeoutMicroSeconds > int.MaxValue)
                    {
                        currentTimeoutMicroSeconds = int.MaxValue;
                    }
                }

                m_checkRead.Clear();
                m_checkWrite.Clear();
                m_checkError.Clear();

                for (int i = 0; i < itemsCount; i++)
                {
                    var pollItem = items[i];

                    if (pollItem.Socket != null)
                    {
                        if (pollItem.Event != PollEvents.None && pollItem.Socket.Handle.Connected)
                            m_checkRead.Add(pollItem.Socket.Handle);
                    }
                    else
                    {
                        if (pollItem.Event.HasIn())
                            m_checkRead.Add(pollItem.FileDescriptor);

                        if (pollItem.Event.HasOut())
                            m_checkWrite.Add(pollItem.FileDescriptor);
                    }
                }

                try
                {
                    SocketUtility.Select(m_checkRead, m_checkWrite, m_checkError, (int)currentTimeoutMicroSeconds);
                }
                catch (SocketException x)
                {
#if DEBUG
                    string textOfListRead = StringLib.AsString(m_checkRead);
                    string textOfListWrite = StringLib.AsString(m_checkWrite);
                    string textOfListError = StringLib.AsString(m_checkError);
                    string xMsg = string.Format("In Selector.Select, Socket.Select({0}, {1}, {2}, {3}) threw a SocketException: {4}", textOfListRead, textOfListWrite, textOfListError, currentTimeoutMicroSeconds, x.Message);
                    Debug.WriteLine(xMsg);
                    throw new FaultException(innerException: x, message: xMsg);
#else
                    throw new FaultException(innerException: x, message: "Within SocketUtility.Select");
#endif
                }

                for (int i = 0; i < itemsCount; i++)
                {
                    var selectItem = items[i];

                    selectItem.ResultEvent = PollEvents.None;

                    if (selectItem.Socket != null)
                    {
                        var events = (PollEvents)selectItem.Socket.GetSocketOption(ZmqSocketOption.Events);

                        if (selectItem.Event.HasIn() && events.HasIn())
                            selectItem.ResultEvent |= PollEvents.PollIn;

                        if (selectItem.Event.HasOut() && events.HasOut())
                            selectItem.ResultEvent |= PollEvents.PollOut;
                    }
                    else
                    {
                        if (m_checkRead.Contains(selectItem.FileDescriptor))
                            selectItem.ResultEvent |= PollEvents.PollIn;

                        if (m_checkWrite.Contains(selectItem.FileDescriptor))
                            selectItem.ResultEvent |= PollEvents.PollOut;
                    }

                    if (selectItem.ResultEvent != PollEvents.None)
                        numberOfEvents++;
                }

                if (timeout == 0)
                    break;

                if (numberOfEvents > 0)
                    break;

                if (timeout < 0)
                {
                    if (firstPass)
                        firstPass = false;

                    continue;
                }

                if (firstPass)
                {
                    stopwatch = Stopwatch.StartNew();
                    firstPass = false;
                    continue;
                }

                // Check also equality as it might frequently occur on 1000Hz clock
                if (stopwatch.ElapsedMilliseconds >= timeout)
                    break;
            }

            return numberOfEvents > 0;
        }
    }
}
