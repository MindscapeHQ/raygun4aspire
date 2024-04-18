﻿using Raygun4Aspire.Filters;

namespace Raygun4Aspire
{
  public interface IRaygunHttpSettings
  {
    List<string> IgnoreSensitiveFieldNames { get; }
    List<string> IgnoreQueryParameterNames { get; }
    List<string> IgnoreFormFieldNames { get; }
    List<string> IgnoreHeaderNames { get; }
    List<string> IgnoreCookieNames { get; }
    List<IRaygunDataFilter> RawDataFilters { get; }

    bool IsRawDataIgnored { get; }
    bool IsRawDataIgnoredWhenFilteringFailed { get; }
    bool UseXmlRawDataFilter { get; }
    bool UseKeyValuePairRawDataFilter { get; }
  }
}
