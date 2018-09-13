using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lure.Net.Messages
{
    public static class NetMessageManager
    {
        private static readonly Dictionary<Type, ushort> TypeActivators = new Dictionary<Type, ushort>();
        private static readonly Dictionary<ushort, Func<NetMessage>> IdActivators = new Dictionary<ushort, Func<NetMessage>>();

        static NetMessageManager()
        {
            Register(typeof(NetMessageManager).Assembly);
        }

        public static void Register(Assembly assembly)
        {
            var messageTypes = assembly
                .GetTypes()
                .Select(x => (Attribute: x.GetCustomAttribute<NetMessageAttribute>(false), Type: x))
                .Where(x => x.Attribute != null && typeof(NetMessage).IsAssignableFrom(x.Type))
                .Select(x => (x.Attribute.MessageTypeId, x.Type))
                .ToList();

            foreach (var (messageTypeId, type) in messageTypes)
            {
                TypeActivators.Add(type, messageTypeId);
                IdActivators.Add(messageTypeId, ObjectActivatorFactory.Create<NetMessage>(type));
            }
        }

        public static TMessage Create<TMessage>() where TMessage : NetMessage
        {
            if (TypeActivators.TryGetValue(typeof(TMessage), out var messageTypeId))
            {
                return (TMessage)Create(messageTypeId);
            }
            else
            {
                return null;
            }
        }

        internal static NetMessage Create(ushort typeId)
        {
            if (IdActivators.TryGetValue(typeId, out var activator))
            {
                var message = activator();
                message.TypeId = typeId;
                return message;
            }
            else
            {
                return null;
            }
        }
    }
}
