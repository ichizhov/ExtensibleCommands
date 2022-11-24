[[Previous section]](Section5.md) / [[Next section]](Section7.md)             

[[Table of contents]](TableOfContent.md)

# 6. Other topics.

## 6.1. Lifetime of command objects.

On the application level, there are some implications of command object lifetime that are worth considering explicitly. Generally, one can create a command object at application startup (or at some other defined time during the software application lifetime) and reuse it multiple times, or create it immediately before command execution and use it only once, creating a new command object for another execution.

In the first approach, parameters must be set every time before the command is executed. Subscription to command lifecycle events may occur at the application startup and be disposed of only when the application terminates. This approach works best if:
1)	Only one instance of the command can be executed at any given time (for example, if it uses a limited resource). Probably, the majority of hardware operations would fall under this category.
2)	The command does not have parameters.

In the second approach, subscription to command lifecycle events must be done every time the command is executed and must be disposed of when the command is no longer running. A factory class constructing command objects may facilitate the subscriptions. This approach is useful if:
1)	The command depends on the parameters of the request (for example, the user needs to specify resources or behaviors that affect the structure of the sequence).
2)	Several instances of the command need to be executed in parallel, each with its own set of parameters (for example, parallel data processing).

A  hybrid approach is to create a command object with the application but to fill it with content at the time of the request. In this case subscriptions to command lifecycle events could still happen on the application startup, but the command sequence may be structured depending on the parameters of the request.

## 6.2. Command event logging.

[[C# code]](../CSharp/ExtensibleCommands/ExtensibleCommands/ExtensibleCommandsCore.cs) [[Java code]](../Java/ExtensibleCommands/src/main/java/org/extensiblecommands/ExtensibleCommandsCore.java)

The Extensible Commands library itself does not have any logging capabilities. However, it defines a simple ILog interface that is used throughout the code to log key events of command execution (start, completion, failure). To make use of logging, a preferred logging package needs to be exposed through the ILog interface and supplied as the ExternalLogger property in the Logger class. Then command event logging can be turned on and off by setting the IsLoggingEnabled property. In the unit test projects that are a part of the Extensible Commands, a simple logger redirecting all logging statements to a console output is supplied.

## 6.3. Debugging tips.

Debugging sequences built using Extensible Commands is more complicated than debugging a sequence of methods. It is possible to trace through a sequence of commands on a single thread, though it is fairly tedious. If a command sequence needs to be debugged, the following approaches are recommended:
1)	Enable command event logging and analyze the sequence of command events.
2)	Place breakpoints in Execute() methods of commands to be debugged.

## 6.4. Unit Testing.

[[C# unit tests]](../CSharp/ExtensibleCommands/ExtensibleCommandsUnitTests) [[Java unit tests]](../Java/ExtensibleCommands/src/test/java/org/extensiblecommands)

The Extensible Commands library comes with a suite of unit tests that test all the internal plumbing of the infrastructure. Every command class is subjected to standard unit tests checking the correct setting of the command states, responses to various failures, stop, resume, and abort scenarios and other aspects of the Extensible Commands functionality.

[[Previous section]](Section5.md) / [[Next section]](Section7.md)             

[[Table of contents]](TableOfContent.md)
