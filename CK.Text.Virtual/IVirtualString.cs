namespace CK.Text.Virtual
{
    /// <summary>
    /// This interface contains definitions for <see cref="VirtualStringMatcher"/> paramater.
    /// </summary>
    public interface IVirtualString
    {
        /// <summary>
        /// Returns the current length available.
        /// </summary>
        /// <value>The length.</value>
        long Length { get; }

        /// <summary>
        /// Gets the char value of <see cref="IVirtualString"/> at position <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index to look at.</param>
        /// <returns>The character found.</returns>
        char this[long index] { get; }

        /// <summary>
        /// Returns a string from the <see cref="IVirtualString"/> in the given range.
        /// </summary>
        /// <param name="index">The index to start the string from.</param>
        /// <param name="length">The length of the string.</param>
        /// <returns>The string containing characters in the range.</returns>
        string GetText( long index, int length );
    }
}
