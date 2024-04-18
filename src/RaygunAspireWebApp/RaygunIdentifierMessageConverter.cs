using Mindscape.Raygun4Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaygunAspireWebApp
{
  public class RaygunIdentifierMessageConverter : JsonConverter<RaygunIdentifierMessage>
  {
    public override RaygunIdentifierMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      if (reader.TokenType != JsonTokenType.StartObject)
      {
        throw new JsonException();
      }

      string identifier = null;
      bool isAnonymous = false;
      string email = null;
      string fullName = null;
      string firstName = null;
      string uuid = null;

      while (reader.Read())
      {
        if (reader.TokenType == JsonTokenType.EndObject)
        {
          break;
        }

        if (reader.TokenType == JsonTokenType.PropertyName)
        {
          var propertyName = reader.GetString();
          reader.Read();

          if (string.Equals(propertyName, "Identifier", StringComparison.OrdinalIgnoreCase))
          {
            identifier = reader.GetString();
          }
          else if (string.Equals(propertyName, "IsAnonymous", StringComparison.OrdinalIgnoreCase))
          {
            isAnonymous = reader.GetBoolean();
          }
          else if (string.Equals(propertyName, "Email", StringComparison.OrdinalIgnoreCase))
          {
            email = reader.GetString();
          }
          else if (string.Equals(propertyName, "FullName", StringComparison.OrdinalIgnoreCase))
          {
            fullName = reader.GetString();
          }
          else if (string.Equals(propertyName, "FirstName", StringComparison.OrdinalIgnoreCase))
          {
            firstName = reader.GetString();
          }
          else if (string.Equals(propertyName, "UUID", StringComparison.OrdinalIgnoreCase))
          {
            uuid = reader.GetString();
          }
        }
      }

      return new RaygunIdentifierMessage(identifier)
      {
        Email = email,
        FullName = fullName,
        FirstName = firstName,
        Identifier = identifier,
        IsAnonymous = isAnonymous,
        UUID = uuid
      };
    }

    public override void Write(Utf8JsonWriter writer, RaygunIdentifierMessage value, JsonSerializerOptions options)
    {
      throw new NotImplementedException();
    }
  }
}
