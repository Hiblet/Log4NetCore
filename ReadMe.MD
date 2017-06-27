# Log4NetCore

## Description

This project is a minimal set of files to implement Log4Net file appender logging in AspNetCore.
AspNetCore uses dependency injection to provide logging, and this code implements the required ILogger and ILoggerFactory interfaces.
The code is thread-safe, and has a single dedicated logging thread for improved performance.


## Quick Start

 - NuGet Log4Net
 - Add this project's files to your project in a folder called 'Logging'
 - Add "using NZ01;" to your StartUp.cs class
 - Add "loggerFactory.AddLog4Net();" to StartUp.Configure()
 - Add "NZ01.Log4NetAsyncLog.Stop();" to the end of Program.Main()
 - In your class to log, add a member variable of type ILogger.
 - In your class to log, populate the member variable using an instance ctor parameter.
 - In your class to log member function, call ILogger.Log<T> or ILogger.LogInformation() to write.
 - Consider direct logging in speed sensitive operations.
 - Locate log file in ./Logs subdirectory.
 
See Usage section below for details.


## Design

A single thread is established for the lifetime of the application.  
The thread is used to consume objects that are stored on a concurrent queue.
Event signalling is used so that the queue is only consumed when a new object is enqueued, which means there is no polling of the queue.

Log entries created through the ILogger interface record the calling class.
Log entries created directly through the static interface do not automatically log the class, and the class should be logged as part of the message.

The design aim was to provide logging which would not impact performance in any way, so that it could be turned on or off in-the-running without disturbing normal production operation.
As only a single, dedicated thread is used, you will not get any thread starvation.



## Contents

### Code

#### Log4NetProvider.cs
    
This file is an implementation of ILoggerProvider.  
It loads the config, creates the ILogger objects and provides access to the ILoggers to the rest of the app.
There should be one ILogger object created per class that uses a logger via injection.
ILogger objects are stored in a concurrent dictionary.


#### Log4NetLogger.cs

This file is an implementation of ILogger.
The main function is to overload the Log<T> function so that it loads the singleton concurrent queue of data to log.


#### Log4NetExtensions.cs

Small class to provide the AddLog4Net() startup function.


#### Log4NetManager.cs

This class allows you to set the Log4Net logging level in the running.


#### Log4NetAsyncLog.cs

This is the main chunk of code.
It consists of a class to define the wrapper object that is queued, and a class to set up the thread, the queue, and to consume the queue.


### Configuration

#### log4Net.config 

A standard log file config that is newly created each run with a timestamp in the file name, using local time.


#### log4Net_DateLogFile.config 

A copy of the default config, to facilitate switching between this and my usual alternative file.


#### log4Net_SameFileEachRun.config

An alternative config that uses the same file for each run.



## Dependencies

Requires Microsoft.Extensions.Logging for logging interfaces.
Requires Log4Net module, available on NuGet.



## Usage

 - Add Log4Net using NuGet
 - Add this project's files to your project in a folder like 'Logging'
 - Add "using NZ01;" to your Startup.cs class
 - Add "loggerFactory.AddLog4Net();" to Startup.Configure()
 - Add "NZ01.Log4NetAsyncLog.Stop();" to the end of Program.Main().  This is not critical, but it will gracefully terminate the log thread and write an exit message to the log.
 - Use Dependency Injection or direct static access to log messages that default to a file in ".\Logs"
 

### Dependency Injection (Slow)

[Slow?: Do your own tests with a simple loop, but I found using ILogger was incredibly slow.  Logging 10000 text lines took 24 seconds.  The speed sludge is not in the local code or the Log4Net code either, because the Fast method below is a greasey whippet.]

In your controller or class that you wish to log from:

 - Add a member variable for the ILogger type:
       private readonly ILogger _logger;
       
 - Update the instance constructor for the class to take in an ILogger variable, and set the member variable:
       public HelloWorldController(ILogger<HelloWorldController> logger) { _logger = logger; }
       
 - In a member function, call the standard logger interface functions:
 
       private void Boom()
       {
           string wouldLogThis = "OMG Something Happened!";

           // Using the MS simplified wrapper:
           _logger.LogInformation(prefix + wouldLogThis);
                
           // Using the MS primitive Log function, with custom formatter or lambda function, where ex would be a caught exception maybe
           _logger.Log<string>(LogLevel.Debug, 0, wouldLogThis, null, ((x,ex) => x.ToString() + " " + ex.ToString()));
           _logger.Log<string>(LogLevel.Information, 0, wouldLogThis, null, NZ01.Log4NetLogger.MyOwnFormatter);
       }
       

 
### Direct (Fast)

[Fast? Logging 10000 text lines in a loop with ILogger took 24 seconds, logging directly using this method was sub-second for all 10000 lines.  Seriously, something is currently wrong in ILogger, but no doubt MS will fix this, you know, sometime...]

 - In a member function you wish to log from, make direct static calls to standard Log4Net functions ie Debug/Info/Warn/Error/Fatal on the logger.  This still only loads the concurrent queue and therefore it is threadsafe.  The function names are replicated for convenience only.
 
 
       private void Boom()
       {
           string prefix = "MyClass.Boom(),";
           
           string wouldLogThis = "OMG Something Happened!";           
                     
           NZ01.Log4NetAsyncLog.Debug(prefix + wouldLogThis); 
       }



## Implementation Details

### Queue Size

The queue size is defaulted to warn at 10000 (ten thousand) entries, and error at 1000000 (one million) entries, but you could make this a config option.
If the queue size exceeds the error threshold (one million), new data is not enqueued, without error.  
The thinking is that if you have a process (or processes) that is/are able to run ahead of a dedicated thread by this many entries, you have a serious problem.
There are some diagnostics in the files to record the number of calls to various functions, which are harmless and can be removed if you want.


### Queue size and Thread are Logged

Note that the size of the queue is logged when the data is enqueued and dequeued.
This can give you additional clues as to what is happening in the application.

Also, the enqueuing thread is logged (the thread making the call to log), making this useful when trying to see what is happening on various simultaneous threads.


### In-The-Running Changes

Log level can be changed whilst the app is running if you call the static NZ01.Log4NetManager.SetLogLevel() function.
The idea is that you hook up an admin-only API action to this function, and pass a log level.  
You can then shift from INFO to DEBUG log level with an API call, which you can, of course, trigger from a web page, or however you need.

What we have lost here is the Log4Net ability to set a log level per instance of Log4Net logger, and therefore an independent log threshold level per class.
I prefer too much info to too little, and the ability to switch the log level for the whole app is something I prefer.


### Stopping

There is a static Stop() function that will gracefully stop the logging thread.

Web Apps in AspNetCore do not yet (June 2017) signal correctly that the app is stopping.
Startup.Configure() is supposed to take an IApplicationLifetime parameter, and you can use this to register callback functions that are called when the application starts and stops.  
However, MS have some kind of bug going on and the Stopping and Stopped callbacks do not fire.

As a useful work around, it is important to remember that Core apps are now console apps, and have a Main() function.
You can stop the thread in both Web Apps and Console Apps by making a call to the static function Log4NetAsyncLog.Stop() as the last line of Main().
You do not have to do this, but calling this function writes a positive message to the log file and shows that the app exited gracefully.
This can be useful for discriminating between crashes, and user-initiated exits.
Failure to call to Stop() should not cause any problems, but you would lose this diagnostic element.




## Licensing

Use as you see fit, but donate something to Second Chance Tasman Cats and Dogs Home if your conscience is bothering you.

 - https://www.facebook.com/secondchancetasman/


 
## The Last Word

Any bugs, let me know, I use this in anger so it would be good to have feedback.
Angrily shoot me in the head as AvidFan on Destiny/XBox.
