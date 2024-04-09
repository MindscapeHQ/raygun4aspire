using Mindscape.Raygun4Net;
using Raygun4Aspire.Filters;

namespace Raygun4Aspire
{
  public class RaygunSettings : RaygunSettingsBase, IRaygunHttpSettings
  {
    public int[]? ExcludedStatusCodes { get; set; }

    public List<string> IgnoreSensitiveFieldNames { get; set; } = new();

    public List<string> IgnoreQueryParameterNames { get; set; } = new();

    public List<string> IgnoreFormFieldNames { get; set; } = new();

    public List<string> IgnoreHeaderNames { get; set; } = new();

    public List<string> IgnoreCookieNames { get; set; } = new();

    public List<IRaygunDataFilter> RawDataFilters { get; } = new();

    public bool IsRawDataIgnored { get; set; }

    public bool IsRawDataIgnoredWhenFilteringFailed { get; set; }

    public bool UseXmlRawDataFilter { get; set; }

    public bool UseKeyValuePairRawDataFilter { get; set; }
  }
}
