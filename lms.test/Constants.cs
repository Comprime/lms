using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.IO;
using nng;


namespace lms.test {
	public static class Constants {
		private static Lazy<IAPIFactory<IMessage>> nngFactory = new Lazy<IAPIFactory<IMessage>>(CreateNngFactory);
		private static IAPIFactory<IMessage> CreateNngFactory(){
			var path = Path.GetDirectoryName(typeof(Constants).Assembly.Location);
            var ctx = new NngLoadContext(path);
            return NngLoadContext.Init(ctx);
		}
		public static IAPIFactory<IMessage> NngFactory => nngFactory.Value;


		private static Lazy<ILoggerFactory> logFactory = new Lazy<ILoggerFactory>(CreateLoggerFactory);
		private static ILoggerFactory CreateLoggerFactory() =>
			LoggerFactory.Create(factory => {
				factory.AddConsole();
                factory.AddFilter("lms.IServer", LogLevel.Trace);
                factory.SetMinimumLevel(LogLevel.Information);
			});
		public static ILoggerFactory LogFactory => logFactory.Value;
	}
}