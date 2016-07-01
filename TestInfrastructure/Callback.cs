using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Server.Exceptions;
using Server.Model;

namespace TestInfrastructure
{
    public class Callback<T> : Callback
    {
        private readonly Action<T> _callback;
        private readonly Action<T, byte[]> _callbackContent;

        private Callback(Action<Error> callbackError): base(callbackError)
        {
        }

        public Callback(Action<T> callback, Action<Error> callbackError) : this(callbackError)
        {
            _callback = callback;
        }

        public Callback(Action<T, byte[]> callback, Action<Error> callbackError) : this(callbackError)
        {
            _callbackContent = callback;
        }

        public override async Task OnAction(Tuple<Packet, byte[]> packet)
        {
            var data = ParseData(packet);

            if (_callback != null)
            {
                await Task.Run(()=> _callback(data));
            }
            else if (_callbackContent != null)
            {
                await Task.Run(() => _callbackContent(data, packet.Item2));
            }
        }

        private static T ParseData(Tuple<Packet, byte[]> packet)
        {
            var jdata = packet.Item1.Data as JObject;
            T data = default(T);

            if (jdata != null)
            {
                data = jdata.ToObject<T>();
                return data;
            }

            var jArray = packet.Item1.Data as JArray;
            if (jArray != null)
            {
                data = jArray.ToObject<T>();
                return data;
            }

            if (packet.Item1.Data is T)
            {
                data = (T) packet.Item1.Data;
                return data;
            }

            if (packet.Item1.Data is long && typeof (T) == typeof (int))
            {
                data = (T) ((object) Convert.ToInt32(packet.Item1.Data));
                return data;
            }

            return data;
        }
    }

    public abstract class Callback
    {
        //protected readonly Action _callback;
        //protected readonly Action<byte[]> _callbackContent;
        protected readonly Action<Error> CallbackError;

        protected Callback(Action<Error> callbackError)
        {
            CallbackError = callbackError;
        }

        public abstract Task OnAction(Tuple<Packet, byte[]> packet);

        public virtual async Task OnError(Tuple<Packet, byte[]> packet)
        {
            if (CallbackError != null)
            {
                await Task.Run(() => CallbackError(packet.Item1.Error));
            }
        }
    }
}