using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using serverServer.Domain;
using serverServer.Domain.Entities.Geoposition;
using serverServer.Exceptions;
using serverServer.Model;
using serverServer.Options;
using serverServer.ViewModels;

namespace TestInfrastructure
{
    public class SocketClient
    {
        public readonly Guid Id;

        private readonly Func<string, Task<WebSocket>> _connectFunc;

        public ClientType ClientType { get; }

        private readonly ConcurrentDictionary<Guid, Callback> _requests = new ConcurrentDictionary<Guid, Callback>();
        private readonly Dictionary<EventTypes, ManualResetEvent> _resetEvents;
        private readonly Dictionary<NotificationTypes, ManualResetEvent> _resetPushUpEvents;
        public readonly ConcurrentDictionary<ActionType, ConcurrentStack<Packet>> ResponcePackets = 
            new ConcurrentDictionary<ActionType, ConcurrentStack<Packet>>();

        public readonly ConcurrentBag<Tuple<EventTypes,dynamic>> Events =
            new ConcurrentBag<Tuple<EventTypes, dynamic>>();

        public readonly ConcurrentBag<Tuple<NotificationTypes, PushUpModel>> PushUps =
            new ConcurrentBag<Tuple<NotificationTypes, PushUpModel>>();

        private SocketClient()
        {
            Id = Guid.NewGuid();
            _resetEvents = new Dictionary<EventTypes, ManualResetEvent>();
            foreach (var e in Enum.GetValues(typeof(EventTypes)))
            {
                _resetEvents.Add((EventTypes)e, new ManualResetEvent(false));
            }

            _resetPushUpEvents = new Dictionary<NotificationTypes, ManualResetEvent>();
            foreach (var e in Enum.GetValues(typeof(NotificationTypes)))
            {
                _resetPushUpEvents.Add((NotificationTypes)e, new ManualResetEvent(false));
            }
        }

        public SocketClient(Func<string, Task<WebSocket>> connectFunc, ClientType clientType): this()
        {
            _connectFunc = connectFunc;
            ClientType = clientType;
        }

        public void Connect(string token = null)
        {
            var resetEvent = new ManualResetEvent(false);
            Exception error = null;
             _connectFunc(token).ContinueWith(r =>
            {
                if (r.Exception != null)
                {
                    error = r.Exception;
                    resetEvent.Set();
                    return;
                }
                Socket = r.Result;
                Task.Run(() => Receiver(Socket));
                resetEvent.Set();
            });
            resetEvent.WaitOne();
            if (error != null)
            {
                throw error;
            }
        }

        private async void Receiver(WebSocket socket)
        {
            try
            {
                var inputBuffer = new byte[4096];
                byte[] inputMsg;
                WebSocketReceiveResult result;
                using (MemoryStream outStream = new MemoryStream())
                {
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(inputBuffer, 0, inputBuffer.Length), CancellationToken.None);
                        outStream.Write(inputBuffer, 0, result.Count);
                    } while (!result.EndOfMessage);
                    inputMsg = outStream.ToArray();
                }
                while (!result.CloseStatus.HasValue)
                {
                    try
                    {
                        var packet = ParseMessage(inputMsg, result);
                        await Processed(packet);
                    }
                    catch (Exception ex)
                    {
                        await Socket.CloseOutputAsync(WebSocketCloseStatus.InvalidPayloadData, "Error parse packet",
                                CancellationToken.None);
                    }

                    using (var outStream = new MemoryStream())
                    {
                        do
                        {
                            result = await socket.ReceiveAsync(new ArraySegment<byte>(inputBuffer, 0, inputBuffer.Length), CancellationToken.None);
                            outStream.Write(inputBuffer, 0, result.Count);
                        } while (!result.EndOfMessage);
                        inputMsg = outStream.ToArray();
                    }
                }
                await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
            catch (Exception e)
            {
                //Disconnected?.Invoke(this);
            }
        }

        public async Task Send<T>(ActionType type, string action, T data, Callback callback)
        {
            var packet = Packet.CreateRequest(type, action, data);
            var bufferSend = Encoding.UTF8.GetBytes(packet.ToJson());
            if(_requests.TryAdd(packet.Id, callback))
            {
                await Socket.SendAsync(new ArraySegment<byte>(bufferSend), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public async Task Send<T>(ActionType type, string action, T data)
        {
            var packet = Packet.CreateRequest(type, action, data);
            var bufferSend = Encoding.UTF8.GetBytes(packet.ToJson());
            await Socket.SendAsync(new ArraySegment<byte>(bufferSend), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task SendBinary<T>(ActionType type, string action, T model, byte[] data, Callback callback)
        {
            var packet = Packet.CreateRequest(type, action, model);
            ArraySegment<byte> outputBuffer;
            using (MemoryStream outStream = new MemoryStream())
            {
                var buf = Encoding.UTF8.GetBytes(SocketServiceOptions.PacketKey);
                outStream.Write(buf, 0, buf.Length);
                buf = Encoding.UTF8.GetBytes(packet.ToJson());
                var lenPacket = BitConverter.GetBytes(buf.Length);
                outStream.Write(lenPacket, 0, lenPacket.Length);
                outStream.Write(buf, 0, buf.Length);
                outStream.Write(data, 0, data.Length);
                outputBuffer = new ArraySegment<byte>(outStream.ToArray());
            }
            if (_requests.TryAdd(packet.Id, callback))
            {
                await Socket.SendAsync(outputBuffer, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }

        public async Task<TRes> Invoke<T, TRes>(ActionType actionType, string method, T model, byte[] data)
        {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            Error error = null;
            dynamic result = null;
            await SendBinary(actionType, method, model, data,
                new Callback<TRes>(o =>
                {
                    result = o;
                    resetEvent.Set();
                }, e =>
                {
                    error = e;
                    resetEvent.Set();
                }));
            await Task.Run(() =>
            {
                resetEvent.WaitOne();
                resetEvent.Reset();
            });
            if (error == null)
            {
                return result;
            }
            throw new Exception(error.ToString());
        }

        public async Task<TRes> Invoke<T, TRes>(ActionType actionType, string method, T model)
        {
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            Error error = null;
            dynamic result = null;
            await Send(actionType, method, model,
                new Callback<TRes>(o =>
                {
                    result = o;
                    resetEvent.Set();
                }, e =>
                {
                    error = e;
                    resetEvent.Set();
                }));
            await Task.Run(() =>
            {
                resetEvent.WaitOne();
                resetEvent.Reset();
            });
            if (error == null)
            {
                return result;
            }
            throw new Exception(error.ToString());
        }

        public async Task Invoke<T>(ActionType actionType, string method, T model)
        {
            await Send(actionType, method, model);
        }

        private async Task Processed(Tuple<Packet, byte[]> packetAggregate)
        {
            var packet = packetAggregate.Item1;

            var responcePackets = ResponcePackets.GetOrAdd(packet.ActionType, new ConcurrentStack<Packet>());
            responcePackets.Push(packet);

            if (packet.PacketType == PacketType.Response && _requests.ContainsKey(packet.Id))
            {
                Callback callback;
                if (_requests.TryRemove(packet.Id, out callback))
                {
                    await callback.OnAction(packetAggregate);
                }
            }
            if (packet.PacketType == PacketType.Error)
            {
                Callback callback;
                if (_requests.ContainsKey(packet.Id) && _requests.TryRemove(packet.Id, out callback))
                {
                    await callback.OnError(packetAggregate);
                }
                else
                {
                    OnError(packetAggregate.Item1.Error);
                }
            }
            if (packet.IsEvent())
            {
                switch (packet.Action)
                {
                    case nameof(EventTypes.PoiSet):
                        OnSetPoi(packet);
                        break;
                    case nameof(EventTypes.PoiUpdate):
                        OnUpdatePoi(packet);
                        break;
                    case nameof(EventTypes.PoiDelete):
                        OnDeletePoi(packet);
                        break;
                    case nameof(EventTypes.PushUp):
                        OnPushUp(packet);
                        break;
                    default: OnOtherEvent(packet);
                        break;  
                }
            }
        }

        private Tuple<Packet, byte[]> ParseMessage(byte[] inputMsg, WebSocketReceiveResult receiveResult)
        {
            if (receiveResult.MessageType == WebSocketMessageType.Text)
            {
                var messageString = Encoding.UTF8.GetString(inputMsg);
                return Tuple.Create(Packet.Parse(messageString), new byte[0]);
            }
            if (receiveResult.MessageType == WebSocketMessageType.Binary)
            {
                Tuple<Packet, byte[]> result;
                using (var stream = new MemoryStream(inputMsg))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        int packetLen = reader.ReadInt32();
                        var packet = Encoding.UTF8.GetString(reader.ReadBytes(packetLen));

                        int length = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
                        var content = length != 0 ? reader.ReadBytes(length) : null;
                        result = Tuple.Create(Packet.Parse(packet), content);
                    }
                }
                return result;
            }
            throw new Exception("Incorrect message");
        }

        public async Task Disconnect()
        {
            try
            {
                await Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public bool IsConnected => Socket?.State == WebSocketState.Open;

        public WebSocket Socket { get; private set; }

        public ConcurrentDictionary<Guid, Poi> VisiblePoi = new ConcurrentDictionary<Guid, Poi>();

        private Poi[] PacketToPoi(Packet packet)
        {
            var jarray = packet.Data as JArray;
            Poi[] data = default(Poi[]);

            if (jarray != null)
            {
                data = jarray.ToObject<Poi[]>();
            }
            return data;
        }

        public Exception Exception = null;

        private void OnError(Error error)
        {
            foreach (var autoResetEvent in _resetEvents)
            {
                autoResetEvent.Value.Set();
            }
            foreach (var autoResetEvent in _resetPushUpEvents)
            {
                autoResetEvent.Value.Set();
            }
            Exception = new Exception(error.ToString());
        }

        public async Task WaitEvent(EventTypes type)
        {
            await Task.Run(() =>
            {
                _resetEvents[type].WaitOne(TimeSpan.FromSeconds(60));
                _resetEvents[type].Reset();
            });
            if (Exception != null)
            {
                throw Exception;
            }
        }

        public async Task WaitPushUp(NotificationTypes type)
        {
            await Task.Run(() =>
            {
                _resetPushUpEvents[type].WaitOne(TimeSpan.FromSeconds(60));
                _resetPushUpEvents[type].Reset();
            });
            if (Exception != null)
            {
                throw Exception;
            }
        }

        protected void OnSetPoi(Packet packet)
        {
            var pois = PacketToPoi(packet);
            VisiblePoi.Clear();
            foreach (var poi in pois)
            {
                VisiblePoi.TryAdd(poi.Id, poi);
            }
            _resetEvents[EventTypes.PoiSet].Set();
        }

        protected void OnUpdatePoi(Packet packet)
        {
            var pois = PacketToPoi(packet);
            foreach (var poi in pois)
            {
                VisiblePoi.AddOrUpdate(poi.Id, poi, (guid, oldPoi) => poi);
            }
            _resetEvents[EventTypes.PoiUpdate].Set();
        }

        protected void OnDeletePoi(Packet packet)
        {
            var pois = PacketToPoi(packet);
            foreach (var poi in pois)
            {
                Poi removedPoi;
                VisiblePoi.TryRemove(poi.Id,out removedPoi);
            }
            _resetEvents[EventTypes.PoiDelete].Set();
        }

        protected virtual void OnAddedBid(Packet packet)
        {
            var jObject = packet.Data as JObject;
            dynamic data = default(dynamic);

            if (jObject != null)
            {
                data = jObject.ToObject<dynamic>();
            }
            Events.Add(Tuple.Create(EventTypes.TripAddedBid, (object)data));
            _resetEvents[EventTypes.TripAddedBid].Set();
        }

        protected virtual void OnRemovedBid(Packet packet)
        {
            var jObject = packet.Data as JObject;
            dynamic data = default(dynamic);

            if (jObject != null)
            {
                data = jObject.ToObject<dynamic>();
            }
            Events.Add(Tuple.Create(EventTypes.TripRemovedBid, (object)data));
            _resetEvents[EventTypes.TripRemovedBid].Set();
        }

        protected virtual void OnOtherEvent(Packet packet)
        {
            var jObject = packet.Data as JObject;
            dynamic data = default(dynamic);

            if (jObject != null)
            {
                data = jObject.ToObject<dynamic>();
            }
            EventTypes eventTypes;
            if (Enum.TryParse(packet.Action, out eventTypes))
            {
                Events.Add(Tuple.Create(eventTypes, (object)data));
                _resetEvents[eventTypes].Set();
            }
            
        }

        protected virtual void OnPushUp(Packet packet)
        {
            var jObject = packet.Data as JObject;

            if (jObject != null)
            {
                var data = jObject.ToObject<PushUpModel>();
                NotificationTypes eventTypes;
                var index = data.Category.IndexOf("#", StringComparison.Ordinal);
                var category = data.Category.Substring(0, index == -1 ? data.Category.Length: index);
                if (Enum.TryParse(category, out eventTypes))
                {
                    PushUps.Add(Tuple.Create(eventTypes, data));
                    _resetPushUpEvents[eventTypes].Set();
                }
            }
            _resetEvents[EventTypes.PushUp].Set();
        }

        public override bool Equals(object obj)
        {
            var other = obj as SocketClient;
            return other != null && Equals(Id, other.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}