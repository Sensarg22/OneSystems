using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Server.Model;

namespace TestInfrastructure.ServiceProxy
{
    public static class ServiceManager
    {
        public static ServiceManager<T> Service<T>(this SocketClient socketClient)
        {
            return new ServiceManager<T>(socketClient);
        }
    }

    public class ServiceManager<T>
    {
        private readonly SocketClient _socketClient;

        public ServiceManager(SocketClient socketClient)
        {
            _socketClient = socketClient;
        }

        

        public async Task<TRes> Invoke<TRes>(ActionType actionType, Expression<Func<T, Task<TRes>>> action)
        {
            object val;
            byte[] data;
            var methodName = MethodName(actionType, (MethodCallExpression)action.Body, out val, out data);

            return data == null
                ? await _socketClient.Invoke<object, TRes>(actionType, methodName, val)
                : await _socketClient.Invoke<object, TRes>(actionType, methodName, val, data);
        }

        public async Task Invoke(ActionType actionType, Expression<Func<T, Task>> action)
        {
            object val;
            byte[] data;
            var methodName = MethodName(actionType, (MethodCallExpression)action.Body, out val, out data);

            await _socketClient.Invoke(actionType, methodName, val);
        }

        private static string MethodName(ActionType actionType, MethodCallExpression body, out object val, out byte[] data)
        {
            var values = body.Arguments
                .Select(argument => new KeyValuePair<Type, object>(argument.Type, GetValue(argument)))
                .ToList();

            var fullServiceName = body.Method.DeclaringType?.Name ?? string.Empty;
            var serviceName = fullServiceName.Substring(1, fullServiceName.IndexOf("PublicService", StringComparison.Ordinal) - 1);
            var methodName = body.Method.Name;
            var parametrs = body.Method.GetParameters().Select(x => x.Name).ToArray();

            val = null;
            data = null;

            //if (parametrs.Length == 1)
            //{
            //    var bytes = values[0].Value as byte[];
            //    if (bytes != null)
            //    {
            //        data = bytes;
            //    }
            //    else
            //    {
            //        val = values[0].Value;
            //    }
            //}
            //else if (parametrs.Length > 1)
            //{
            //    val = parametrs
            //        .Select((x, i) => new {Name = x, values[i].Value})
            //        .Where(x=>!(x.Value is byte[]))
            //        .ToDictionary(x => x.Name, x => x.Value);
            //    var d = values.Select(x=>x.Value).FirstOrDefault(x => x is byte[]);
            //    if (d != null)
            //    {
            //        data = (byte[])d;
            //    }
            //}

            val = parametrs
                    .Select((x, i) => new { Name = x, values[i].Value })
                    .Where(x => !(x.Value is byte[]))
                    .ToDictionary(x => x.Name, x => x.Value);
            var d = values.Select(x => x.Value).FirstOrDefault(x => x is byte[]);
            if (d != null)
            {
                data = (byte[])d;
            }

            if (actionType == ActionType.Data)
            {
                methodName = $"{serviceName}/{methodName}";
            }
            return methodName;
        }

        public static MemberExpression ResolveMemberExpression(Expression expression)
        {
            var memberExpression = expression as MemberExpression;
            if (memberExpression != null)
            {
                return memberExpression;
            }
            var unaryExpression = expression as UnaryExpression;
            if (unaryExpression != null)
            {
                return (MemberExpression)unaryExpression.Operand;
            }
            throw new NotSupportedException(expression.ToString());
        }

        private static object GetValue(Expression expression, Stack<MemberExpression> parents = null)
        {
            var newExpression = expression as NewExpression;
            if (newExpression != null)
            {
                return newExpression.Constructor.Invoke(newExpression.Arguments.Select(x=>GetValue(x)).ToArray());
            }

            var memberInitExpression = expression as MemberInitExpression;
            if (memberInitExpression != null)
            {
                var obj = GetValue(memberInitExpression.NewExpression);
                var type = obj.GetType();
                foreach (var binding in memberInitExpression.Bindings)
                {
                    switch (binding.BindingType)
                    {
                        case MemberBindingType.Assignment:
                            var v = GetValue(((MemberAssignment) binding).Expression);
                            var f = type.GetField(binding.Member.Name);
                            if (f != null)
                            {
                                f.SetValue(obj, v);
                            }
                            else
                            {
                                var p = type.GetProperty(binding.Member.Name);
                                p?.SetValue(obj, v);
                            }
                            
                            break;
                        default:
                            throw new NotSupportedException(binding.BindingType.ToString());
                    }
                }
            }

            var constantExpression = expression as ConstantExpression;
            if (constantExpression != null)
            {
                if (parents == null)
                {
                    return constantExpression.Value;
                }

                var value = constantExpression.Value;
                while (parents.Count>0)
                {
                    var parent = parents.Pop();
                    if (parent.Member.MemberType == MemberTypes.Property)
                        value = ((PropertyInfo)parent.Member).GetValue(value);
                    else if (parent.Member.MemberType == MemberTypes.Field)
                        value = ((FieldInfo)parent.Member).GetValue(value);
                    else
                        throw new Exception("Property must be of type FieldInfo or PropertyInfo");
                }
                return value;
            }
            var memberExpression = expression as MemberExpression;
            if (memberExpression?.Expression != null)
            {
                if (parents == null)
                {
                    parents = new Stack<MemberExpression>();
                }
                parents.Push(memberExpression);
                return GetValue(memberExpression.Expression, parents);
            }
            var unaryExpression = expression as UnaryExpression;
            if (unaryExpression != null)
            {
                return GetValue(unaryExpression.Operand);
            }

            var getter = Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile();

            if (parents != null)
            {
                var value = getter();
                while (parents.Count > 0)
                {
                    var parent = parents.Pop();
                    if (parent.Member.MemberType == MemberTypes.Property)
                        value = ((PropertyInfo)parent.Member).GetValue(value);
                    else if (parent.Member.MemberType == MemberTypes.Field)
                        value = ((FieldInfo)parent.Member).GetValue(value);
                    else
                        throw new Exception("Property must be of type FieldInfo or PropertyInfo");
                }
                return value;
            }

            return getter();
        }
    }
}