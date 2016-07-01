using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Server.Model;

namespace TestInfrastructure.ServiceProxy
{
    public static class ServiceManagerExtension
    {
        public static Task<TRes> Auth<TService, TRes>(this ServiceManager<TService> service, Expression<Func<TService, Task<TRes>>> action)
        {
            return service.Invoke(ActionType.Auth, action);
        }

        public static Task Auth<TService>(this ServiceManager<TService> service, Expression<Func<TService, Task>> action)
        {
            return service.Invoke(ActionType.Auth, action);
        }

        public static Task<TRes> Event<TService,TRes>(this ServiceManager<TService> service, Expression<Func<TService, Task<TRes>>> action)
        {
            return service.Invoke(ActionType.Event, action);
        }

        public static Task Event<TService>(this ServiceManager<TService> service, Expression<Func<TService, Task>> action)
        {
            return service.Invoke(ActionType.Event, action);
        }

        public static Task<TRes> Data<TService, TRes>(this ServiceManager<TService> service, Expression<Func<TService, Task<TRes>>> action)
        {
            return service.Invoke(ActionType.Data, action);
        }

        public static Task Data<TService>(this ServiceManager<TService> service, Expression<Func<TService, Task>> action)
        {
            return service.Invoke(ActionType.Data, action);
        }
    }
}