﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Mindscape.Raygun4Net;
using Raygun4Aspire.Filters;
using System.Collections;
using System.Globalization;
using System.Text;

namespace Raygun4Aspire.Builders
{
  public class RaygunRequestMessageBuilder
  {
    private const int MaxRawDataLength = 4096; // bytes

    public static async Task<RaygunRequestMessage> Build(HttpContext? context, RaygunSettings options)
    {
      if (context == null)
      {
        return new RaygunRequestMessage();
      }

      var request = context.Request;

      var message = new RaygunRequestMessage
      {
        HostName = request.Host.Value,
        Url = request.GetDisplayUrl(),
        HttpMethod = request.Method,
        IPAddress = GetIpAddress(context.Connection),
        QueryString = GetQueryString(request, options),
        Cookies = GetCookies(request, options),
        RawData = GetRawData(request, options),
        Headers = GetHeaders(request, options)
      };

      message.Form = await GetForm(request, options);

      return message;
    }

    private static string GetIpAddress(ConnectionInfo connection)
    {
      var ip = connection.RemoteIpAddress ?? connection.LocalIpAddress;

      if (ip == null)
      {
        return "";
      }

      int? port = connection.RemotePort == 0 ? connection.LocalPort : connection.RemotePort;

      if (port != 0)
      {
        return ip + ":" + port.Value;
      }

      return ip.ToString();
    }

    private static IDictionary GetQueryString(HttpRequest request, IRaygunHttpSettings options)
    {
      IDictionary queryString;

      try
      {
        queryString = ToDictionary(request.Query, s => IsIgnored(s, options.IgnoreQueryParameterNames), s => IsIgnored(s, options.IgnoreSensitiveFieldNames));
      }
      catch (Exception e)
      {
        queryString = new Dictionary<string, string> { { "Failed to retrieve", e.Message } };
      }

      return queryString;
    }

    private static IList GetCookies(HttpRequest request, IRaygunHttpSettings options)
    {
      IList cookies;
      try
      {
        cookies = request.Cookies.Where(c => !IsIgnored(c.Key, options.IgnoreCookieNames) && !IsIgnored(c.Key, options.IgnoreSensitiveFieldNames))
          .Select(c => new RaygunRequestMessage.Cookie(c.Key, c.Value)).ToList();
      }
      catch (Exception e)
      {
        cookies = new List<string> { "Failed to retrieve cookies: " + e.Message };
      }

      return cookies;
    }

    private static string? GetRawData(HttpRequest request, IRaygunHttpSettings options)
    {
      if (options.IsRawDataIgnored)
      {
        return null;
      }

      try
      {
        var contentType = request.ContentType;
        var isTextHtml = contentType != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(contentType, "text/html", CompareOptions.IgnoreCase) >= 0;
        var isFormUrlEncoded = contentType != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(contentType, "application/x-www-form-urlencoded", CompareOptions.IgnoreCase) >= 0;
        var isHttpGet = request.Method == "GET";
        var streamIsNull = request.Body == Stream.Null;
        var streamIsRewindable = request.Body.CanSeek;

        if (streamIsNull || !streamIsRewindable || isHttpGet || isFormUrlEncoded || isTextHtml)
        {
          return null;
        }

        Dictionary<string, string>? ignoredMultiPartFormData = null;

        if (contentType != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(contentType, "multipart/form-data", CompareOptions.IgnoreCase) >= 0)
        {
          // For multipart form data, gather all the form names and values to be stripped out later.
          ignoredMultiPartFormData = GetIgnoredFormValues(request.Form, s => IsIgnored(s, options.IgnoreFormFieldNames));
        }

        request.Body.Seek(0, SeekOrigin.Begin);

        // If we are ignoring form fields, increase the max amount that we read from the stream to make sure we include the entirety of any value that may be stripped later on.
        var length = MaxRawDataLength;

        if (ignoredMultiPartFormData != null && ignoredMultiPartFormData.Count > 0)
        {
          length += ignoredMultiPartFormData.Values.Max(s => s == null ? 0 : s.Length);
        }

        length = Math.Min(length, (int)request.Body.Length);

        // Read the stream

        var buffer = new byte[length];
        request.Body.Read(buffer, 0, length);

        var rawData = Encoding.UTF8.GetString(buffer);

        request.Body.Seek(0, SeekOrigin.Begin);

        // Strip out ignored form fields from multipart form data payloads.
        if (ignoredMultiPartFormData != null)
        {
          rawData = StripIgnoredFormData(rawData, ignoredMultiPartFormData);
        }

        // Filter out sensitive values.
        rawData = StripSensitiveValues(rawData, options);

        // Early escape if there's no data.
        if (string.IsNullOrEmpty(rawData))
        {
          return null;
        }

        // Ensure the raw data string is not too large (over 4096 bytes).
        if (rawData?.Length <= MaxRawDataLength)
        {
          return rawData;
        }

        return rawData?.Substring(0, MaxRawDataLength);
      }
      catch (Exception e)
      {
        return "Failed to retrieve raw data: " + e.Message;
      }
    }

    // This is specific to multipart/form-data
    private static Dictionary<string, string> GetIgnoredFormValues(IFormCollection form, Func<string, bool> ignore)
    {
      var ignoredFormValues = new Dictionary<string, string>();

      foreach (var key in form.Keys)
      {
        if (ignore(key))
        {
          ignoredFormValues.Add(key, form[key]);
        }
      }

      return ignoredFormValues;
    }

    // This is specific to multipart/form-data
    private static string StripIgnoredFormData(string rawData, Dictionary<string, string> ignored)
    {
      foreach (var key in ignored.Keys)
      {
        var toRemove = "name=\"" + key + "\"\r\n\r\n" + ignored[key];
        rawData = rawData.Replace(toRemove, "");
      }

      return rawData;
    }

    private static string? StripSensitiveValues(string rawData, IRaygunHttpSettings options)
    {
      // Early escape if there's no data.
      if (string.IsNullOrEmpty(rawData))
      {
        return null;
      }

      // Find the parser we want to use.
      var filters = GetRawDataFilters(options);

      foreach (var filter in filters)
      {
        // Parse the raw data into a dictionary.
        if (filter.CanParse(rawData))
        {
          var filteredData = filter.Filter(rawData, options.IgnoreSensitiveFieldNames);

          if (!string.IsNullOrEmpty(filteredData))
          {
            return filteredData;
          }
        }
      }

      // We have failed to parse and filter the raw data, so check if the data contains sensitive values and should be dropped.
      if (options.IsRawDataIgnoredWhenFilteringFailed && DataContains(rawData, options.IgnoreSensitiveFieldNames))
      {
        return null;
      }

      return rawData;
    }

    private static IList<IRaygunDataFilter> GetRawDataFilters(IRaygunHttpSettings options)
    {
      var parsers = new List<IRaygunDataFilter>();

      if (options.RawDataFilters != null && options.RawDataFilters.Count > 0)
      {
        parsers.AddRange(options.RawDataFilters);
      }

      if (options.UseXmlRawDataFilter)
      {
        parsers.Add(new RaygunXmlDataFilter());
      }

      if (options.UseKeyValuePairRawDataFilter)
      {
        parsers.Add(new RaygunKeyValuePairDataFilter());
      }

      return parsers;
    }

    private static bool DataContains(string data, IReadOnlyList<string> values)
    {
      var exists = false;

      foreach (var value in values)
      {
        if (data.Contains(value))
        {
          exists = true;
          break;
        }
      }

      return exists;
    }

    private static IDictionary GetHeaders(HttpRequest request, IRaygunHttpSettings options)
    {
      IDictionary headers = new Dictionary<string, string>();
      try
      {
        foreach (var header in request.Headers)
        {
          if (IsIgnored(header.Key, options.IgnoreHeaderNames) || IsIgnored(header.Key, options.IgnoreSensitiveFieldNames))
          {
            continue;
          }

          headers[header.Key] = string.Join(",", header.Value);
        }
      }
      catch (Exception e)
      {
        headers = new Dictionary<string, string> { { "Failed to retrieve", e.Message } };
      }

      return headers;
    }

    private static async Task<IDictionary?> GetForm(HttpRequest request, IRaygunHttpSettings options)
    {
      IDictionary? form = null;

      try
      {
        if (request.HasFormContentType)
        {
          form = ToDictionary(await request.ReadFormAsync(), s => IsIgnored(s, options.IgnoreFormFieldNames), s => IsIgnored(s, options.IgnoreSensitiveFieldNames));
        }
      }
      catch (Exception e)
      {
        form = new Dictionary<string, string> { { "Failed to retrieve", e.Message } };
      }

      return form;
    }

    private static IDictionary ToDictionary(IQueryCollection query, Func<string, bool> isQueryStringVariableIgnored, Func<string, bool> isSensitive)
    {
      var dict = new Dictionary<string, string>();

      foreach (var value in query.Where(v => isQueryStringVariableIgnored(v.Key) == false && isSensitive(v.Key) == false))
      {
        dict[value.Key] = string.Join(",", value.Value);
      }

      return dict;
    }

    private static IDictionary ToDictionary(IFormCollection query, Func<string, bool> isFormFieldIgnored, Func<string, bool> isSensitive)
    {
      var dict = new Dictionary<string, string>();

      foreach (var value in query.Where(v => isFormFieldIgnored(v.Key) == false && isSensitive(v.Key) == false))
      {
        dict[value.Key] = string.Join(",", value.Value);
      }

      return dict;
    }

    internal static bool IsIgnored(string? key, IReadOnlyList<string> list)
    {
      if (key == null || (list.Count == 1 && "*".Equals(list[0])))
      {
        return true;
      }

      // ReSharper disable once LoopCanBeConvertedToQuery
      foreach (var ignoredKey in list)
      {
        var ignoreResult = ignoredKey switch
        {
          _ when ignoredKey.StartsWith("*") && ignoredKey.EndsWith("*") && ignoredKey.Length > 2 => CheckContains(ignoredKey, key),
          _ when ignoredKey.StartsWith("*") => CheckEndsWith(ignoredKey, key),
          _ when ignoredKey.EndsWith("*") => CheckStartsWith(ignoredKey, key),
          _ => key.Equals(ignoredKey, StringComparison.OrdinalIgnoreCase)
        };

        if (ignoreResult)
        {
          return true;
        }
      }

      return false;
    }

    private static bool CheckStartsWith(string ignoredKey, string key)
    {
      var value = ignoredKey.AsSpan(0, ignoredKey.Length - 1);

      return key.AsSpan().StartsWith(value, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CheckEndsWith(string ignoredKey, string key)
    {
      var value = ignoredKey.AsSpan(1);
      return key.AsSpan().EndsWith(value, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CheckContains(string ignoredKey, string key)
    {
      var value = ignoredKey.AsSpan(1, ignoredKey.Length - 2);

      return key.AsSpan().IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
  }
}
