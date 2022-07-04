using System;
using System.Collections;
using Xunit;

namespace QLNet.Tests;

public static partial class QAssert
{
    public static void CollectionAreEqual(ICollection expected, ICollection actual)
    {
        Assert.Equal(expected, actual);
    }
    public static void CollectionAreNotEqual(ICollection notExpected, ICollection actual)
    {
        Assert.NotEqual(notExpected, actual);
    }

    public static void AreNotSame(object notExpected, object actual)
    {
        Assert.NotSame(notExpected, actual);
    }

    public static void Fail(string message)
    {
        Assert.True(false, message);
    }

    public static void AreEqual(double expected, double actual, double delta)
    {
        Assert.True(System.Math.Abs(expected - actual) <= delta);
    }

    public static void AreEqual(double expected, double actual, double delta, string message)
    {
        Assert.True(System.Math.Abs(expected - actual) <= delta, message);
    }

    public static void AreEqual<T>(T expected, T actual)
    {
        Assert.Equal(expected, actual);
    }

    public static void AreEqual<T>(T expected, T actual, string message)
    {
        Assert.Equal(expected, actual);
    }

    public static void AreNotEqual<T>(T expected, T actual)
    {
        Assert.NotEqual(expected, actual);
    }

    public static void IsTrue(bool condition)
    {
        Assert.True(condition);
    }

    public static void IsTrue(bool condition, string message)
    {
        Assert.True(condition, message);
    }

    public static void IsFalse(bool condition)
    {
        Assert.False(condition);
    }

    public static void IsFalse(bool condition, string message)
    {
        Assert.False(condition, message);
    }

    /// <summary>
    /// Verifies that an object reference is not null.
    /// </summary>
    /// <param name="obj">The object to be validated</param>
    public static void Require(object obj)
    {
        Assert.NotNull(obj);
    }

    /// <summary>
    /// Verifies an Action throw the specified Exception
    /// </summary>
    /// <typeparam name="T">The Exception</typeparam>
    /// <param name="action">The Action</param>
    public static void ThrowsException<T>(Action action) where T : SystemException
    {
        Assert.Throws<T>(action);
    }

}