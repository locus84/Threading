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

Because the **await** keyword captures current context and use it later execution,\
A short living thread pool thread can be messed up by this keyword.

```cs
async Task SomeFunction()
{
    await myFiber.Enqueue(() => SomeAsyncFunction("Action Style"));
    await SomeAsyncFunction("Direct call"));
}

async Task SomeAsyncFunction(string log)
{
    //You can check where is your context anytime
    Console.WriteLine(log + " : "  + myFiber.IsCurrentThread);
    // - "Action Styple : true"
    // - "Direct call : false"

    await Task.Delay(1000);
    //after calling above await keyword, the execution context can be somewhere else
    Console.WriteLine(myFiber.IsCurrentThread);
    //returns always false
}
```

But here comes the rescue. **TaskFiber.IntoFiber()**

```cs
async Task SomeAsyncFunction()
{
    ...
    await Task.Delay(1000);
    //simply await TaskFiber.IntoFiber() here.
    await myFiber.IntoFiber();

    Console.WriteLine(myFiber.IsCurrentThread);
    //returns true

    //if you're awaiting a task, then it can be simpler
    await Task.Delay(1000).IntoFiber(myFiber);

    Console.WriteLine(myFiber.IsCurrentThread);
    //returns true

    //you can even omit fiber parameter if you sure you're in a thread fiber(will introduce in 1.0.2)
    //and wanna back to that fiber after await.
    await Task.Delay(1000).IntoFiber();
    Console.WriteLine(myFiber.IsCurrentThread);
    //returns true
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
