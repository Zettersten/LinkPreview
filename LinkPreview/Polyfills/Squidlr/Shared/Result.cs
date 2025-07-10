using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace LinkPreview.Polyfills.Squidlr.Shared;

[StructLayout(LayoutKind.Auto)]
public readonly struct Result<T, TError>
    where TError : struct, Enum
{
    private readonly T value;
    private readonly TError errorCode;

    /// <summary>
    /// Initializes a new successful result.
    /// </summary>
    /// <param name="value">The result value.</param>
    public Result(T value)
    {
        this.value = value;
    }

    /// <summary>
    /// Initializes a new unsuccessful result.
    /// </summary>
    /// <param name="error">The error code.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="error"/> represents a successful code.</exception>
    public Result(TError error)
    {
        Unsafe.SkipInit(out this.value);
        this.errorCode = error;
    }

    /// <summary>
    /// Extracts the actual result.
    /// </summary>
    /// <exception cref="FormatException">The value is unavailable.</exception>
    public T Value
    {
        get
        {
            this.Validate();
            return this.value;
        }
    }

    /// <summary>
    /// Gets a reference to the underlying value.
    /// </summary>
    /// <value>The reference to the result.</value>
    /// <exception cref="FormatException">The value is unavailable.</exception>
    [UnscopedRef]
    [JsonIgnore]
    public ref readonly T ValueRef
    {
        get
        {
            this.Validate();
            return ref this.value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [StackTraceHidden]
    private void Validate()
    {
        if (!this.IsSuccessful)
            this.Throw();
    }

    /// <summary>
    /// Returns the value if present; otherwise return default value.
    /// </summary>
    /// <returns>The value, if present, otherwise <c>default</c>.</returns>
    public T? ValueOrDefault => this.value;

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public TError Error => this.errorCode;

    public bool IsSuccessful
    {
        get { return this.ValueOrDefault is not null; }
    }

    [StackTraceHidden]
    [DoesNotReturn]
    private void Throw() => throw new FormatException($"{this.Error.GetType().Name} errored.");

    /// <summary>
    /// Extracts actual result.
    /// </summary>
    /// <param name="result">The result object.</param>
    public static explicit operator T(in Result<T, TError> result) => result.Value;
}
