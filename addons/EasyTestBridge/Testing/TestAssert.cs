using System;

namespace EasyTestBridge.Testing;

/// <summary>
/// 断言失败异常
/// </summary>
public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}

/// <summary>
/// 测试断言方法集合
/// </summary>
public static class TestAssert
{
    public static void isTrue(bool condition, string? message = null)
    {
        if (!condition)
            throw new AssertionException(message ?? "expected true, got false");
    }

    public static void isFalse(bool condition, string? message = null)
    {
        if (condition)
            throw new AssertionException(message ?? "expected false, got true");
    }

    public static void areEqual<T>(T expected, T actual, string? message = null)
    {
        if (!Equals(expected, actual))
            throw new AssertionException(
                message ?? $"expected <{expected}>, got <{actual}>");
    }

    public static void areNotEqual<T>(T expected, T actual, string? message = null)
    {
        if (Equals(expected, actual))
            throw new AssertionException(
                message ?? $"expected not <{expected}>, but got it");
    }

    public static void isNotNull(object? value, string? message = null)
    {
        if (value == null)
            throw new AssertionException(message ?? "expected non-null, got null");
    }

    public static void isNull(object? value, string? message = null)
    {
        if (value != null)
            throw new AssertionException(message ?? $"expected null, got <{value}>");
    }

    public static void fail(string message = "test explicitly failed")
    {
        throw new AssertionException(message);
    }
}
