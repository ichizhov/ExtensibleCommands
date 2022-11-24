[[Previous section]](Section1.md) / [[Next section]](Section3.md)             

[[Table of contents]](TableOfContent.md)

# 2. Overview of Extensible Commands.

At the core of the Extensible Commands approach is the hierarchy of classes based on the command pattern, as discussed in the [“Gang of Four” book](https://springframework.guru/gang-of-four-design-patterns/). The basic idea of the command pattern is the encapsulation of a command represented by a method of a class, whose main purpose is to provide access to this method. The classic advantages of the command pattern, as compared to a straightforward method invocation, are:
- separation of command objects creation and its execution. A command can be created at application startup, but executed later on;
- the ability to build composite command objects with the ability to track execution steps;
- the ability to build a hierarchy of command classes expanding functionality as needed.

The Extensible Commands library offers a set of classes using a combination of these patterns:
- Command
   - Every command object has an Execute() method that uniquely reflects the purpose of this command object.
   - A command is created first and executed (or run) later.
- Decorator
   - Decorator commands contain one or more other commands (referred to as sub-commands) while adding important new functionality. 
- Composite
   - Composite commands also contain other commands (also referred to as sub-commands) and provide different models of execution order.
   
## 2.1. Advantages and applicability area of Extensible Commands.

Extensible Commands targets a set of problems that are common but not limited to machine control software. It is designed to simplify software architecture and development by moving a set of functionalities to the framework level that can be developed and tested separately and then reused with confidence across the application code base.

Specifically, Extensible Commands provides abstraction in the following areas:
1)	Failure handling.
2)	Common flow control functionality, such as retries.
3)	Abort, stop and continue operations.
4)	Encapsulation of thread synchronization.

Extensible Commands is designed to address the issue of growing complexity and volatility of business logic and the correspondent ballooning of maintenance efforts, refactoring, and accompanying defects as the application evolves. It offers an efficient approach to mapping operational sequences to code. It is presumed that the applications supporting a large number of complex and frequently changing sequences with a lot of failure scenarios and branching would benefit the most from the Extensible Commands approach. Applications for machine control are especially good targets for the Extensible Commands-based implementation due to a wide range of failure possibilities due to hardware malfunction or non-availability and the necessity to execute complex sequences. But any software application that needs to manage complex operational sequences of steps should benefit from the Extensible Commands approach. 

Extensible Commands can be “mixed and matched” with any software architecture and can be used in addition to existing software design patterns and practices of a specific application or domain. It does not require a full or exclusive commitment and can be used as needed in the parts or layers of the application where it can bring the most benefits.

## 2.2. Disadvantages of Extensible Commands.

Software applications that deal with relatively short, simple, or rarely changing sequences and failure scenarios are likely to be burdened by the Extensible Commands approach rather than benefit from it. 

Some disadvantages must be taken into account before adopting the Extensible Commands approach in the application’s design.
1)	Execution path involves additional infrastructure layers.
   - It will be more difficult to trace the execution path through the code in the debugger due to additional command infrastructural code. Breakpoints at strategic junctions and Extensible Commands logging should help.
2)	Command parameters.
   - Commands are objects and therefore are “first-class citizens” in object-oriented programming languages such as C#, and as such, they can have properties that configure their behavior (i.e. act as command parameters). Since it is possible to create a command object first and set its parameters later, in a multi-threaded environment there may be race conditions associated with a separation of setting command parameters and its execution.
   - If a command is used as part of more complex commands, setting its parameters must occur as part of the command sequence itself, especially, if the parameters of a sub-command depend on the results of one of the previous steps. If such a parameter-setting step is omitted, the command may have wrong parameters during execution.

[[Previous section]](Section1.md) / [[Next section]](Section3.md)             

[[Table of contents]](TableOfContent.md)
