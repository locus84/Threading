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
You also can enqueue async method into TaskFiber.
```cs
myFiber.Enqueue(async () => await Dosomthing());
```
\
**Be aware!**

Because the **await** keyword captures current context and use it later execution,\
A short living thread pool thread can be messed up by this keyword.

```cs
//let's say you're enqueuing async task
void SomeFunction()
{
    myFiber.Enqueue(() => SomeAsyncFunction());
}

async Task SomeAsyncFunction()
{
    //In here, we assure that the function is calling in TaskFiber ThreadPool Thread.
    //You can check this anytime you want
    Console.WriteLine(myFiber.IsCurrentThread);
    //returns true

    await Task.Delay(1000);
    //but after calling above await keyword, the execution context can be somewhere else
    Console.WriteLine(myFiber.IsCurrentThread);
    //returns false
}
```

But here comes the rescue. **TaskFiber.IntoFiber()**

```cs
async Task SomeAsyncFunction()
{
    //....
    await Task.Delay(1000);
    //simply await TaskFiber.IntoFiber() here.
    await myFiber.IntoFiber();

    Console.WriteLine(myFiber.IsCurrentThread);
    //returns true

    //if you're awaiting a task, then it can be simpler
    await Task.Delay(1000).IntoFiber(myFiber);

    Console.WriteLine(myFiber.IsCurrentThread);
    //returns true
}
```
\
Common mistakes

```cs
//if you're enqueuing async task directly
void SomeFunction()
{
    await myFiber.Enqueue(SomeAsyncFunction());
}

async Task SomeAsyncFunction()
{
    //because the execution of SomeAsyncFuction continues from caller context,
    Console.WriteLine(myFiber.IsCurrentThread);
    //returns false
}
```

But actually you don't have to even enqueue in this case.
```cs
void SomeFunction()
{
    //assigning is just to prevent warning
    var asyncTask = SomeAsyncFunction();
}

async Task SomeAsyncFunction()
{
    await myFiber.IntoFiber();
    //this will promise you're in the TaskFiber as always..
    //...so calling
    Console.WriteLine(myFiber.IsCurrentThread);
    //returns true
}
```
<br />


## Motivation

When I was in .Net childhood, I fall in love deeply with **Retlang** which is a concorrency library like **Jetlang** in Java world.\
It gave me simple approches how to use multicore environment without race conditions, like functional languages such as **Erlang**.\
And then, **TPL**(Task Parallel Library) and **async await** keywords are introduced, my codes are get simplar than before.\
But still, I wanted to use somewhat **Actor Based Concurrency** as before, I made a simple library that utilize Task and 'async await' funtionality.

## Installation

Download source files and include them into your project.
I'll post this project to Nuget asap.


## License

[MIT](https://raw.githubusercontent.com/locus84/Threading/c6f053aac6840c133dc7f2a302de8799ea6daf36/LICENSE)
