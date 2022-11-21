[[Previous section]](Section6.md)          

[[Table of contents]](TableOfContent.md)

# 7. Conclusions: extracting value from Extensible Commands.

## 7.1. Guidelines for building Extensible Commands-based applications.

To build an efficient machine control software application (or some other application implementing complex sequences) using Extensible Commands, it is recommended to separate the functionality into 3 or 4 tiers or layers, as is conventionally done in most enterprise applications. Each layer may use Extensible Commands differently, for example, as shown in Table 5.

|Layer|What it does|
|---------|---------|
|Low-level API|- Exposes functionality of individual components and sub-systems through public methods.<br>- Signals failure by throwing exceptions of ExtensibleCommandsException type from the bodies of all exposed public methods.|
|Mid-level components|-	Wraps low-level API methods in Command classes.<br>- Exposes Command objects to higher-level clients.<br>- Generally, does not contain complex sequences.|
|High-level sequences|-	Uses Command objects exposed by mid-level classes to create sequences of any complexity.<br>- Exposes Command objects to User Interface or another external client to launch these sequences.|
|User Interface|-	Initiates operational sequences by launching commands asynchronously.<br>- Subscribes to Started, Completed, and Failed events to update relevant UI controls.<br>- Provides UI controls for Abort/Pause/Resume functions.<br>- Alarm/error notification.|

Table 5. Application layers.

A natural way to develop an application based on Extensible Commands is to progressively build a collection of command classes and reusable objects of increasing complexity:
1) Instances of standard command classes from the Extensible Commands library implementing application-specific operational sequences.
2) Custom Command classes extending standard Extensible Commands classes, similar to examples described in [Section 5](Section5.md).

## 7.2. Possible future development of Extensible Commands.

The described version of the Extensible Commands is trimmed down to a very basic level and many potential additions were removed or not pursued in favor of keeping the library relatively small and simple. This section discusses some of the features that may be implemented if the Extensible Commands library can is modified and/or extended to address problems and requirements of specific software applications. 

Most of the functionality described in this section requires modification of the library code. These modifications could be a part of the continuous development of the library or branched off and modified specifically for the application. In C# there is also a possibility to create extension methods that allow adding new functionality to classes without modifying their code. However, in Java, there is no such option, and Extensible Commands code modification is necessary.

The features described here would be more difficult to implement alternatively, especially to introduce them into an already existing application.

### 7.2.1. Estimation and measurement of execution time.

One interesting aspect of machine control  software application is measuring, estimating, and collecting statistics on execution times of various operations for purposes of:
-	calculating the estimated time of the operation to inform the user how long the operation is expected to take and provide a frequently updated indicator (such as a progress bar) to show the progression of the operation;
-	tracking variability of operations and detecting abnormal changes in the execution time (such as slowing down);
-	dynamic correction of execution time estimates based on actually measured execution time.

In the absence of Extensible Commands, one may need to implement some way to measure time intervals via stopwatch or another language framework construct, and then introduce start/stop statements throughout the code. To collect statistics, the time information must be channeled from all these points in the code to a central entity that calculates the statistics and makes some decisions, or possibly, do the same work in the code implementing the operations.

With the Extensible Commands library, there are much better options. One way to do this would be to:
1) Add GetEstimatedTime() method to the Command class and implement it for every standard command in the library in a recursive fashion (i.e. estimated time of a composite command would be calculated based on the estimated times of its children).
- For example, an estimated time of a Sequential command would be the sum of the estimated times of its sub-commands.
- Similarly, an estimated time of a Parallel command would be the largest estimated time of its sub-commands (perhaps, plus some empirical constant buffer time to account for thread management), etc.
- Ultimately, every custom low-level command would have to implement its own estimation method based on the specific operation and its parameters. 
- The library would ensure automatic aggregation and “bubbling up” of individual low-level estimated times to provide accurate estimated times for operations of any complexity. Provided the code for low-level execution time estimates is developed, estimation of execution time for any complex operation, present or future, would be automatic and bear no development cost. Thus, this approach is highly scalable and requires development only if new low-level operations are added.
2) Measuring execution time is already implemented in the Command class (EllapsedTime and EllapsedTimeMsec properties). Acquiring execution time statistics could be achieved by keeping the last N execution times, perhaps along with command parameters, and re-calculating the required statistics on every new command execution.
- Individual command classes may be able to acquire statistics about the execution time of its instances, for example, if new private fields and methods are added to calculate execution time statistics. This would be limited to a particular instance of the command. With the use of static fields/methods, it should be possible to acquire statistics on all instances of a particular command class.
- Perhaps, a better approach is to create a centralized component that can subscribe to state change events of commands of interest. This component can re-calculate execution time statistics of individual command objects every time command execution is complete. 
- Once the statistics are available, it becomes possible to flag abnormal excursions from the acceptable execution time window, or automatically correct the estimated execution time calculations using the feedback from the actual measurement. 

### 7.2.2. Reserving system resources.

One of the common requirements in machine control applications is preventing conflicting instructions, i.e. simultaneous execution of two or more operations requiring access to the same physical system components. For example, if a long-running operation is started, any other operation requiring access to the same physical resource(s) should be blocked. This is further complicated by the fact that the resource may not be in use at the time a potentially conflicting operation is started but will be used at some later time.

Extensible Commands can be utilized to provide a straightforward solution to these requirements. One possible implementation would involve the following:
1) Modify Command classes to include a list of used resources/components. For SimpleCommand classes, this list represents the actual resources and can be supplied at construction time. For other classes, the lists can be aggregated recursively from descendant commands.
2) Modify the Command class to include a delegate for reservation check. This delegate would take a list of resources used by this command and return the result of the reservation check. The reservation check will be performed at the beginning of the execution.
3) Implement a centralized class that would keep a list of currently used resources and the reservation check function described in (2).

### 7.2.3. Selective pause or abort.

One interesting feature that may be easily implemented using Extensible Commands is the selective pause or abort of some specific operations. When an abnormal situation occurs, it may be necessary to pause or abort certain operations based on the specifics of that abnormal situation. For example, if a danger of mechanical collision is detected, all motion-related commands could be paused or aborted, while other unrelated commands may proceed.

With Extensible Commands, a possible implementation of such behavior would involve identifying categories of operations that would require such special Pause/Abort handling and distinguishing them by either ensuring that they are of the same command type, or have a special property. Provided references to all currently executing commands are stored and available, at failure time it is possible to examine all descendant commands of all currently executing commands, find which ones have not been completed yet, and determine if any of them would require pause or abort. This approach is scalable, and would not require any new code when new commands are implemented.

[[Previous section]](Section6.md)          

[[Table of contents]](TableOfContent.md)
