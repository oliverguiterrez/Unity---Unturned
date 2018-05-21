﻿using System;
using System.Drawing;
using System.Reflection;
using Rocket.API.Commands;
using Rocket.API.DependencyInjection;
using Rocket.API.Logging;
using Rocket.API.User;
using Rocket.Core.Configuration;
using Rocket.Core.Extensions;
using Rocket.Core.Logging;

namespace Rocket.Unturned.Console
{
    public class UnturnedConsole : IConsole, IFormattable
    {
        private readonly IDependencyContainer container;

        public UnturnedConsole(IDependencyContainer container)
        {
            SessionConnectTime = DateTime.Now;
            BaseLogger.SkipTypeFromLogging(GetType());
            this.container = container;
        }

        public string Id => "Console";
        public string Name => "Console";
        public string IdentityType => IdentityTypes.Console;

        public IUserManager UserManager => container.Resolve<IUserManager>("game");
        public bool IsOnline => true;
        public DateTime SessionConnectTime { get; }
        public DateTime? SessionDisconnectTime => null;
        public DateTime? LastSeen => DateTime.Now;
        public string UserType => "Console";

        public void WriteLine(string format, params object[] bindings)
            => WriteLine(LogLevel.Information, format, Color.White, bindings);

        public void WriteLine(string format, Color? color = null, params object[] bindings)
            => WriteLine(LogLevel.Information, format, color, bindings);

        public void WriteLine(LogLevel level, string format, params object[] bindings)
            => WriteLine(LogLevel.Information, format, Color.White, bindings);

        public void WriteLine(LogLevel level, string format, Color? color = null, params object[] bindings)
        {
            IRocketSettingsProvider rocketSettings = container.Resolve<IRocketSettingsProvider>();
            Color orgCol = ConsoleLogger.GetForegroundColor();

            SetForegroundColor(Color.White);
            System.Console.Write("[");

            SetForegroundColor(BaseLogger.GetLogLevelColor(level));
            System.Console.Write(BaseLogger.GetLogLevelPrefix(level));

            SetForegroundColor(Color.White);
            System.Console.Write("] ");

            if (rocketSettings?.Settings.Logging.IncludeMethods ?? true)
            {
                SetForegroundColor(Color.White);
                System.Console.Write("[");

                SetForegroundColor(Color.DarkGray);
                System.Console.Write(GetLoggerCallingMethod().GetDebugName());

                SetForegroundColor(Color.White);
                System.Console.Write("] ");
            }

            SetForegroundColor(color ?? Color.White);

            string line = string.Format(format, bindings);
            System.Console.WriteLine(line);

            SetForegroundColor(orgCol);
        }

        public void Write(string format, Color? color = null, params object[] bindings)
        {
            ConsoleColor orgColor = System.Console.ForegroundColor;
            ConsoleLogger.SetForegroundColor(color ?? Color.White);
            System.Console.Write(format, bindings);
            System.Console.ForegroundColor = orgColor;
        }

        public void Write(string format, params object[] bindings)
        {
            Write(format, null, bindings);
        }

        private MethodBase GetLoggerCallingMethod() => ReflectionExtensions.GetCallingMethod(typeof(UnturnedConsole));

        private void SetForegroundColor(Color color)
        {
            ConsoleLogger.SetForegroundColor(color);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null)
                return Name.ToString(formatProvider); ;

            if (format.Equals("id", StringComparison.OrdinalIgnoreCase))
                return Id.ToString(formatProvider);

            if (format.Equals("name", StringComparison.OrdinalIgnoreCase))
                return Name.ToString(formatProvider);

            throw new FormatException($"\"{format}\" is not a valid format.");
        }
    }
}