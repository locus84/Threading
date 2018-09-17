# Locus.Threading

Simple Threading Library for Dot Not Core\
Actor based concurrency with **Task** & **async await** support

<br />

## Examples

How to create actor(fiber)
```cs
var myFiber = new TaskFiber();
```
\
How to enqueue a action or a task.
```cs
myFiber.Enqueue(myTask);

//or 
myFiber.Enqueue(myAction);

//you can also enqueue a Func<T> or Task<T>
myFiber.Enqueue(new Task<int>(() => 0));
```
\
The Enqueue function returns Task(or Task<T\> ) so you can await it
```cs
await myFiber.Enqueue(myAction);

//or
await myFiber.Enqueue(myTask);

//or
var myInt = await myFiber.Enqueue(new Task<int>(() => 0));
```
But **DO NOT** enqueue async method directly
```cs
void SomeFunction()
{
    myFiber.Enqueue(SomeAsyncFunction()); // this will raise an exception that task is already started
}

async Task SomeAsyncFunction()
{
    ...
}
```
\
**Async Await Support!**

An **await** keyword captures current context and use it later execution.\
So after calling **await** keyword, your code will be handled by unknown threadpool thread. 

But each **TaskFiber** have it's own custom logical Synchronization Context. \
**TaskFiber**'s SynchronizationContext will remember current Fiber as a returning Thread. \
After called **await** we'll safly be back to our calling Fiber.



```cs
async Task SomeFunction()
{
    //this will await full async execution
    await SomeAsyncFunction("Direct call"));

    //this will await just enqueueing and 1st iteration of SomeAsyncFunction.
    //(right before another await inside function)
    await myFiber.Enqueue(() => SomeAsyncFunction("Action Style"));
}

async Task SomeAsyncFunction(string log)
{
    //You can check where is your context anytime
    Console.WriteLine(log + " : "  + myFiber.IsCurrentThread);
    // - "Direct call : false"
    // - "Action Styple : true"
    
    //if you call this function directly, call one of following to get into TaskFiber execution
    await myFiber;
    await Task.Yield().IntoFiber(myFiber);
    await myFiber.IntoFiber();

    //Now you're in myFiber's execution chain.
    Console.WriteLine(log + " : "  + myFiber.IsCurrentThread);
    // - "Direct call : true"
    // - "Action Styple : true"

    await Task.Delay(1000);
    //when calling above await keyword, the execution context will be stored
    Console.WriteLine(myFiber.IsCurrentThread);
    // - "Direct call : true"
    // - "Action Styple : true"
}
```


There is also MessageFiber<T\> class for better performance. Take a look.
\
<br />


## Motivation

When I was in .Net childhood, I fall in love deeply with **Retlang** which is a concorrency library like **Jetlang** in Java world.\
It gave me simple approches how to use multicore environment without race conditions, like functional languages such as **Erlang**.\
And then, **TPL**(Task Parallel Library) and **async await** keywords are introduced, my codes are get simplar than before.\
But still, I wanted to use somewhat **Actor Based Concurrency** as before, I made a simple library that utilize Task and 'async await' funtionality.

## Installation

Download source files and include them into your project.\
Or use nuget package console.

```
PM > Install-Package Locus.Threading
```Works too.


## License

[MIT](https://raw.githubusercontent.com/locus84/Threading/c6f053aac6840c133dc7f2a302de8799ea6daf36/LICENSE)
