using System.Xml.Linq;

namespace Raygun4Aspire.Filters
{
  public class RaygunXmlDataFilter : IRaygunDataFilter
  {
    private const string FilteredValue = "[FILTERED]";

    public bool CanParse(string data)
    {
      if (!string.IsNullOrEmpty(data))
      {
        var index = data.TakeWhile(char.IsWhiteSpace).Count();

        if (index < data.Length)
        {
          var firstChar = data.ElementAt(index);
          if (firstChar.Equals('<'))
          {
            return true;
          }
        }
      }

      return false;
    }

    public string? Filter(string data, IList<string> ignoredKeys)
    {
      try
      {
        var doc = XDocument.Parse(data);

        // Begin the filtering.
        FilterElementsRecursive(doc.Descendants(), ignoredKeys);

        return doc.ToString(SaveOptions.DisableFormatting);
      }
      catch
      {
        return null;
      }
    }

    private static void FilterElementsRecursive(IEnumerable<XElement> descendants, IList<string> ignoredKeys)
    {
      foreach (var element in descendants)
      {
        if (element.HasElements)
        {
          // Keep searching for the outer leaf.
          FilterElementsRecursive(element.Descendants(), ignoredKeys);
        }

        // Remove sensitive values.
        FilterElement(element, ignoredKeys);
      }
    }

    private static void FilterElement(XElement element, IList<string> ignoredKeys)
    {
      // Check if a value is set first and then if this element should be filtered.
      if (!string.IsNullOrEmpty(element.Value) && ignoredKeys.Any(f => f.Equals(element.Name.LocalName, StringComparison.OrdinalIgnoreCase)))
      {
        element.Value = FilteredValue;
      }

      if (element.HasAttributes)
      {
        foreach (var attribute in element.Attributes())
        {
          // Check if a value is set first and then if this attribute should be filtered.
          if (!string.IsNullOrEmpty(attribute.Value) && ignoredKeys.Any(f => f.Equals(attribute.Name.LocalName, StringComparison.OrdinalIgnoreCase)))
          {
            attribute.Value = FilteredValue;
          }
        }
      }
    }
  }
}
