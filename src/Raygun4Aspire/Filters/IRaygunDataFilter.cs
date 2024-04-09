namespace Raygun4Aspire.Filters
{
  public interface IRaygunDataFilter
  {
    /// <summary>
    /// Returns whether or not this filter will be able to parse the given data.
    /// This method is called to determine if the subsequent Filter method will be called.
    /// </summary>
    /// <returns><c>true</c>, if the data is can be parsed by the implemented class, <c>false</c> otherwise.</returns>
    /// <param name="data">The raw request data payload.</param>
    bool CanParse(string data);

    /// <summary>
    /// Filter the specified data by checking for the given keys whose values will be removed.
    /// </summary>
    /// <returns>The filtered raw request data payload. Or null if the data could not be filtered.</returns>
    /// <param name="data">The raw request data payload.</param>
    /// <param name="ignoredKeys">Keys whose values should be removed.</param>
    string? Filter(string data, IList<string> ignoredKeys);
  }
}
