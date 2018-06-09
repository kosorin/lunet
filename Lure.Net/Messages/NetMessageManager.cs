using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Lure.Net.Messages
{
    public static class NetMessageManager
    {
        private static readonly Dictionary<ushort, Type> Types = new Dictionary<ushort, Type>();

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
                .Select(x => (x.Attribute.Id, x.Type))
                .ToList();

            foreach (var (typeId, type) in messageTypes)
            {
                Types.Add(typeId, type);
            }
        }

        internal static NetMessage Create(ushort typeId)
        {
            if (Types.TryGetValue(typeId, out var type))
            {
                var message = (NetMessage)Activator.CreateInstance(type);
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
