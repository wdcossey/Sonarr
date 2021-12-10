using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Moq;


namespace NzbDrone.Test.Common
{
    public static class LoggerExtensions
    {
        public static void Expected(this Mock<ILogger> loggerMock, LogLevel logLevel, Func<Times> times)
        {
            loggerMock
                .Verify(logger => logger.Log(logLevel, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType,Exception,string>>()), times);
        }
        
        public static void Expected<TService>(this Mock<ILogger<TService>> loggerMock, LogLevel logLevel, Func<Times> times, Type exceptionType = null)
        {
            loggerMock
                .Verify(logger => logger.Log(
                    logLevel, 
                    It.IsAny<EventId>(), 
                    It.IsAny<It.IsAnyType>(), 
                    It.Is<Exception>((o, type) => exceptionType == null || o.GetType() == exceptionType), 
                    It.IsAny<Func<It.IsAnyType,Exception,string>>()), 
                    times);
        }

        public static void ExpectedErrors<TService>(this Mock<ILogger<TService>> loggerMock, Func<Times> timesFunc) 
            => loggerMock.Expected(LogLevel.Error, timesFunc);
        
        public static void ExpectedErrors<TService>(this Mock<ILogger<TService>> loggerMock, Times times) 
            => loggerMock.ExpectedErrors(() => times);
        
        public static void ExpectedErrors<TService>(this Mock<ILogger<TService>> loggerMock, int callCount)
            => loggerMock.ExpectedErrors(Times.Exactly(callCount));

        public static void ExpectedErrors(this Mock<ILogger> loggerMock, Func<Times> timesFunc) 
            => loggerMock.Expected(LogLevel.Error, timesFunc);
        
        public static void ExpectedErrors(this Mock<ILogger> loggerMock, Times times) 
            => loggerMock.ExpectedErrors(() => times);
        
        public static void ExpectedErrors(this Mock<ILogger> loggerMock, int callCount)
            => loggerMock.ExpectedErrors(Times.Exactly(callCount));
        
        public static void ExpectedWarns<TService>(this Mock<ILogger<TService>> loggerMock, Func<Times> timesFunc)
            => loggerMock.Expected(LogLevel.Warning, timesFunc);
        
        public static void ExpectedWarns<TService>(this Mock<ILogger<TService>> loggerMock, Times times)
            => loggerMock.ExpectedWarns(() => times);

        public static void ExpectedWarns<TService>(this Mock<ILogger<TService>> loggerMock, int callCount)
            => loggerMock.ExpectedWarns(Times.Exactly(callCount));
        
        public static void ExpectedWarns(this Mock<ILogger> loggerMock, Func<Times> timesFunc)
            => loggerMock.Expected(LogLevel.Warning, timesFunc);
        
        public static void ExpectedWarns(this Mock<ILogger> loggerMock, Times times)
            => loggerMock.ExpectedWarns(() => times);

        public static void ExpectedWarns(this Mock<ILogger> loggerMock, int callCount)
            => loggerMock.ExpectedWarns(Times.Exactly(callCount));
        
        /*public static void WaitForErrors<TService>(this Mock<ILogger<TService>> loggerMock, Func<Times> timesFunc, int msec)
        {
            var waitEvent = new ManualResetEventSlim();
            
            while (true)
            {
                lock (_logs)
                {
                    var levelLogs = _logs.Where(l => l.Level == LogLevel.Error).ToList();

                    if (levelLogs.Count >= count)
                    {
                        break;
                    }

                    waitEvent.Reset();
                }

                if (!waitEvent.Wait(msec))
                    break;
            }

            loggerMock.Expected(LogLevel.Error, timesFunc);
        }*/
    }
}
