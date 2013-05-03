using System;
using System.IO;
using System.Reflection;
using System.Text;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace ABB.SrcML.Utilities {
    /// <summary>
    /// File logger for SrcML.NET
    /// </summary>
    public class SrcMLFileLogger {

        /*
        public static string MyLoggerName { get; set; }

        public static ILog CreateFileLogger(string loggerName) {
            MyLoggerName = loggerName;
            return LogManager.GetLogger(loggerName);
        }
        */

        /// <summary>
        /// Create a file logger
        /// </summary>
        /// <param name="loggerName"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static ILog CreateFileLogger(string loggerName, string filePath) {
            var appender = CreateFileAppender(loggerName + "Appender", filePath);
            AddAppender(loggerName, appender);
            return LogManager.GetLogger(loggerName);
        }

        /// <summary>
        /// Return the default logger
        /// </summary>
        public static ILog DefaultLogger {
            get {
                return LogManager.GetLogger("DefaultLogger");
            }
        }

        /*
        public static ILog MyLogger {
            get {
                return LogManager.GetLogger(MyLoggerName);
            }
        }
        */

        private static void AddAppender(string loggerName, IAppender appender) {
            var log = LogManager.GetLogger(loggerName);
            var logger = (Logger)log.Logger;

            logger.AddAppender(appender);
            logger.Repository.Configured = true;
        }

        private static IAppender CreateFileAppender(string name, string fileName) {
            var appender = new FileAppender {
                Name = name,
                File = fileName,
                AppendToFile = false,
                ImmediateFlush = true,
                LockingModel = new FileAppender.MinimalLock()
            };

            var layout = new PatternLayout {
                ConversionPattern = "%date %-5level %logger - %message%newline "
            };
            layout.ActivateOptions();

            appender.Layout = layout;
            appender.ActivateOptions();

            return appender;
        }

        static SrcMLFileLogger() {
            var fileInfo = new FileInfo(Assembly.GetCallingAssembly().Location);
            var defaultLogPath = Path.Combine(fileInfo.DirectoryName, "SrcML.NETService" + Guid.NewGuid() + ".log");
            //var defaultLogPath = Path.Combine("C:\\Data\\", "SrcML.NETServicee_" + MyLoggerName + "_" + Guid.NewGuid() + ".log");
            CreateDefaultLogger(defaultLogPath);
        }

        private static void CreateDefaultLogger(string defaultLoggerLogFile) {
            string configurationContent =
                @"<?xml version='1.0'?>
				<log4net>
					<appender name='DefaultFileAppender' type='log4net.Appender.FileAppender'>
						<file value='" + defaultLoggerLogFile + @"' />
						<appendToFile value='false' />
						<lockingModel type='log4net.Appender.FileAppender+MinimalLock' />
						<layout type='log4net.Layout.PatternLayout'>
							<conversionPattern value='%date %-5level %logger - %message%newline' />
						</layout>
					</appender>

                    <logger name='DefaultLogger' additivity='false'>
                        <level value='ALL' />
                        <appender-ref ref='DefaultFileAppender' />
                    </logger>
    
					<root>
						<level value='DEBUG' />
						<appender-ref ref='DefaultFileAppender' />
					</root>
				</log4net>";
            XmlConfigurator.Configure(new MemoryStream(Encoding.Default.GetBytes(configurationContent)));
        }
    }
}
