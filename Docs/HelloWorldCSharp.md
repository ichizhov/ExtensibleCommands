# “Hello World” example in C#. 

Here is a simple step-by-step procedure to create a “Hello World” console application using the Extensible Commands C# library in the Microsoft Visual Studio Community Edition 2022 development environment.  

1) To create a new target "Hello World" project, open Visual Studio (or, if the target project already exists, skip to Step (2)). In a start-up window panel: 
   - Select "Create a new project". 
   - In the next window select the "Console application" template and press "Next". 
   - In the next window ("Configure your new project") enter the project name and location and press "Next". 
   - In the next window keep the default Target framework and press "Create". 
   - The new project should now be loaded and the main Visual Studio 2022 window should be displayed. 
2) Add Extensible Commands package to the project: 
   - Select the menu item "Tools > NuGet Package Manager > Manage NuGet Packages for Solution ..." 
   - In the NuGet tab press the "Browse" sub-tab and enter "ExtensibleCommands" in the search box and press "Enter". 
   - From the list of packages select ExtensibleCommands, then select the project in the solution as a target and press "Install". 
   - You may see the “Preview Changes” message box, press “OK”. 
   - The package should now be added as a dependency to the project. 
3) Add the sample code: 
   - Copy and paste the below sample code to the body of the Main() method in the Program.cs source file (or directly, if a new .NET 6 framework default console app template is used). 
   - Add statement ```using ExtensibleCommands;``` to the top of the file. 
```
// Output string to console 
var helloWorldCmd = new SimpleCommandI<string>(input => Console.WriteLine(input), "Hello World\n"); 

// Wait for user input 
var waitForConsoleInputCmd = new SimpleCommand(() => Console.ReadKey()); 

// Create a sequence of the above 2 steps 
var sequentialCommand = new SequentialCommand(); 
sequentialCommand.Add(helloWorldCmd).Add(waitForConsoleInputCmd); 

// Supply input and run sequence 
helloWorldCmd.Input = "Hello World!"; 
sequentialCommand.Run(); 
```

The code can now be compiled and run.