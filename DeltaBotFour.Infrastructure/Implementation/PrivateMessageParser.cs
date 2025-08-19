using DeltaBotFour.Models;
using System;

namespace DeltaBotFour.Infrastructure.Implementation
{
    internal class PrivateMessageParser
    {
        private readonly DB4Thing _privateMessage;

        public PrivateMessageParser(DB4Thing privateMessage)
        {
            _privateMessage = privateMessage;
        }

        public PrivateMessageParseResult Parse()
        {
            // 2025-08 - Reddit removed PMs and replaced with chat. Old ModMail seems to work
            // with the subject / body approach, but chats don't have a subject.
            // Adding support for both via a "chat command"
            var command = _privateMessage.Subject.Trim().ToLowerInvariant();
            var argument = _privateMessage.Body.Trim();

            var isDirectChat = string.Equals(command, "[direct chat room]", StringComparison.CurrentCultureIgnoreCase);

            // Reddit makes the subject "[direct chat room]" for chats
            if (isDirectChat)
            {
                command = null;

                // The command will be in the body and should start with a !
                if (argument.StartsWith('!'))
                {
                    var parts = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    command = parts[0].Replace("!", string.Empty);

                    if (parts.Length > 1)
                    {
                        argument = parts[1];
                    }
                }
            }

            return new PrivateMessageParseResult
            {
                IsDirectChat = isDirectChat,
                Command = command,
                Argument = argument
            };
        }
    }
}