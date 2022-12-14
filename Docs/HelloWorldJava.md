# “Hello World” example in Java. 

Here is a simple step-by-step procedure to get started using the Extensible Commands Java library in IntelliJ IDEA 2022.2 development environment. 

1) To create a new target "Hello World" project open IntelliJ IDEA (or, if the target project already exists, skip to Step (2)). On the "Welcome to IntelliJ IDEA" start-up panel: 
   - Press the "New Project" button. 
   - In the “New Project” change the project name and location as needed. Select "Java" for the language, and “Maven” for the build system, and make sure the JDK selection is above 10. Make sure the “Add sample code” checkbox is checked. Press the "Next" button. 
   - The basic console application should be created. 
2) Add Extensible Commands library to the project: 
   - Add the following block to the pom.xml file
```
<dependencies> 
    <dependency> 
        <groupId>io.github.ichizhov</groupId> 
        <artifactId>extensiblecommands</artifactId> 
        <version>1.1.0</version> 
    </dependency> 
</dependencies> 
```
   - Go to Settings > Build, Execution, Deployment > Maven and make sure that the check box “Always update snapshots” is checked. 
3) Add the sample code: 
   - Copy and paste the below sample code to the body of the main() method in the Main.java source file. 
   - Add statement import org.extensiblecommands.*; to the top of the file. 
   - Add statement throws Exception to the declaration of main(). 
```
// Output string to console 
var helloWorldCmd = new SimpleCommandI<String>(input -> System.out.println(input), "Hello World\n"); 

// Wait for user input 
var waitForConsoleInputCmd = new SimpleCommand(() -> System.in.read()); 

// Create a sequence of the above 2 steps 
var sequentialCommand = new SequentialCommand(); 
sequentialCommand.add(helloWorldCmd).add(waitForConsoleInputCmd); 

// Supply input and run sequence 
helloWorldCmd.setInput("Hello World!"); 
sequentialCommand.run(); 
```

The code can now be compiled and run.