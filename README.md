# Extensible Commands

## 1. Introduction. Why Extensible Commands?

I am a software engineer creating software that controls complex machinery in modern high-tech factories. This project is aimed at solving a number of practical architectural and software engineering challenges I encountered in my work. After working on it for a number of years and successsfully applying it to substantially enhance the quality, flexibility and speed of development of my software I decided to publish it as an open source project.

So, what are the challenges that this project aspires to address?

When one creates software that controls a complex piece of equipment, one is very quickly faced with the need to develop and maintain sequences of operational steps. These sequences quickly grow into a hierarchy of progressively complex operations that could be called individually or as part of yet more complex sequences. They constantly need to be modified, optimized, moved around, split and merged, in response to project development needs. The original "pain point" that gave rise to this project was the extreme difficulty of keeping error handling aspects of the code intact and correctly functioning during these modifications. However, the solution turned out to provide many more benefits than just resolving this original problem. Thus, the motivation behind this work is the desire to find a better way to deal with high volatility of operational sequence code, and encapsulate many aspects of this volatility behind a dedicated infrastructural implementation.

There are many software products offering various levels of workflow or orchestration automation (a representative list can be found [here](https://github.com/meirwah/awesome-workflow-engines)). Almost all of them are specific to a particular domain (such as business process automation) or written in languages rareley used in my domain of interest. Some require extensive dependencies on other libraries with licences that may not be suitable for commercial development and deployment. As such, I found them of little practical use for my purposes.

Though my experience was specific to applications involving orchestration of complex hardware systems, the principles and implementation of Extensible Commands contain no assumptions about the application domain. They use only the basic constructs of high-level programming languages (C# and Java). It should be possible to re-use it in any domain where orchestration of complex sequences is a highly volatile area of the code. 

## 2. Basic structure of the project.

The body of the code contains 2 mirror implementations: in [C#](CSharp/ExtensibleCommands/ExtensibleCommands) and [Java](Java/ExtensibleCommands/src/main/java/org/extensiblecommands). Each contains the main project and the unit test project. Unit tests also serve as a reference on the the practical use of the library code in different scenarios.

The main project contains classes implementing Extensible Commands. The approach is inspired by the command pattern as decribed by the ["Gang of Four"](https://springframework.guru/gang-of-four-design-patterns/). The essence of the approach is to combine the command pattern (with the encapsulation of the operation inside a class), and the composite pattern (with the combination of inheritance and composition relationship). This allows the construction of a hierarchy of command objects that are derived from each other and can be contained within each other.

The overall class diagram of the project is shown below.

![Picture 1](Docs/Figures/Figure1.png)

Each command class implements an aspect of flow control functionality. Many correspond to functionality already implemented in high-level programming languages (such as ConditionalCommand for if-else operator), but others represent control functionality not available "out of the box" (such as RetryCommand). SimpleCommand classes serve as the implementors of the actual content of operations, while all other commands serve to stictch these individual operations into coordinated sequences.

In a practical application, instances of these classes can be created and combined into sequences. Also, new classes can be derived from the standard command classes adding new functionalities to them.

Each command can be executed by calling its Run() method. There is no external execution manager. As the command is being executed, it goes through various states that can be monitored. 

This section is just a brief introdction to the internals of the Extensible Comands library. For detailed description, please see the [Main Documentation](Docs/TableOfContent.md).

## 3. Getting Started.

This section describes how to get started with Extensible Commands by writing a canonical "Hello World" application. 

### 3.1. C#. [NEEDS TESTING]

Here is a simple step-by-step procedure to create a console application using Extensible Commands C# library in Microsoft Visual Studio Community Edition 2019 development environment. For other situations, the procedure may be modified accordingly.

To include Extensible Commands project into a C#/.NET application please follow these steps.
1) Create new target "Hello World" project (or for your specific target project, skip to Step (2). Open Visual Studio 2019. In a start window panel:
   - Select "Create a new project".
   - In the next window select " Console application" template and press "Next".
   - In the next window ("Configure your new project") enter the project name and its location and press "Next". 
   - In the next window keep the default Target framework and press "Create".
   - The new project should now be loaded and the main Visual Studio 2019 window should be displayed.
2) Add Extensible Commands package to the project:
   - Select menu item "Tool > NuGet Package Manager > Manage NuGet Packages for Solution ..."
   - In the NuGet tab press "Browse" sub-tab and enter "ExtensibleCommands" in the search box and press "Enter".
   - From the list of packages select ExtensibleCommands, then select the project in the solution as a target and press "Install".
   - The package should now be added as a dependency to the project.
3) Add the sample code:
   - Copy and paste the below sample code to the body of Main() method in Program.cs source file.
   - Add statement ```using ExtensibleCommands;``` to the top of the file.
 
The code can now be compiled and run.

```
        // Output string to console
        var helloWorldCmd = new SimpleCommandI<string>(input => Console.WriteLine(input), "Hello World\n");

        // Wait for user input
        var waitForConsoleInputCmd = new SimpleCommand(() => Console.ReadKey());

        // Create sequence of the above 2 steps
        var sequentialCommand = new SequentialCommand();
        sequentialCommand.Add(helloWorldCmd).Add(waitForConsoleInputCmd);

        // Supply input and run sequence
        helloWorldCmd.Input = "Hello World!";
        sequentialCommand.Run();
```

### 3.2. Java. [NEEDS TESTING]

Java project is structured so that it is compilable from Maven command line, as well as from IntelliJ IDEA development environment.

Here is a simple step-by-step procedures to get started using Extensible Commands Java library in IntelliJ IDEA 2021.1 development environment. For other situations, the procedure may be modified accordingly.

To include Extensible Commands project into a Java application as a Maven module, please follow these steps. 
1) Create new target "Hello World" project (or for your specific target project, skip to Step (2). On "Welcome to IntelliJ IDEA" startup panel:
   - Press "New Project" button.
   - Keep selections of "Java" and "12.0" in "Project SDK" drop-down menu. Press "Next" button.
   - Check "Create project from template" checkbox. Select "Command Line App" template. Press "Next" button.
   - Enter project name, for example, "HelloWorldApp". Enter desired project location and base package name. Press "Next" button. At this point the new project will be created.
   - Once the project is created, open File -> Project Structure -> Modules -> Rename module to HelloWorldApp. (?)
2) Add Extensible Commands library to the project:
   - Open File -> Project Structure -> Modules.
   - In the panel on the right add a dependency: press "+" and select "Library" from the drop-down menu.
   - In the "Choose Library" dialog box press "New Library" and select "From Maven..." from the drop-down menu.
   - In the "Download Library from Maven Repository" dialog box search for Extensible Commands library by typing "extensiblecommands" in the search box.
   - Select "extensiblecommands" from the list and press "OK".
   - In "Configure Library" dialog box keep the selections and press "OK".
   - Select "extensiblecommands" from the list and "Project Libraries" and press "Add Selected".
   - Press "Apply" and then "OK". The library is now aded to the project.
3) Add the sample code:
   - Copy and paste the below sample code to the body of main() method in Main.java source file.
   - Add statement ```import org.extensiblecommands.*;``` to the top of the file.
   - Add statement ```throws Exception``` to the declaration of main().

The code can now be compiled and run.

```
        // Output string to console
        var helloWorldCmd = new SimpleCommandI<String>(input -> System.out.println(input), "Hello World\n");

        // Wait for user input
        var waitForConsoleInputCmd = new SimpleCommand(() -> System.in.read());

        // Create sequence of the above 2 steps
        var sequentialCommand = new SequentialCommand();
        sequentialCommand.add(helloWorldCmd).add(waitForConsoleInputCmd);

        // Supply input and run sequence
        helloWorldCmd.setInput("Hello World!");
        sequentialCommand.run();
```

### 3.3. More documentation and code samples.

Code samples used in the documentation to illustrate the basic use of command classes can be found in this unit test source file:  [CSharp/ExtensibleCommands/ExtensibleCommandsUnitTests/CommandExamplesTest.cs](CSharp/ExtensibleCommands/ExtensibleCommandsUnitTests/CommandExamplesTest.cs). They are in C# only, there is no corresponding file in the Java project.

Unit tests are organized by command classes and is a good source to get more insights into the details of each command class ([[C# unit tests]](CSharp/ExtensibleCommands/ExtensibleCommandsUnitTests)[[Java unit tests]](Java/ExtensibleCommands/src/test/java/org/extensiblecommands)). 

For practical use cases of complex command construction, please see [Section 5 of Main Documentation](Docs/Section5.md) and these unit test source files:  [CSharp/ExtensibleCommands/ExtensibleCommandsUnitTests/GenericExtensibleCommandsTest.cs](CSharp/ExtensibleCommands/ExtensibleCommandsUnitTests/GenericExtensibleCommandsTest.cs) (C#) and [Java/ExtensibleCommands/src/test/java/org/extensiblecommands/GenericExtensibleCommandsTest.java](Java/ExtensibleCommands/src/test/java/org/extensiblecommands/GenericExtensibleCommandsTest.java) (Java).

## 4. Difference between C# and Java implementations.

C# and Java implementations are written to be as similar as possible. C# generally adopts [Microsoft coding style](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions), and Java - [Google coding style](https://google.github.io/styleguide/javaguide.html). The substantive differences beyond generic coding style differences are documented in the table below.

|Item/Area|C#|Java|
|---------------|---------------|------------|
|Platform|.NET Standard 2.0|JDK 12.0|
|Dependencies|System.Reactive 5.0.0|io.reactivex.rxjava2:rxjava:2.1.0|
|Thread Synchronization	|Use standard class ManualResetEventSlim.|Since there is no standard class with the same functionality, a custom ManualResetEvent class is implemented using object under the hood.|
|Delegates|Use language keyword delegate.	|Define interfaces with a single method with an appropriate signature. This generates extra interface source files that do not exist in the C# version of the code.|
|Properties|Use regular or auto properties, as needed. |Use get()/set() methods to acess or modify the fields. |
|Exceptions|Throw exceptions of type Exception as needed.|Exceptions of type RuntimeException are thrown during command execution failures.|
|Comments|Use XML comments.|Use JavaDoc comments (for non-trivial methods and fields).|
|Order of declarations within a class|Properties -> Fields -> Constructors -> Public methods -> Private methods|Fields -> Constructors -> Public methods ->Private methods|

## 5. Links

Here are links to various parts of the Extensible Commands code and documentation:

[Main Documentation](Docs/TableOfContent.md)

[C# code](CSharp/ExtensibleCommands/ExtensibleCommands)

[C# unit tests](CSharp/ExtensibleCommands/ExtensibleCommandsUnitTests)

[Java code](Java/ExtensibleCommands/src/main/java/org/extensiblecommands)

[Java unit tests](Java/ExtensibleCommands/src/test/java/org/extensiblecommands)

## 6. License

The project is covered by the [MIT license](LICENSE).

## 7. Releases.

The current version of the project is the initial release ...
