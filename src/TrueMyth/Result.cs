﻿using System;
using System.Collections.Generic;

namespace TrueMyth
{
    using Unsafe;

    /// <summary>
    /// A static class that provides factory and extension methods for <see cref="Result{TVal,TErr}"/>.
    /// </summary>
    public static class Result
    {
        /// <summary>
        /// Convenience method to facilitate invoking static <c>Result&lt;TVal,TErr&gt;</c>
        /// methods without type parameters.
        /// </summary>
        public static Result<T, TErr> From<T,TErr>(Maybe<T> maybe, TErr error) => maybe.IsJust 
            ? Result<T,TErr>.Ok((T)maybe)
            : Result<T,TErr>.Err(error);

        /// <summary>
        /// Execute the provided callback, wrapping the return value in an `Result.Ok` or `Result.Err` if there is an exception.
        /// </summary>
        /// <example>
        /// <code>
        /// var aSuccessfulOperation = () => 2 + 2;
        /// var anOkResult = Result.Try(aSuccessfulOperation, "Oh no!"); // Ok&lt;int,string&gt;[4]
        /// 
        /// var aThrowingOperation = () => throw new Exception("Bummer");
        /// var anErrResult = Result.Try(aThrowingOperation, "Oh no!"); // Err&lt;int,string&gt;[Oh no!]
        /// </code>
        /// </example>
        public static Result<TVal, TErr> Try<TVal, TErr>(Func<TVal> fn, TErr error)
        {
            try
            {
                return fn();
            }
            catch
            {
                return error;
            }
        }

        /// <summary>
        /// Execute the provided callback, wrapping the return value in an `Result.Ok`.  If there is an exception, wrap
        /// the result of `errFn` in a `Result.Err`.
        /// </summary>
        /// <example>
        /// <code>
        /// var aSuccessfulOperation = () => 2 + 2;
        /// var anOkResult = Result.Try(aSuccessfulOperation, () => string.Empty); // Ok&lt;int,string&gt;[4]
        /// 
        /// var aThrowingOperation () => throw new Exception("Bummer");
        /// var anErrResult = Result.Try(aThrowingOperation, (exn) => exn.Message); // Err&lt;int,string&gt;[Bummer]
        /// </code>
        /// </example>
        public static Result<TVal, TErr> Try<TVal, TErr>(Func<TVal> fn, Func<TErr> errFn)
        {
            try
            {
                return fn();
            }
            catch
            {
                return errFn();
            }
        }
    }

    /// <summary>
    /// <para>A <c>Result&lt;TValue,TError&gt;</c> is a type representing the value result of an operation which may
    /// fail, with a successful value type of <c>TValue</c> or an error type of <c>TError</c> (pun intended!).  If the
    /// value is present, it is <b>Ok</b>, and if absent, it's <b>Err</b>. There are several ways to check if a <c>Result</c>
    /// is <b>Ok</b> or <b>Err</b>, but the most direct and explicit are the <see cref="IsOk"/> and <see cref="IsErr"/> properties. 
    /// </para>
    /// <para>
    /// This provides a type-safe container for dealing with the possibility that an error occurred, without needing to
    /// scatter
    /// <c>try</c>/<c>catch</c> blocks throughout your codebase. This has two major advantages:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///     You <em>know</em> when an item may have a failure case, unlike cases in which exceptions are thrown (which
    ///     may be thrown from any function with no warning and no help from the type system).
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     The error scenario is a first-class citizen, and the provided helper functions and methods allow you to deal
    ///     with the type in much the same way as you might an array — transforming values if present, or dealing with
    ///     errors instead of necessary.
    ///     </description>
    ///   </item>
    /// </list>
    /// </para>
    /// </summary>
    /// <example>
    /// To make this concrete, let's look at an example.see  Without TrueMyth, you might have the following C♯:
    /// <code>
    /// int MightSucceed(bool doesSucceed) 
    /// {
    ///     if (!doesSucceed) 
    ///         throw new Exception("hey guess what? it didn't succeed.see");
    ///
    ///     return 42;
    /// }
    ///
    /// int Main(string[] args) 
    /// {
    ///     var doubleTheAnswer = MightSucceed(true) * 2; Console.WriteLine(doubleTheAnswer); 
    ///     // 84; this is fine
    ///
    ///     var doubleAnError = MightSucceed(false) * 2; Console.Write(doubleAnError); 
    ///     // oops!  we never even get here.
    /// }
    /// </code>
    /// If we wanted to <em>handle</em> that error, we'd need to first of all know that the function could throw an
    /// error. Assuming we knew that — progbably we'd figure it out via painful discovery at runtime — then we'd need to
    /// wrap it up in a <c>try</c>/<c>catch</c> block:
    /// <code>
    /// int Main(string[] args) 
    /// {
    ///     try 
    ///     {
    ///         var doubleTheAnswer = MightSucceed(true) * 2; 
    ///         Console.WriteLine(doubleTheAnswer); // 84; this is fine
    ///
    ///         var doubleAnError = MightSucceed(false) * 2; 
    ///         Console.WriteLine(doubleAnError);
    ///     } 
    ///     catch(Exception exn)
    ///     {
    ///         Console.WriteLine(exn.Message);
    ///     }
    /// }
    /// </code>
    /// This is a pain to work with!
    ///
    /// The next thing we might try is returning an error code and mutating an object passed in (e.g. 
    /// <c>bool TryMightSucceed(bool doesSucceed, out int result)</c>). But that has a few problems:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///     You have to mutate an object. This doesn't work for simple items like numbers, and it can also be pretty
    ///     unexpected behavior at times — you want to <em>know</em> when something is going to change, and mutating
    ///     freely throughout a library or application makes that impossible.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     You have to make sure to actually check the return code to make sure it's valid. In theory, we're all
    ///     disciplined enough to always do that. In practice, we often end up reasoning,
    ///     <em>Well, this particular call can never fail... </em> (but of course, it probably can, just not in the way
    ///     we expect).
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     We don't have a good way to return a <em>reason</em> for the error. We end up needing to introduce another
    ///     parameter, designed to be mutated, to make sure that's possible.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     Even if you go to all the trouble of doing all that, you need to make sure — every time — that you use only
    ///     the error value if the return code specified an error, and only the success value if the return code
    ///     specified that it succeeded.
    ///     </description>
    ///   </item>
    /// </list>
    /// Our way out is <c>Result&lt;TValue,TError&gt;</c>. It lets us just return one thing from a function, which
    /// encapsulates the possiblility of failure in the very thing we return. We get:
    /// <list>
    ///   <item>
    ///     <description>
    ///     the simplicity of jsut dealing with the return value of a function (no <c>try</c>/<c>catch</c>
    ///     to worry about!)
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     the ease of expressing an error we got without throwing an exception
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     the explicitness about success or failure that we got with a return code
    ///     </description>
    ///   </item>
    /// </list>
    /// Here's what that same example from above would look like using <c>Result</c>:
    /// <code>
    /// Result&lt;int,string&gt; MightSucceed(bool doesSucceed) => doesSucceed 
    ///     ? Result&lt;int,string&gt;.Ok(42) 
    ///     : Result&lt;int,string&gt;.Err("something went wrong!");
    ///
    /// int Main(string[] args) 
    /// {
    ///     int Double(int x) = x * 2;
    ///
    ///     var doubleTheAnswer = MightSucceed(true).Select(Double); 
    ///     Console.WriteLine(doubleTheAnswer); // Ok&lt;int,string&gt;[84]
    ///
    ///     var doubleAnErr = MightSucceed(false).Select(Double); 
    ///     Console.WriteLine(doubleAnErr); // Err&lt;int,string&gt;[something went wrong]
    /// }
    /// </code>
    /// Note that if we tried to call <c>MightSucceed(true)*2</c> here, we'd get a type error — this wouldn't make it
    /// past the compile step.
    /// </example>
    /// <typeparam name="TVal">The value type for **Ok** values.</typeparam>
    /// <typeparam name="TErr">The value type for **Err** values.</typeparam>
    
    public sealed class Result<TVal, TErr> : IComparable, IComparable<Result<TVal,TErr>>
    {
        #region Private Fields

        private readonly TVal _value;
        private readonly TErr _error;
        private readonly bool _isOk;

        #endregion

        #region Public Properties

        /// <summary>
        /// Is this <c>Result</c> an <b>Ok</b>? This property is <c>true</c> if so.
        /// </summary>
        public bool IsOk => _isOk;

        /// <summary>
        /// This is merely the reverse of <see cref="IsOk"/>; if <c>this</c> is an <b>Err</b>, then
        /// the property is <c>true</c>.
        /// </summary>
        public bool IsErr => !_isOk;

        #endregion

        #region Constructors

        private Result() {}

        private Result(TVal value, TErr error, bool isOk)
        {
            // basically just a defense against result instances that don't make sense.
            // e.g., new Result<int,string>(0, "error", true) doesn't make sense, and
            // neither does new Result<int,string>(7, null, false);
            if (isOk)
            {
                if (EqualityComparer<TVal>.Default.Equals(value, default(TVal)) && !EqualityComparer<TErr>.Default.Equals(error, default(TErr)))
                {
                    throw new ArgumentException("Invalid state for Ok result.", nameof(value));
                }
            }
            else
            {
                if (!EqualityComparer<TVal>.Default.Equals(value, default(TVal)) && EqualityComparer<TErr>.Default.Equals(error, default(TErr)))
                {
                    throw new ArgumentException("Invalid state for Err result.", nameof(error));
                }
            }

            _value = value;
            _error = error;
            _isOk = isOk;
        }

        #endregion

        #region Instance Methods
        /// <summary>
        /// You can think of this like a short-circuiting logical "and" operation on a <c>Result</c> type. If result 
        /// is Ok, then the result is the <c>andResult</c>. If result is Err, the result is the Err.
        /// 
        /// This is useful when you have another <c>Result</c> value you want to provide if and only if you have an Ok 
        /// – that is, when you need to make sure that if you Err, whatever else you're handing a <c>Result</c> to 
        /// also gets that Err.
        /// 
        /// Notice that, unlike in <see cref="Map{UValue}(Func{TVal,UValue})"/> or its variants, the original 
        /// result is not involved in constructing the new Result.
        /// </summary>
        /// <param name="andResult">Result returned if <c>this</c> is an Ok.</param>
        public Result<T, TErr> And<T>(Result<T,TErr> andResult) => this._isOk ? andResult : new Result<T,TErr>(default(T), this._error, false);

        /// <summary>
        /// Apply a function to the wrapped value if Ok and return a new Ok containing the resulting value; or if it 
        /// is Err return it unmodified.
        /// 
        /// This differs from <see cref="Map{UValue}(Func{TVal,UValue})"/> in that <c>thenFn</c> returns another 
        /// <c>Result</c>. You can use <c>andThen</c> to combine two functions which both create a Result from an 
        /// unwrapped type.
        /// </summary>
        /// <param name="bindFn">Function that returns Result if <c>this</c> is an Ok.</param>
        /// <typeparam name="T">Ok value type of result of <c>thenFn</c>.</typeparam>
        public Result<T, TErr> AndThen<T>(Func<TVal, Result<T, TErr>> bindFn) => this._isOk ? bindFn(this._value) : this.Map(val => default(T));

        /// <summary>
        /// Map over a Result instance: apply the function to the wrapped value if the instance is Ok, and return the wrapped error value 
        /// wrapped as a new Err of the correct type (Result&lt;UValue, TError&gt;) if the instance is Err.
        /// 
        /// <c>Result.Select</c> works a lot like it does for <see cref="IEnumerable{T}"/>, but with one important difference. Both 
        /// <c>Result</c> and <c>IEnumerable</c> are containers for other kinds of items, but where <c>IEnumerable</c>> has 0 to n items, 
        /// a <c>Result</c> always has exactly one item, which is either a success or an error instance.
        /// 
        /// Where <c>IEnumerable.Select</c> will apply the mapping function to every item in the enumeration (if there are any), 
        /// <c>Result.Select</c> will only apply the mapping function to the (single) element if an Ok instance, if there is one.
        /// 
        /// If you have no items in an array of numbers named foo and call foo.Select(x => x + 1), you'll still some have an array with 
        /// nothing in it. But if you have any items in the array (<c>{2, 3}</c>), and you call <c>foo.Select(x => x + 1)</c> on it, 
        /// you'll get a new array with each of those items inside the array "container" transformed (<c>{3, 4}</c>).
        /// 
        /// With <c>Result.Select</c>, the Err variant is treated by the map function kind of the same way as the empty array case: 
        /// it's just ignored, and you get back a new <c>Result</c> that is still just the same Err instance. But if you have an 
        /// Ok variant, the map function is applied to it, and you get back a new <c>Result</c> with the value transformed, and still 
        /// wrapped in an Ok.
        /// </summary>
        /// <param name="mapFn">Mapping function applied to Ok value.</param>
        /// <typeparam name="T">Destination value type resulting from the mapping function.</typeparam>
        /// <example>
        /// <code>
        /// long Double(int n) => n * 2;
        /// 
        /// var anOk = Result&lt;int,string&gt;.Ok(12);
        /// var mappedOk = anOk.Select(Double);
        /// Console.WriteLine(mappedOk.ToString()); // Ok&lt;long,string&gt;[12]
        /// 
        /// var anErr = Result&lt;int,string&gt;.Err("error");
        /// var mappedErr = anErr.Select(Double);
        /// Console.WriteLine(mappedErr.ToString()); // Err&lt;long,string&gt;[error]
        /// </code>
        /// </example>
        public Result<T, TErr> Map<T>(Func<TVal,T> mapFn) => this._isOk
            ? new Result<T,TErr>(mapFn(this._value), default(TErr), true)
            : new Result<T,TErr>(default(T), this._error, false);

        // mapOr
        /// <summary>
        /// Map over a <c>Result</c> instance as in map and get out the value if result is an Ok, or return a default value if result is an Err.
        /// </summary>
        /// <param name="mapFn">Mapping function to apply to wrapped Ok value.</param>
        /// <param name="defaultValue">Fallback value to return if <c>this</c> is an Err.</param>
        /// <typeparam name="T">Destination type resulting from mapping function <c>mapFn</c>.</typeparam>
        /// <returns></returns>
        public T MapReturn<T>(Func<TVal,T> mapFn, T defaultValue) => this._isOk  ? mapFn(this._value) : defaultValue;

        /// <summary>
        /// Map over a <c>Result</c>, exactly as in map, but operating on the value wrapped in an Err instead of the value wrapped in the Ok. 
        /// This is handy for when you need to line up a bunch of different types of errors, or if you need an error of one shape to 
        /// be in a different shape to use somewhere else in your codebase.
        /// </summary>
        /// <param name="mapFn">Mapping function to apply to error wrapped in Err <c>Result</c>.</param>
        /// <typeparam name="T">Mapped error type resulting from mapping function.</typeparam>
        public Result<TVal, T> MapErr<T>(Func<TErr,T> mapFn) => !this._isOk
            ? new Result<TVal,T>(default(TVal), mapFn(this._error), false)
            : new Result<TVal,T>(this._value, default(T), true);

        /// <summary>
        /// Performs the same basic functionality as <see cref="UnwrapOrElse(Func{TErr,TVal})"/>, but instead of simply unwrapping the value if 
        /// it is Ok and applying a function to generate the same type if it is Err, lets you supply functions which may transform the wrapped 
        /// type if it is Ok or get a default value for Err.
        /// 
        /// This is kind of like a poor man's version of pattern matching, for which C♯ has only limited support.
        /// </summary>
        /// <param name="ok"></param>
        /// <param name="err"></param>
        /// <typeparam name="T"></typeparam>
        /// <example>
        /// Instead of code like this:
        /// <code>
        /// var anOk = Result&lt;int,string&gt;.Ok(42);
        /// int value;
        /// switch(anOk.IsOk)
        /// {
        ///     true:
        ///         value = anOk.UnsafelyUnwrap() * 2;
        ///         break;
        ///     false:
        ///         value = anOk.UnsafelyUnwrapErr().Length + 2;
        ///         break;
        /// }
        /// Console.WriteLine(value); // 84
        /// </code>
        /// we can write code like this:
        /// <code>
        /// var anOk = Result&lt;int,string&gt;.Ok(42);
        /// 
        /// var value = anOk.Match(
        ///     ok: val => val * 2,
        ///     err: errval => errval.Length + 2;
        /// );
        /// Console.WriteLine(value); // 84
        /// </code>
        /// </example>
        public T Match<T>(Func<TVal,T> ok, Func<TErr,T> err) => this._isOk ? ok(this._value) : err(this._error);

        /// <summary>
        /// Provides similar functionality as <see cref="Match{T}(Func{TVal,T}, Func{TErr,T})"/>, but with no return type.
        /// </summary>
        public void Match(Action<TVal> ok, Action<TErr> err) 
        {
            if (this.IsOk)
            {
                ok(this._value);
            }
            else
            {
                err(this._error);
            }
        }

        /// <summary>
        /// Provide a fallback for a given <c>Result</c>. Behaves like a logical or: if the result value is an Ok, returns that result; otherwise, 
        /// returns the <c>defaultResult</c> value.
        /// 
        /// This is useful when you want to make sure that something which takes a <c>Result</c> always ends up getting an Ok variant, by 
        /// supplying a default value for the case that you currently have an Err.
        /// </summary>
        /// <param name="defaultResult"></param>
        public Result<TVal, T> Or<T>(Result<TVal, T> defaultResult) => this._isOk ? new Result<TVal,T>(this._value, default(T), true) : defaultResult;

        /// <summary>
        /// Like <see cref="Or{UError}(Result{TVal,UError})"/>, but using a function to construct the alternative Result.
        /// 
        /// Sometimes you need to perform an operation using other data in the environment to construct the fallback 
        /// value. In these situations, you can pass a function (which may be a closure) as the elseFn to generate 
        /// the fallback Result&lt;TValue,TError&gt;. It can then transform the data in the Err to something usable 
        /// as an Ok, or generate a new Err instance as appropriate.
        /// 
        /// Useful for transforming failures to usable data.
        /// </summary>
        public Result<TVal, T> OrElse<T>(Func<Result<TVal,T>> elseFn) => this._isOk ? new Result<TVal,T>(this._value, default(T), true) : elseFn();

        /// <summary>
        /// An alias for <see cref="Map{UValue}(Func{TVal, UValue})"/>.
        /// </summary>
        public Result<T, TErr> Select<T>(Func<TVal, T> mapFn) => Map(mapFn);

        /// <summary>
        /// An alias for <see cref="MapErr{UError}(Func{TErr,UError})"/>
        /// </summary>
        public Result<TVal, T> SelectErr<T>(Func<TErr, T> mapFn) => MapErr(mapFn);

        /// <summary>
        /// An alias for <see cref="AndThen{UValue}(Func{TVal, Result{UValue, TErr}})"/>.
        /// </summary>
        public Result<T, TErr> SelectMany<T>(Func<TVal, Result<T, TErr>> bindFn) => AndThen(bindFn);

        /// <summary>
        /// Convert a <c>Result</c> to a <see cref="Maybe{TValue}"/>.
        /// 
        /// The converted type will be Just if the <c>Result</c> is Ok or Nothing if the <c>Result</c> is Err; the 
        /// wrapped error value will be discarded.
        /// </summary>
        public Maybe<TVal> ToMaybe() => this._isOk ? Maybe<TVal>.Of(this._value) : Maybe<TVal>.Nothing;

        /// <summary>
        /// Safely get the value out of the <b>Ok</b> variant of a <c>Result</c>. This is the recommended way to get a value
        /// out of a <c>Result</c> most of the time.
        /// </summary>
        /// <param name="defaultValue">Fallback value to be returned if <c>this</c> is <b>Err</b>.</param>
        /// <example>
        /// <code>
        /// var anOk = Result&lt;int,string&gt;.Ok(1);
        /// Console.WriteLine(anOk.UnwrapOr(0)); // 1
        /// 
        /// var anErr = Result&lt;int,string&gt;.Err("error");
        /// Console.WriteLine(anErr.UnwrapOr(0)); // 0
        /// </code>
        /// </example>
        public TVal UnwrapOr(TVal defaultValue) => this._isOk ? _value : defaultValue;

        /// <summary>
        /// Safely get the value out of a <c>Result&lt;TValue,TError&gt;</c> by returning the wrapped value if it is <b>Ok</b>
        /// or by applying <c>elseFn</c> if it is <b>Err</b>.
        /// 
        /// This is useful when you need to generate a value (e.g. by using current values in the environment – whether 
        /// preloaded or by local closure) instead of having a single default value available (as in <see cref="UnwrapOr(TVal)"/>).
        /// </summary>
        /// <param name="elseFn">Function to apply to map <c>TError</c> to <c>TVaue</c>.</param>
        /// <example>
        /// <code>
        /// var someOtherValue = 2;
        /// var handleError = (string err) => err.Length + someOtherValue;
        /// 
        /// var anOk = Result&lt;int,string&gt;.Ok(42);
        /// Console.WriteLine(anOk.UnwrapOrElse(handleError)); // 42
        /// 
        /// var anErr = Result&lt;int,string&gt;.Err("error");
        /// Console.WriteLine(anErr.UnwrapOrElse(handleError)); // error
        /// </code>
        /// </example>
        public TVal UnwrapOrElse(Func<TErr,TVal> elseFn) => this._isOk ? _value : elseFn(this._error);

        #endregion

        #region Object Overrides

        /// <summary>
        /// Produces a string format like the following: "Ok&lt;TValue,TError&gt;[value]" or "Err&lt;TValue,TError&gt;[error]".
        /// </summary>
        public override string ToString() => this._isOk 
            ? $"Ok<{typeof(TVal).Name}>[{this._value}]" 
            : $"Err<{typeof(TErr).Name}>[{this._error}]";

        /// <exclude/>
        public override bool Equals(object o)
        {
            if (o == null || GetType() != o.GetType())
            {
                return false;
            }

            var r = o as Result<TVal,TErr>;
            if (this._isOk)
            {
                return EqualityComparer<TVal>.Default.Equals(this._value,r._value);
            }
            else
            { 
                return EqualityComparer<TErr>.Default.Equals(this._error,r._error);
            }
        }

        /// <exclude/>
        public override int GetHashCode()
        {
            unchecked
            {
                const int prime = 29;
                int hash = 17;
                hash = hash * prime + typeof(TVal).GetHashCode();
                hash = hash * prime + typeof(TErr).GetHashCode();
                hash = hash * prime + this._isOk.GetHashCode();
                if (this._isOk)
                {
                    hash = hash + prime + this._value.GetHashCode();
                }
                else
                {
                    hash = hash + prime + this._error.GetHashCode();
                }
                return hash;
            }
        }

        #endregion

        #region IComparable Implementation
        
        /// <exclude/>
        public int CompareTo(Result<TVal,TErr> otherResult)
        {
            if (otherResult == null)
            {
                return 1;
            }

            if (object.ReferenceEquals(this, otherResult))
            {
                return 0;
            }

            if (this.IsErr)
            {
                if (otherResult.IsOk)
                {
                    return -1;
                }
                else if(typeof(IComparable).IsAssignableFrom(typeof(TErr)))
                {
                    var thisErr = this.UnsafelyUnwrapErr() as IComparable;
                    var thatErr = otherResult.UnsafelyUnwrapErr();
                    return thisErr.CompareTo(thatErr);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                if (otherResult.IsErr)
                {
                    return 1;
                }
                else if (typeof(IComparable).IsAssignableFrom(typeof(TVal)))
                {
                    var thisValue = this.UnsafelyUnwrap() as IComparable;
                    var thatValue = otherResult.UnsafelyUnwrap();
                    return thisValue.CompareTo(thatValue);
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <exclude/>
        public int CompareTo(object obj)
        {
            if (GetType() != obj.GetType())
            {
                throw new ArgumentException($"Parameter of different type: {obj.GetType()}", nameof(obj));
            }

            return CompareTo((Result<TVal,TErr>)obj);
        }

        #endregion

        #region Static Methods & Operators

        /// <summary>
        /// A factory method for creating <b>Err</b> <c>Result</c> instances.
        /// </summary>
        /// <param name="err">The error value wrapped by the <c>Result</c></param>
        public static Result<TVal, TErr> Err(TErr err) => new Result<TVal, TErr>(default(TVal), err, false);

        /// <summary>
        /// Transform a <c>Maybe&lt;T&gt;</c> into a <c>Result&lt;T,TError&gt;</c>. If the <c>Maybe</c>
        /// is a <b>Just</b>, its value will be wrapped in the <b>Ok</b> variant; if it is a <b>Nothing</b> the
        /// <c>errValue</c> will be wrapped in the <b>Err</b> variant.
        /// </summary>
        /// <param name="maybe"></param>
        /// <param name="errValue"></param>
        /// <returns></returns>
        public static Result<TVal,TErr> From(Maybe<TVal> maybe, TErr errValue) => maybe.ToResult(errValue);

        /// <summary>
        /// A factory method for creating <b>Ok</b> <c>Result</c> instances.
        /// <note><c>null</c> is allowed by the type signature, but it is highly recommended
        /// to use <see cref="Maybe{TValue}"/> rather than a <c>null</c> as a result.
        /// </note>
        /// </summary>
        /// <param name="value">The success value wrapped by the <c>Result</c></param>
        public static Result<TVal, TErr> Ok(TVal value) => new Result<TVal, TErr>(value, default(TErr), true);

        /// <summary>
        /// Implicit conversion operator from <c>Result&lt;TValue, TError&gt;</c> to <c>TValue</c>. Equivalent to <c>result.UnsafelyUnwrap</c>, but 
        /// is helpful for eliminating unnecessary syntax.
        /// <note>
        /// This operator will not work if <c>TValue</c> and <c>TError</c> are the same because there will be no
        /// way for the type inference system in C♯ to resolve them.
        /// </note>
        /// </summary>
        /// <example>
        /// This allows us to simplify code that would otherwise be needlessly verbose. For example, without this operator we might write the following:
        /// <code>
        /// int SomeOperation()
        /// {
        ///     var resultOfOtherOperation = SomeOtherOperation();
        ///     
        ///     if (resultOfOtherOperation.IsOk)
        ///     {
        ///         return resultOfOtherOperation.UnsafelyUnwrap();
        ///     }
        ///     else
        ///     {
        ///         throw new Exception("oops! SomeOtherOperation failed!");    
        ///     }
        /// }
        /// </code>
        /// Using this implicit conversion, this could be simplified:
        /// <code>
        /// int SomeOperation() => return SomeOtherOperation();
        /// </code>
        /// <note>
        /// There are actually safer <em>and</em> more semantic methods for handling results than this, and we recommend the use 
        /// of <see cref="UnwrapOr(TVal)"/> or any of the several methods provided for such use; however, we recognize that in 
        /// spite of our desire to avoid exceptions and <c>null</c>, reality insists that sometimes we have to use them, particularly
        /// when making changes to incumbent software.
        /// </note>
        /// </example>
        public static explicit operator TVal(Result<TVal, TErr> result) => 
            result._isOk ? result._value : throw new InvalidOperationException("Invalid conversion to value type.");

        /// <summary>
        /// This works similarly to <see cref="explicit operator TVal(Result{TVal,TErr})"/>, but instead of conversion to a <c>TValue</c>,
        /// the implicit conversion is to a <c>TError</c>.
        /// <note>
        /// This operator will not work if <c>TValue</c> and <c>TError</c> are the same because there will be no
        /// way for the type inference system in C♯ to resolve them.
        /// </note>
        /// </summary>
        /// <param name="result"></param>
        public static explicit operator TErr(Result<TVal, TErr> result) =>
            !result._isOk ? result._error : throw new InvalidOperationException("Invalid conversion to error type.");

        /// <summary>
        /// Implicit conversion operator from <c>TValue</c> to <c>Result&lt;TValue,TError&gt;</c>.  As with the other operators, this
        /// is intended to reduce boilerplate code required by the type system and allow you to achieve maximum leverage of type
        /// inference.
        /// 
        /// <note>
        /// This operator will not work if <c>TValue</c> and <c>TError</c> are the same because there will be no
        /// way for the type inference system in C♯ to resolve them.
        /// </note>
        /// </summary>
        /// <param name="value"></param>
        /// <example>
        /// When constructing a <c>Result&lt;TValue,TError&gt;</c> the type system can be a little bit fussy. We might write:
        /// <code>
        /// Result&lt;int,string&gt; SomeOperation()
        /// {
        ///     try
        ///     {
        ///         int result = SomeOtherOperation(); // db query, file i/o, etc.
        ///         return Result&lt;int,string&gt;.Ok(result);
        ///     }
        ///     catch (Exception exn)
        ///     {
        ///         return Result&lt;int,string&gt;.Err(exn.Message);
        ///     }
        /// }
        /// </code>
        /// Using these operators, we can write it like this:
        /// <code>
        /// Result&lt;int,string&gt; SomeOperation()
        /// {
        ///     try
        ///     {
        ///         int result = SomeOtherOperation(); // db query, file i/o, etc.
        ///         return result;
        ///     }
        ///     catch (Exception exn)
        ///     {
        ///         return exn.Message;
        ///     }
        /// }
        /// </code>
        /// This will be particularly powerful when combined with similar operators defined for <see cref="Maybe{TValue}"/> when you 
        /// have a return type like <c>Result&lt;Maybe&lt;TValue&gt;,TError&gt;</c>.
        /// </example>
        public static implicit operator Result<TVal,TErr>(TVal value) => new Result<TVal,TErr>(value, default(TErr), true);

        /// <summary>
        /// Implicit conversion operator from <c>TError</c> to <c>Result&lt;TValue,TError&gt;</c>.  As with the other operators, this
        /// is intended to reduce boilerplate code required by the type system and allow you to achieve maximum leverage of type
        /// inference.
        /// 
        /// <note>
        /// This operator will not work if <c>TValue</c> and <c>TError</c> are the same because there will be no
        /// way for the type inference system in C♯ to resolve them.
        /// </note>
        /// 
        /// See examples provided with <see cref="implicit operator Result{TVal,TErr}(TVal)"/>.
        /// </summary>
        public static implicit operator Result<TVal,TErr>(TErr error) => new Result<TVal,TErr>(default(TVal), error, false);

        #endregion
    }
}